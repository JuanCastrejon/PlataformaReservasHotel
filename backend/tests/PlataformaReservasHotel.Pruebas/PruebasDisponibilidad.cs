using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlataformaReservasHotel.Api.Controladores;
using PlataformaReservasHotel.Api.Datos;
using PlataformaReservasHotel.Api.Dominio;
using PlataformaReservasHotel.Api.Modelos;
using PlataformaReservasHotel.Api.Servicios;

namespace PlataformaReservasHotel.Pruebas;

public class PruebasDisponibilidad : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly ContextoHotel _contexto;
    private readonly ServicioDisponibilidad _servicio;
    private readonly DisponibilidadController _controlador;

    public PruebasDisponibilidad()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<ContextoHotel>()
            .UseSqlite(_conexion)
            .Options;

        _contexto = new ContextoHotel(opciones);
        _contexto.Database.EnsureCreated();
        InicializadorDatos.Inicializar(_contexto);

        _servicio = new ServicioDisponibilidad(_contexto);
        var servicioCotizacion = new ServicioCotizacion(_contexto, _servicio);
        _controlador = new DisponibilidadController(_servicio, servicioCotizacion);
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

    [Fact]
    public async Task Caso01_BusquedaValida_DevuelveCategoriasYCantidadesCorrectas()
    {
        var idBogota = ObtenerIdSede("Bogotá");
        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idBogota,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5),
            NumeroHuespedes = 4
        };

        var resultado = await _servicio.ConsultarDisponibilidadAsync(parametros);

        Assert.Equal(3, resultado.Count); // Estándar (cap 6), Premium (cap 6), VIP (cap 6)
        var estandar = resultado.Single(r => r.TipoHabitacion == "Estándar");
        Assert.Equal(20, estandar.HabitacionesDisponibles);
        Assert.Equal(6, estandar.CapacidadMaxima);

        var premium = resultado.Single(r => r.TipoHabitacion == "Premium");
        Assert.Equal(20, premium.HabitacionesDisponibles);

        var vip = resultado.Single(r => r.TipoHabitacion == "VIP");
        Assert.Equal(2, vip.HabitacionesDisponibles);
    }

    [Theory]
    [InlineData("2026-11-05", "2026-11-05")] // Fechas iguales
    [InlineData("2026-11-06", "2026-11-05")] // Entrada posterior a salida
    public async Task Caso02_FechaEntradaIgualOPosteriorASalida_LanzaArgumentException(string entrada, string salida)
    {
        var idBogota = ObtenerIdSede("Bogotá");
        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idBogota,
            FechaEntrada = DateOnly.Parse(entrada),
            FechaSalida = DateOnly.Parse(salida),
            NumeroHuespedes = 2
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _servicio.ConsultarDisponibilidadAsync(parametros));

        Assert.Contains("estrictamente anterior", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public async Task Caso03_NumeroHuespedesNoPositivo_LanzaArgumentException(int huespedes)
    {
        var idBogota = ObtenerIdSede("Bogotá");
        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idBogota,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5),
            NumeroHuespedes = huespedes
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _servicio.ConsultarDisponibilidadAsync(parametros));

        Assert.Contains("entero positivo", ex.Message);
    }

    [Fact]
    public async Task Caso04_HabitacionesConCapacidadInsuficiente_QuedanExcluidas()
    {
        // En Barranquilla la capacidad máxima es 4 personas (Estándar: 4, Premium: 4)
        var idBarranquilla = ObtenerIdSede("Barranquilla");
        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idBarranquilla,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5),
            NumeroHuespedes = 5
        };

        var resultado = await _servicio.ConsultarDisponibilidadAsync(parametros);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task Caso05_HabitacionOcupadaDuranteLaEstadia_QuedaExcluida()
    {
        // En Cartagena la categoría Premium tiene exactamente 1 habitación física (CTG-PRE-001)
        var idCartagena = ObtenerIdSede("Cartagena");
        var habitacion = _contexto.Habitaciones
            .Include(h => h.CategoriaHabitacion)
            .Single(h => h.CategoriaHabitacion.SedeId == idCartagena && h.CategoriaHabitacion.Tipo == "Premium");

        _contexto.Ocupaciones.Add(new Ocupacion
        {
            HabitacionId = habitacion.Id,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5)
        });
        await _contexto.SaveChangesAsync();

        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idCartagena,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5),
            NumeroHuespedes = 2,
            TipoHabitacion = "Premium"
        };

        var resultado = await _servicio.ConsultarDisponibilidadAsync(parametros);

        var premium = Assert.Single(resultado);
        Assert.Equal(0, premium.HabitacionesDisponibles);
    }

    [Fact]
    public async Task Caso06_OcupacionQueTerminaEnFechaDeEntrada_NoBloqueaLaHabitacion()
    {
        var idCartagena = ObtenerIdSede("Cartagena");
        var habitacion = _contexto.Habitaciones
            .Include(h => h.CategoriaHabitacion)
            .Single(h => h.CategoriaHabitacion.SedeId == idCartagena && h.CategoriaHabitacion.Tipo == "Premium");

        // Ocupación previa del 2026-10-28 al 2026-11-01 (salida a las 12:00 m.)
        _contexto.Ocupaciones.Add(new Ocupacion
        {
            HabitacionId = habitacion.Id,
            FechaEntrada = new DateOnly(2026, 10, 28),
            FechaSalida = new DateOnly(2026, 11, 1)
        });
        await _contexto.SaveChangesAsync();

        // Búsqueda solicitada del 2026-11-01 al 2026-11-05 (check-in 2026-11-01 a las 15:00)
        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idCartagena,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5),
            NumeroHuespedes = 2,
            TipoHabitacion = "Premium"
        };

        var resultado = await _servicio.ConsultarDisponibilidadAsync(parametros);

        var premium = Assert.Single(resultado);
        Assert.Equal(1, premium.HabitacionesDisponibles);
    }

    [Fact]
    public async Task Caso07_OcupacionQueEmpiezaEnFechaDeSalida_NoBloqueaLaHabitacion()
    {
        var idCartagena = ObtenerIdSede("Cartagena");
        var habitacion = _contexto.Habitaciones
            .Include(h => h.CategoriaHabitacion)
            .Single(h => h.CategoriaHabitacion.SedeId == idCartagena && h.CategoriaHabitacion.Tipo == "Premium");

        // Ocupación posterior del 2026-11-05 al 2026-11-10 (check-in a las 15:00)
        _contexto.Ocupaciones.Add(new Ocupacion
        {
            HabitacionId = habitacion.Id,
            FechaEntrada = new DateOnly(2026, 11, 5),
            FechaSalida = new DateOnly(2026, 11, 10)
        });
        await _contexto.SaveChangesAsync();

        // Búsqueda del 2026-11-01 al 2026-11-05 (check-out a las 12:00 m.)
        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idCartagena,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5),
            NumeroHuespedes = 2,
            TipoHabitacion = "Premium"
        };

        var resultado = await _servicio.ConsultarDisponibilidadAsync(parametros);

        var premium = Assert.Single(resultado);
        Assert.Equal(1, premium.HabitacionesDisponibles);
    }

    [Theory]
    [InlineData("2026-10-30", "2026-11-02")] // Solapamiento al inicio
    [InlineData("2026-11-04", "2026-11-08")] // Solapamiento al final
    [InlineData("2026-11-02", "2026-11-04")] // Totalmente contenida
    [InlineData("2026-10-25", "2026-11-10")] // Envuelve la estancia completa
    public async Task Caso08_OcupacionQueSeCruzaParcialmenteConIntervalo_BloqueaLaHabitacion(string ocupEntrada, string ocupSalida)
    {
        var idCartagena = ObtenerIdSede("Cartagena");
        var habitacion = _contexto.Habitaciones
            .Include(h => h.CategoriaHabitacion)
            .Single(h => h.CategoriaHabitacion.SedeId == idCartagena && h.CategoriaHabitacion.Tipo == "Premium");

        _contexto.Ocupaciones.Add(new Ocupacion
        {
            HabitacionId = habitacion.Id,
            FechaEntrada = DateOnly.Parse(ocupEntrada),
            FechaSalida = DateOnly.Parse(ocupSalida)
        });
        await _contexto.SaveChangesAsync();

        // Consulta solicitada: 2026-11-01 al 2026-11-05
        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idCartagena,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5),
            NumeroHuespedes = 2,
            TipoHabitacion = "Premium"
        };

        var resultado = await _servicio.ConsultarDisponibilidadAsync(parametros);

        var premium = Assert.Single(resultado);
        Assert.Equal(0, premium.HabitacionesDisponibles);
    }

    [Fact]
    public async Task Caso09_BusquedaConTodasLasHabitacionesOcupadas_DevuelveCeroDisponibilidad()
    {
        // En Cali la categoría VIP tiene exactamente 2 habitaciones
        var idCali = ObtenerIdSede("Cali");
        var habitacionesVip = _contexto.Habitaciones
            .Include(h => h.CategoriaHabitacion)
            .Where(h => h.CategoriaHabitacion.SedeId == idCali && h.CategoriaHabitacion.Tipo == "VIP")
            .ToList();

        Assert.Equal(2, habitacionesVip.Count);

        // Ocupamos ambas habitaciones
        foreach (var hab in habitacionesVip)
        {
            _contexto.Ocupaciones.Add(new Ocupacion
            {
                HabitacionId = hab.Id,
                FechaEntrada = new DateOnly(2026, 12, 1),
                FechaSalida = new DateOnly(2026, 12, 10)
            });
        }
        await _contexto.SaveChangesAsync();

        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idCali,
            FechaEntrada = new DateOnly(2026, 12, 3),
            FechaSalida = new DateOnly(2026, 12, 7),
            NumeroHuespedes = 2,
            TipoHabitacion = "VIP"
        };

        var resultado = await _servicio.ConsultarDisponibilidadAsync(parametros);

        var vip = Assert.Single(resultado);
        Assert.Equal(0, vip.HabitacionesDisponibles);
    }

    [Fact]
    public async Task Caso10_CategoriaOpcional_FiltraCorrectamenteLosResultados()
    {
        // Bogotá tiene Estándar, Premium y VIP. Solicitamos solo Premium.
        var idBogota = ObtenerIdSede("Bogotá");
        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idBogota,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5),
            NumeroHuespedes = 2,
            TipoHabitacion = "Premium"
        };

        var resultado = await _servicio.ConsultarDisponibilidadAsync(parametros);

        var categoria = Assert.Single(resultado);
        Assert.Equal("Premium", categoria.TipoHabitacion);
        Assert.Equal(20, categoria.HabitacionesDisponibles);
    }

    [Fact]
    public async Task Caso11_Sede_NoDevuelveHabitacionesDeOtraSede()
    {
        // Cali tiene solo Premium y VIP (no tiene Estándar)
        var idCali = ObtenerIdSede("Cali");
        var parametros = new ConsultaDisponibilidadParametros
        {
            SedeId = idCali,
            FechaEntrada = new DateOnly(2026, 11, 1),
            FechaSalida = new DateOnly(2026, 11, 5),
            NumeroHuespedes = 2
        };

        var resultado = await _servicio.ConsultarDisponibilidadAsync(parametros);

        Assert.All(resultado, r => Assert.Equal(idCali, r.SedeId));
        Assert.DoesNotContain(resultado, r => r.TipoHabitacion == "Estándar");
        Assert.Contains(resultado, r => r.TipoHabitacion == "Premium");
        Assert.Contains(resultado, r => r.TipoHabitacion == "VIP");
    }

    [Fact]
    public async Task Controlador_RetornaBadRequest_CuandoParametrosSonInvalidos()
    {
        var idBogota = ObtenerIdSede("Bogotá");

        // Fechas invertidas
        var respuesta = await _controlador.ObtenerHabitacionesDisponibles(
            sedeId: idBogota,
            fechaEntrada: new DateOnly(2026, 11, 5),
            fechaSalida: new DateOnly(2026, 11, 2),
            numeroHuespedes: 2,
            tipoHabitacion: null,
            cancelacion: default);

        Assert.IsType<BadRequestObjectResult>(respuesta);
    }

    [Fact]
    public async Task Controlador_RetornaNotFound_CuandoSedeNoExiste()
    {
        var respuesta = await _controlador.ObtenerHabitacionesDisponibles(
            sedeId: 9999, // Inexistente
            fechaEntrada: new DateOnly(2026, 11, 1),
            fechaSalida: new DateOnly(2026, 11, 5),
            numeroHuespedes: 2,
            tipoHabitacion: null,
            cancelacion: default);

        Assert.IsType<NotFoundObjectResult>(respuesta);
    }

    [Fact]
    public async Task Controlador_RetornaOk_ConRespuestaValida()
    {
        var idBogota = ObtenerIdSede("Bogotá");

        var respuesta = await _controlador.ObtenerHabitacionesDisponibles(
            sedeId: idBogota,
            fechaEntrada: new DateOnly(2026, 11, 1),
            fechaSalida: new DateOnly(2026, 11, 5),
            numeroHuespedes: 2,
            tipoHabitacion: "Estándar",
            cancelacion: default);

        var okResult = Assert.IsType<OkObjectResult>(respuesta);
        var categorias = Assert.IsType<List<CategoriaDisponibilidadDto>>(okResult.Value);
        var categoria = Assert.Single(categorias);
        Assert.Equal("Estándar", categoria.TipoHabitacion);
        Assert.Equal(20, categoria.HabitacionesDisponibles);
    }
}
