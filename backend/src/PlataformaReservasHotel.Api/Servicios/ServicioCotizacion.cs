using Microsoft.EntityFrameworkCore;
using PlataformaReservasHotel.Api.Datos;
using PlataformaReservasHotel.Api.Dominio;
using PlataformaReservasHotel.Api.Modelos;

namespace PlataformaReservasHotel.Api.Servicios;

public interface IServicioCotizacion
{
    Task<CotizacionEstadiaDto> CotizarEstadiaAsync(
        ConsultaCotizacionParametros parametros,
        CancellationToken cancelacion = default);
}

public class ServicioCotizacion : IServicioCotizacion
{
    private readonly ContextoHotel _contexto;
    private readonly IServicioDisponibilidad _servicioDisponibilidad;

    public ServicioCotizacion(ContextoHotel contexto, IServicioDisponibilidad servicioDisponibilidad)
    {
        _contexto = contexto;
        _servicioDisponibilidad = servicioDisponibilidad;
    }

    public async Task<CotizacionEstadiaDto> CotizarEstadiaAsync(
        ConsultaCotizacionParametros parametros,
        CancellationToken cancelacion = default)
    {
        // 1. Validaciones básicas
        if (parametros.FechaEntrada >= parametros.FechaSalida)
        {
            throw new ArgumentException("La fecha de entrada debe ser estrictamente anterior a la fecha de salida.");
        }

        if (parametros.NumeroHuespedes <= 0)
        {
            throw new ArgumentException("El número de huéspedes debe ser un entero positivo mayor que cero.");
        }

        if (parametros.CantidadHabitaciones <= 0)
        {
            throw new ArgumentException("La cantidad de habitaciones debe ser un entero positivo mayor que cero.");
        }

        if (parametros.CantidadHabitaciones > parametros.NumeroHuespedes)
        {
            throw new ArgumentException("La cantidad de habitaciones no puede ser superior al número de huéspedes.");
        }

        // 2. Verificar sede
        var sede = await _contexto.Sedes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == parametros.SedeId, cancelacion);

        if (sede == null)
        {
            throw new KeyNotFoundException($"No se encontró la sede con identificador {parametros.SedeId}.");
        }

        // 3. Obtener categoría
        CategoriaHabitacion? categoria = null;
        if (parametros.CategoriaId.HasValue)
        {
            categoria = await _contexto.CategoriasHabitacion
                .AsNoTracking()
                .Include(c => c.Sede)
                .FirstOrDefaultAsync(c => c.Id == parametros.CategoriaId.Value && c.SedeId == parametros.SedeId, cancelacion);
        }
        else if (!string.IsNullOrWhiteSpace(parametros.TipoHabitacion))
        {
            var tipoNormalizado = parametros.TipoHabitacion.Trim().ToLower();
            categoria = await _contexto.CategoriasHabitacion
                .AsNoTracking()
                .Include(c => c.Sede)
                .FirstOrDefaultAsync(c => c.SedeId == parametros.SedeId && c.Tipo.ToLower() == tipoNormalizado, cancelacion);
        }
        else
        {
            throw new ArgumentException("Debe especificar la categoría o tipo de habitación para cotizar.");
        }

        if (categoria == null)
        {
            throw new KeyNotFoundException("No se encontró la categoría de habitación especificada para la sede indicada.");
        }

        // 4. Capacidad máxima total admisible
        var capacidadTotal = parametros.CantidadHabitaciones * categoria.CapacidadMaxima;
        if (parametros.NumeroHuespedes > capacidadTotal)
        {
            throw new ArgumentException($"La capacidad máxima total de las {parametros.CantidadHabitaciones} habitaciones ({capacidadTotal} personas) es insuficiente para alojar a {parametros.NumeroHuespedes} huéspedes.");
        }

        // 5. Validar disponibilidad real en inventario
        var opcionesDisponibles = await _servicioDisponibilidad.ConsultarDisponibilidadAsync(new ConsultaDisponibilidadParametros
        {
            SedeId = parametros.SedeId,
            FechaEntrada = parametros.FechaEntrada,
            FechaSalida = parametros.FechaSalida,
            NumeroHuespedes = 1,
            TipoHabitacion = categoria.Tipo
        }, cancelacion);

        var disponibleCategoria = opcionesDisponibles.FirstOrDefault(o => o.TipoHabitacion == categoria.Tipo);
        var habitacionesLibres = disponibleCategoria?.HabitacionesDisponibles ?? 0;

