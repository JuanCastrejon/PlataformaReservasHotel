using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlataformaReservasHotel.Api.Controladores;
using PlataformaReservasHotel.Api.Datos;
using PlataformaReservasHotel.Api.Dominio;
using PlataformaReservasHotel.Api.Modelos;
using PlataformaReservasHotel.Api.Servicios;

namespace PlataformaReservasHotel.Pruebas;

public class PruebasCotizacion : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly ContextoHotel _contexto;
    private readonly ServicioDisponibilidad _servicioDisponibilidad;
    private readonly ServicioCotizacion _servicioCotizacion;
    private readonly DisponibilidadController _controlador;

    public PruebasCotizacion()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<ContextoHotel>()
            .UseSqlite(_conexion)
            .Options;

        _contexto = new ContextoHotel(opciones);
        _contexto.Database.EnsureCreated();
        InicializadorDatos.Inicializar(_contexto);

        _servicioDisponibilidad = new ServicioDisponibilidad(_contexto);
        _servicioCotizacion = new ServicioCotizacion(_contexto, _servicioDisponibilidad);
        _controlador = new DisponibilidadController(_servicioDisponibilidad, _servicioCotizacion);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }

    private int ObtenerIdSede(string ciudad)
    {
        return _contexto.Sedes.Single(s => s.Ciudad == ciudad).Id;
    }

    private int ObtenerIdCategoria(string ciudad, string tipo)
    {
        return _contexto.CategoriasHabitacion
            .Include(c => c.Sede)
            .Single(c => c.Sede.Ciudad == ciudad && c.Tipo == tipo)
            .Id;
    }

    [Fact]
    public async Task Caso01_CotizacionValida_EnTemporadaBaja_CalculaTarifaBaseCorrecta()
    {
        // Bogotá - Estándar: tarifa base 120000 + 20000 (sede) + 2*20000 (2 huéspedes) = 180000 COP por noche
        var sedeBogotaId = ObtenerIdSede("Bogotá");
        var parametros = new ConsultaCotizacionParametros
        {
            SedeId = sedeBogotaId,
            TipoHabitacion = "Estándar",
            FechaEntrada = new DateOnly(2026, 3, 10),
            FechaSalida = new DateOnly(2026, 3, 13), // 3 noches
            NumeroHuespedes = 2,
            CantidadHabitaciones = 1
        };

        var resultado = await _servicioCotizacion.CotizarEstadiaAsync(parametros);

        Assert.NotNull(resultado);
        Assert.Equal("Bogotá", resultado.Sede);
        Assert.Equal("Estándar", resultado.TipoHabitacion);
        Assert.Equal(3, resultado.NumeroNoches);
        Assert.Equal(2, resultado.NumeroHuespedes);
        Assert.Equal(1, resultado.CantidadHabitaciones);
        Assert.Equal(3, resultado.DesgloseNoches.Count);

        // Cada noche debe costar 180,000 COP y ser Temporada Baja
        foreach (var noche in resultado.DesgloseNoches)
        {
            Assert.Equal("Temporada Baja", noche.Temporada);
            Assert.Equal(180000m, noche.TarifaTotalNoche);
            Assert.Single(noche.DetalleHabitaciones);
            Assert.Equal(2, noche.DetalleHabitaciones[0].Huespedes);
            Assert.Equal(180000m, noche.DetalleHabitaciones[0].TarifaAplicada);
        }

        Assert.Equal(540000m, resultado.TotalEstadia); // 3 * 180,000
    }

    [Fact]
    public async Task Caso02_CotizacionValida_EnTemporadaAlta_AplicaTarifaDiferencialConIncremento()
    {
        // Bogotá - Estándar: tarifa baja = 180000. Temporada alta = 180000 * 1.40 = 252000 COP por noche
        var sedeBogotaId = ObtenerIdSede("Bogotá");
        var parametros = new ConsultaCotizacionParametros
        {
            SedeId = sedeBogotaId,
            TipoHabitacion = "Estándar",
            FechaEntrada = new DateOnly(2026, 6, 20),
            FechaSalida = new DateOnly(2026, 6, 23), // 3 noches en temporada alta
            NumeroHuespedes = 2,
            CantidadHabitaciones = 1
        };

        var resultado = await _servicioCotizacion.CotizarEstadiaAsync(parametros);

        Assert.NotNull(resultado);
        Assert.Equal(3, resultado.NumeroNoches);

        foreach (var noche in resultado.DesgloseNoches)
        {
            Assert.Equal("Temporada Alta - Mitad de Año", noche.Temporada);
            Assert.Equal(252000m, noche.TarifaTotalNoche);
        }

        Assert.Equal(756000m, resultado.TotalEstadia); // 3 * 252,000
    }

    [Fact]
    public async Task Caso03_EstadiaCruceTemporadaBajaYAlta_SumaTarifasDiferenciadasNochePorNoche()
    {
        // Del 13 al 17 de junio 2026 (4 noches):
        // Noches 13 y 14: Temporada Baja (180,000 COP c/u)
        // Noches 15 y 16: Temporada Alta - Mitad de Año (252,000 COP c/u)
        // Total esperado: 180,000 + 180,000 + 252,000 + 252,000 = 864,000 COP
        var sedeBogotaId = ObtenerIdSede("Bogotá");
        var parametros = new ConsultaCotizacionParametros
        {
            SedeId = sedeBogotaId,
            TipoHabitacion = "Estándar",
            FechaEntrada = new DateOnly(2026, 6, 13),
            FechaSalida = new DateOnly(2026, 6, 17),
            NumeroHuespedes = 2,
            CantidadHabitaciones = 1
        };

        var resultado = await _servicioCotizacion.CotizarEstadiaAsync(parametros);

        Assert.NotNull(resultado);
        Assert.Equal(4, resultado.NumeroNoches);
        Assert.Equal(4, resultado.DesgloseNoches.Count);

        // Noche 1: 13 de junio (Baja)
        Assert.Equal(new DateOnly(2026, 6, 13), resultado.DesgloseNoches[0].Fecha);
        Assert.Equal("Temporada Baja", resultado.DesgloseNoches[0].Temporada);
        Assert.Equal(180000m, resultado.DesgloseNoches[0].TarifaTotalNoche);

        // Noche 2: 14 de junio (Baja)
        Assert.Equal(new DateOnly(2026, 6, 14), resultado.DesgloseNoches[1].Fecha);
        Assert.Equal("Temporada Baja", resultado.DesgloseNoches[1].Temporada);
        Assert.Equal(180000m, resultado.DesgloseNoches[1].TarifaTotalNoche);

        // Noche 3: 15 de junio (Alta)
        Assert.Equal(new DateOnly(2026, 6, 15), resultado.DesgloseNoches[2].Fecha);
        Assert.Equal("Temporada Alta - Mitad de Año", resultado.DesgloseNoches[2].Temporada);
        Assert.Equal(252000m, resultado.DesgloseNoches[2].TarifaTotalNoche);

        // Noche 4: 16 de junio (Alta)
        Assert.Equal(new DateOnly(2026, 6, 16), resultado.DesgloseNoches[3].Fecha);
        Assert.Equal("Temporada Alta - Mitad de Año", resultado.DesgloseNoches[3].Temporada);
        Assert.Equal(252000m, resultado.DesgloseNoches[3].TarifaTotalNoche);

        Assert.Equal(864000m, resultado.TotalEstadia);
    }

    [Fact]
    public async Task Caso04_DiaDeSalida_NoSeCobraComoNocheDeAlojamiento()
    {
        // Entrada: 10 de octubre, Salida: 13 de octubre -> Exactamente 3 noches (10, 11 y 12)
        var sedeCaliId = ObtenerIdSede("Cali");
        var parametros = new ConsultaCotizacionParametros
        {
            SedeId = sedeCaliId,
            TipoHabitacion = "Premium",
            FechaEntrada = new DateOnly(2026, 10, 10),
            FechaSalida = new DateOnly(2026, 10, 13),
            NumeroHuespedes = 1,
            CantidadHabitaciones = 1
        };

        var resultado = await _servicioCotizacion.CotizarEstadiaAsync(parametros);

        Assert.Equal(3, resultado.NumeroNoches);
        Assert.Equal(3, resultado.DesgloseNoches.Count);
        Assert.DoesNotContain(resultado.DesgloseNoches, n => n.Fecha == new DateOnly(2026, 10, 13));
        Assert.Equal(new DateOnly(2026, 10, 12), resultado.DesgloseNoches.Last().Fecha);
    }

    [Fact]
    public async Task Caso05_VariacionTarifaria_PorSede_TipoHabitacion_YNumeroOcupantes()
    {
        // 1 huésped vs 2 huéspedes en Barranquilla Estándar:
        // 1 huésped: 120,000 + 0 + 20,000 = 140,000 COP
        // 2 huéspedes: 120,000 + 0 + 40,000 = 160,000 COP
        var sedeBqId = ObtenerIdSede("Barranquilla");
        var cotizacionBq1 = await _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
        {
            SedeId = sedeBqId,
            TipoHabitacion = "Estándar",
            FechaEntrada = new DateOnly(2026, 3, 1),
            FechaSalida = new DateOnly(2026, 3, 2),
            NumeroHuespedes = 1,
            CantidadHabitaciones = 1
        });

        var cotizacionBq2 = await _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
        {
            SedeId = sedeBqId,
            TipoHabitacion = "Estándar",
            FechaEntrada = new DateOnly(2026, 3, 1),
            FechaSalida = new DateOnly(2026, 3, 2),
            NumeroHuespedes = 2,
            CantidadHabitaciones = 1
        });

        Assert.Equal(140000m, cotizacionBq1.TotalEstadia);
        Assert.Equal(160000m, cotizacionBq2.TotalEstadia);
        Assert.True(cotizacionBq2.TotalEstadia > cotizacionBq1.TotalEstadia);

        // Diferencia por Tipo (Bogotá Estándar vs VIP con 2 personas):
        // Bogotá Estándar: 120,000 + 20,000 + 40,000 = 180,000 COP
        // Bogotá VIP: 280,000 + 20,000 + 100,000 = 400,000 COP
        var sedeBogotaId = ObtenerIdSede("Bogotá");
        var cotizacionEstandar = await _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
        {
            SedeId = sedeBogotaId,
            TipoHabitacion = "Estándar",
            FechaEntrada = new DateOnly(2026, 3, 1),
            FechaSalida = new DateOnly(2026, 3, 2),
            NumeroHuespedes = 2,
            CantidadHabitaciones = 1
        });

        var cotizacionVip = await _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
        {
            SedeId = sedeBogotaId,
            TipoHabitacion = "VIP",
            FechaEntrada = new DateOnly(2026, 3, 1),
            FechaSalida = new DateOnly(2026, 3, 2),
            NumeroHuespedes = 2,
            CantidadHabitaciones = 1
        });

        Assert.Equal(180000m, cotizacionEstandar.TotalEstadia);
        Assert.Equal(400000m, cotizacionVip.TotalEstadia);
        Assert.True(cotizacionVip.TotalEstadia > cotizacionEstandar.TotalEstadia);

        // Diferencia por Sede (Cartagena Estándar vs Barranquilla Estándar, 2 personas):
        // Cartagena Estándar: 120,000 + 30,000 + 40,000 = 190,000 COP
        // Barranquilla Estándar: 160,000 COP
        var sedeCtgId = ObtenerIdSede("Cartagena");
        var cotizacionCtg = await _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
        {
            SedeId = sedeCtgId,
            TipoHabitacion = "Estándar",
            FechaEntrada = new DateOnly(2026, 3, 1),
            FechaSalida = new DateOnly(2026, 3, 2),
            NumeroHuespedes = 2,
            CantidadHabitaciones = 1
        });

        Assert.Equal(190000m, cotizacionCtg.TotalEstadia);
        Assert.True(cotizacionCtg.TotalEstadia > cotizacionBq2.TotalEstadia);
    }

    [Fact]
    public async Task Caso06_ParametrosInvalidos_GeneraRechazoControlado()
    {
        var sedeId = ObtenerIdSede("Bogotá");

        // Huéspedes <= 0
        var exHuespedes = await Assert.ThrowsAsync<ArgumentException>(() =>
            _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
            {
                SedeId = sedeId,
                TipoHabitacion = "Estándar",
                FechaEntrada = new DateOnly(2026, 4, 1),
                FechaSalida = new DateOnly(2026, 4, 5),
                NumeroHuespedes = 0,
                CantidadHabitaciones = 1
            }));
        Assert.Contains("número de huéspedes", exHuespedes.Message);

        // Habitaciones <= 0
        var exHabitaciones = await Assert.ThrowsAsync<ArgumentException>(() =>
            _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
            {
                SedeId = sedeId,
                TipoHabitacion = "Estándar",
                FechaEntrada = new DateOnly(2026, 4, 1),
                FechaSalida = new DateOnly(2026, 4, 5),
                NumeroHuespedes = 2,
                CantidadHabitaciones = 0
            }));
        Assert.Contains("cantidad de habitaciones", exHabitaciones.Message);

        // Habitaciones > Huéspedes
        var exMasHabQueHuesp = await Assert.ThrowsAsync<ArgumentException>(() =>
            _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
            {
                SedeId = sedeId,
                TipoHabitacion = "Estándar",
                FechaEntrada = new DateOnly(2026, 4, 1),
                FechaSalida = new DateOnly(2026, 4, 5),
                NumeroHuespedes = 2,
                CantidadHabitaciones = 3
            }));
        Assert.Contains("no puede ser superior al número de huéspedes", exMasHabQueHuesp.Message);

        // Fechas invertidas
        var exFechas = await Assert.ThrowsAsync<ArgumentException>(() =>
            _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
            {
                SedeId = sedeId,
                TipoHabitacion = "Estándar",
                FechaEntrada = new DateOnly(2026, 4, 5),
                FechaSalida = new DateOnly(2026, 4, 1),
                NumeroHuespedes = 2,
                CantidadHabitaciones = 1
            }));
        Assert.Contains("fecha de entrada debe ser estrictamente anterior", exFechas.Message);
    }

    [Fact]
    public async Task Caso07_DistribucionExcedeCapacidadMaxima_GeneraRechazo()
    {
        // Barranquilla Estándar: Capacidad máxima por habitación es 4.
        // Si solicitamos 1 habitación para 5 huéspedes, debe rechazar por capacidad máxima.
        var sedeBqId = ObtenerIdSede("Barranquilla");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
            {
                SedeId = sedeBqId,
                TipoHabitacion = "Estándar",
                FechaEntrada = new DateOnly(2026, 4, 1),
                FechaSalida = new DateOnly(2026, 4, 3),
                NumeroHuespedes = 5,
                CantidadHabitaciones = 1
            }));

        Assert.Contains("capacidad máxima total", ex.Message);
    }

    [Fact]
    public async Task Caso08_CantidadHabitacionesSuperiorADisponibilidad_GeneraConflicto409()
    {
        // Bogotá VIP tiene únicamente 2 habitaciones.
        // Si se solicitan 3 habitaciones VIP, no hay inventario disponible suficiente.
        var sedeBogotaId = ObtenerIdSede("Bogotá");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
            {
                SedeId = sedeBogotaId,
                TipoHabitacion = "VIP",
                FechaEntrada = new DateOnly(2026, 4, 1),
                FechaSalida = new DateOnly(2026, 4, 5),
                NumeroHuespedes = 6,
                CantidadHabitaciones = 3
            }));

        Assert.Contains("Disponibilidad insuficiente", ex.Message);

        // Validar también a nivel de controlador que devuelva HTTP 409 Conflict
        var respuestaControlador = await _controlador.CotizarEstadia(
            sedeBogotaId,
            null,
            "VIP",
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 5),
            6,
            3,
            CancellationToken.None);

        var conflicto = Assert.IsType<ConflictObjectResult>(respuestaControlador);
        Assert.Equal(409, conflicto.StatusCode);
    }

    [Fact]
    public async Task Caso09_TarifaFaltanteParaCombinacion_GeneraErrorControlado422()
    {
        // Eliminamos deliberadamente la tarifa de 1 huésped de temporada baja para Cartagena Premium
        var catCartagenaPremiumId = ObtenerIdCategoria("Cartagena", "Premium");
        var temporadaBaja = _contexto.Temporadas.Single(t => t.EsTemporadaBase);

        var tarifaABorrar = _contexto.Tarifas.Single(t =>
            t.CategoriaHabitacionId == catCartagenaPremiumId &&
            t.TemporadaId == temporadaBaja.Id &&
            t.NumeroHuespedes == 1);

        _contexto.Tarifas.Remove(tarifaABorrar);
        await _contexto.SaveChangesAsync();

        var sedeCtgId = ObtenerIdSede("Cartagena");

        // Invocación directa del servicio
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servicioCotizacion.CotizarEstadiaAsync(new ConsultaCotizacionParametros
            {
                SedeId = sedeCtgId,
                CategoriaId = catCartagenaPremiumId,
                FechaEntrada = new DateOnly(2026, 3, 1),
                FechaSalida = new DateOnly(2026, 3, 3),
                NumeroHuespedes = 1,
                CantidadHabitaciones = 1
            }));

        Assert.Contains("No existe una tarifa configurada", ex.Message);

        // A nivel de controlador debe retornar 422 UnprocessableEntity
        var respuesta = await _controlador.CotizarEstadia(
            sedeCtgId,
            catCartagenaPremiumId,
            null,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 3),
            1,
            1,
            CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(respuesta);
        Assert.Equal(422, unprocessable.StatusCode);
    }

    [Fact]
    public void Caso10_SemillaTarifasYTemporadas_EsIdempotente()
    {
        var temporadasIniciales = _contexto.Temporadas.Count();
        var tarifasIniciales = _contexto.Tarifas.Count();
        var habitacionesIniciales = _contexto.Habitaciones.Count();

        Assert.Equal(3, temporadasIniciales);
        Assert.Equal(162, tarifasIniciales);
        Assert.Equal(108, habitacionesIniciales);

        // Ejecutar InicializadorDatos nuevamente
        InicializadorDatos.Inicializar(_contexto);

        Assert.Equal(temporadasIniciales, _contexto.Temporadas.Count());
        Assert.Equal(tarifasIniciales, _contexto.Tarifas.Count());
        Assert.Equal(habitacionesIniciales, _contexto.Habitaciones.Count());
    }

    [Fact]
    public async Task Caso11_CotizacionEstadia_EsOperacionDeSoloLectura_NoPersisteOcupaciones()
    {
        var ocupacionesIniciales = await _contexto.Ocupaciones.CountAsync();
        var sedeBogotaId = ObtenerIdSede("Bogotá");

        var parametros = new ConsultaCotizacionParametros
        {
            SedeId = sedeBogotaId,
            TipoHabitacion = "Estándar",
            FechaEntrada = new DateOnly(2026, 5, 1),
            FechaSalida = new DateOnly(2026, 5, 5),
            NumeroHuespedes = 4,
            CantidadHabitaciones = 2
        };

        var resultado = await _servicioCotizacion.CotizarEstadiaAsync(parametros);

        Assert.NotNull(resultado);
        var ocupacionesFinales = await _contexto.Ocupaciones.CountAsync();
        Assert.Equal(ocupacionesIniciales, ocupacionesFinales);
    }

    [Fact]
    public async Task DistribucionEquilibrada_MultiplesHabitaciones_ReparteHuespedesAdecuadamente()
    {
        // 5 huéspedes en 2 habitaciones -> Habitación 1: 3 huéspedes, Habitación 2: 2 huéspedes
        var sedeBogotaId = ObtenerIdSede("Bogotá");
        var parametros = new ConsultaCotizacionParametros
        {
            SedeId = sedeBogotaId,
            TipoHabitacion = "Estándar",
            FechaEntrada = new DateOnly(2026, 3, 1),
            FechaSalida = new DateOnly(2026, 3, 2), // 1 noche
            NumeroHuespedes = 5,
            CantidadHabitaciones = 2
        };

        var resultado = await _servicioCotizacion.CotizarEstadiaAsync(parametros);

        Assert.Equal(2, resultado.Habitaciones.Count);
        Assert.Equal(3, resultado.Habitaciones[0].HuespedesAsignados);
        Assert.Equal(2, resultado.Habitaciones[1].HuespedesAsignados);

        // Hab 1 con 3 personas en Bogotá Estándar: 120000 + 20000 + 60000 = 200,000 COP
        // Hab 2 con 2 personas en Bogotá Estándar: 120000 + 20000 + 40000 = 180,000 COP
        // Total = 380,000 COP
        Assert.Equal(200000m, resultado.Habitaciones[0].SubtotalHabitacion);
        Assert.Equal(180000m, resultado.Habitaciones[1].SubtotalHabitacion);
        Assert.Equal(380000m, resultado.TotalEstadia);
    }
}
