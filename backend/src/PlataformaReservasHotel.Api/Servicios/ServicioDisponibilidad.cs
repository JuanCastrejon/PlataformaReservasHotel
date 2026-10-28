using Microsoft.EntityFrameworkCore;
using PlataformaReservasHotel.Api.Datos;
using PlataformaReservasHotel.Api.Modelos;

namespace PlataformaReservasHotel.Api.Servicios;

public interface IServicioDisponibilidad
{
    Task<List<CategoriaDisponibilidadDto>> ConsultarDisponibilidadAsync(
        ConsultaDisponibilidadParametros parametros,
        CancellationToken cancelacion = default);
}

public class ServicioDisponibilidad : IServicioDisponibilidad
{
    private readonly ContextoHotel _contexto;

    public ServicioDisponibilidad(ContextoHotel contexto)
    {
        _contexto = contexto;
    }

    public async Task<List<CategoriaDisponibilidadDto>> ConsultarDisponibilidadAsync(
        ConsultaDisponibilidadParametros parametros,
        CancellationToken cancelacion = default)
    {
        if (parametros.FechaEntrada >= parametros.FechaSalida)
        {
            throw new ArgumentException("La fecha de entrada debe ser estrictamente anterior a la fecha de salida.");
        }

        if (parametros.NumeroHuespedes <= 0)
        {
            throw new ArgumentException("El número de huéspedes debe ser un entero positivo mayor que cero.");
        }

        var sedeExiste = await _contexto.Sedes
            .AsNoTracking()
            .AnyAsync(s => s.Id == parametros.SedeId, cancelacion);

        if (!sedeExiste)
        {
            throw new KeyNotFoundException($"No se encontró la sede con identificador {parametros.SedeId}.");
        }

        var consulta = _contexto.CategoriasHabitacion
            .AsNoTracking()
            .Include(c => c.Sede)
            .Include(c => c.Habitaciones)
                .ThenInclude(h => h.Ocupaciones)
            .Where(c => c.SedeId == parametros.SedeId && c.CapacidadMaxima >= parametros.NumeroHuespedes);

        if (!string.IsNullOrWhiteSpace(parametros.TipoHabitacion))
        {
            var tipoNormalizado = parametros.TipoHabitacion.Trim().ToLower();
            consulta = consulta.Where(c => c.Tipo.ToLower() == tipoNormalizado);
        }

        var categorias = await consulta.ToListAsync(cancelacion);

        var resultados = new List<CategoriaDisponibilidadDto>();

        foreach (var categoria in categorias)
        {
            var habitacionesDisponibles = categoria.Habitaciones.Count(habitacion =>
                !habitacion.Ocupaciones.Any(ocupacion =>
                    parametros.FechaEntrada < ocupacion.FechaSalida &&
                    parametros.FechaSalida > ocupacion.FechaEntrada));

            resultados.Add(new CategoriaDisponibilidadDto
            {
                SedeId = categoria.SedeId,
                Sede = categoria.Sede.Ciudad,
                SedeNombre = categoria.Sede.Nombre,
                TipoHabitacion = categoria.Tipo,
                CapacidadMaxima = categoria.CapacidadMaxima,
                HabitacionesDisponibles = habitacionesDisponibles
            });
        }

        return resultados;
    }
}