        if (habitacionesLibres < parametros.CantidadHabitaciones)
        {
            throw new InvalidOperationException($"Disponibilidad insuficiente. Se solicitaron {parametros.CantidadHabitaciones} habitaciones pero solo hay {habitacionesLibres} disponibles para las fechas requeridas.");
        }

        // 6. Distribución equilibrada de huéspedes por habitación
        var baseHuespedes = parametros.NumeroHuespedes / parametros.CantidadHabitaciones;
        var residuoHuespedes = parametros.NumeroHuespedes % parametros.CantidadHabitaciones;

        var distribucionHuespedes = new List<int>();
        for (var i = 0; i < parametros.CantidadHabitaciones; i++)
        {
            var huespedesEnHabitacion = baseHuespedes + (i < residuoHuespedes ? 1 : 0);
            distribucionHuespedes.Add(huespedesEnHabitacion);
        }

        // 7. Cargar temporadas y tarifas de la categoría
        var temporadas = await _contexto.Temporadas
            .AsNoTracking()
            .ToListAsync(cancelacion);

        var tarifas = await _contexto.Tarifas
            .AsNoTracking()
            .Where(t => t.CategoriaHabitacionId == categoria.Id)
            .ToListAsync(cancelacion);

        var noches = parametros.FechaSalida.DayNumber - parametros.FechaEntrada.DayNumber;
        var desgloseNoches = new List<DesgloseNocheCotizacionDto>();
        var subtotalesPorHabitacion = new decimal[parametros.CantidadHabitaciones];

        for (var n = 0; n < noches; n++)
        {
            var fechaNoche = parametros.FechaEntrada.AddDays(n);
            var temporadaNoche = temporadas.FirstOrDefault(t => t.AplicaEnFecha(fechaNoche))
                                 ?? temporadas.First(t => t.EsTemporadaBase);

            var detalleHabitacionesNoche = new List<DesgloseHabitacionNocheDto>();
            decimal totalNoche = 0m;

            for (var h = 0; h < parametros.CantidadHabitaciones; h++)
            {
                var huespedesHab = distribucionHuespedes[h];
                var tarifa = tarifas.FirstOrDefault(t =>
                    t.TemporadaId == temporadaNoche.Id &&
                    t.NumeroHuespedes == huespedesHab);

                if (tarifa == null)
                {
                    throw new InvalidOperationException($"No existe una tarifa configurada para la categoría '{categoria.Tipo}', temporada '{temporadaNoche.Nombre}' y {huespedesHab} huéspedes.");
                }

                subtotalesPorHabitacion[h] += tarifa.PrecioPorNoche;
                totalNoche += tarifa.PrecioPorNoche;

                detalleHabitacionesNoche.Add(new DesgloseHabitacionNocheDto
                {
                    NumeroHabitacion = h + 1,
                    Huespedes = huespedesHab,
                    TarifaAplicada = tarifa.PrecioPorNoche
                });
            }

            desgloseNoches.Add(new DesgloseNocheCotizacionDto
            {
                Fecha = fechaNoche,
                Temporada = temporadaNoche.Nombre,
                TarifaTotalNoche = totalNoche,
                DetalleHabitaciones = detalleHabitacionesNoche
            });
        }

        var habitacionesDto = new List<DistribucionHabitacionCotizacionDto>();
        for (var h = 0; h < parametros.CantidadHabitaciones; h++)
        {
            habitacionesDto.Add(new DistribucionHabitacionCotizacionDto
            {
                NumeroHabitacion = h + 1,
                HuespedesAsignados = distribucionHuespedes[h],
                SubtotalHabitacion = subtotalesPorHabitacion[h]
            });
        }

        var totalEstadia = subtotalesPorHabitacion.Sum();

        return new CotizacionEstadiaDto
        {
            SedeId = sede.Id,
            Sede = sede.Ciudad,
            SedeNombre = sede.Nombre,
            CategoriaId = categoria.Id,
            TipoHabitacion = categoria.Tipo,
            CapacidadMaximaPorHabitacion = categoria.CapacidadMaxima,
            FechaEntrada = parametros.FechaEntrada,
            FechaSalida = parametros.FechaSalida,
            NumeroNoches = noches,
            NumeroHuespedes = parametros.NumeroHuespedes,
            CantidadHabitaciones = parametros.CantidadHabitaciones,
            Moneda = "COP",
            TotalEstadia = totalEstadia,
            Habitaciones = habitacionesDto,
            DesgloseNoches = desgloseNoches
        };
    }
}
