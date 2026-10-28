using Microsoft.AspNetCore.Mvc;
using PlataformaReservasHotel.Api.Modelos;
using PlataformaReservasHotel.Api.Servicios;

namespace PlataformaReservasHotel.Api.Controladores;

[ApiController]
[Route("api/habitaciones")]
public class DisponibilidadController : ControllerBase
{
    private readonly IServicioDisponibilidad _servicioDisponibilidad;
    private readonly IServicioCotizacion _servicioCotizacion;

    public DisponibilidadController(
        IServicioDisponibilidad servicioDisponibilidad,
        IServicioCotizacion servicioCotizacion)
    {
        _servicioDisponibilidad = servicioDisponibilidad;
        _servicioCotizacion = servicioCotizacion;
    }

    [HttpGet("disponibles")]
    public async Task<IActionResult> ObtenerHabitacionesDisponibles(
        [FromQuery] int? sedeId,
        [FromQuery] DateOnly? fechaEntrada,
        [FromQuery] DateOnly? fechaSalida,
        [FromQuery] int? numeroHuespedes,
        [FromQuery] string? tipoHabitacion,
        CancellationToken cancelacion)
    {
        if (!sedeId.HasValue)
        {
            return BadRequest(new { mensaje = "El parámetro sedeId es obligatorio." });
        }

        if (!fechaEntrada.HasValue || !fechaSalida.HasValue)
        {
            return BadRequest(new { mensaje = "Las fechas de entrada y salida son obligatorias." });
        }

        if (fechaEntrada.Value >= fechaSalida.Value)
        {
            return BadRequest(new { mensaje = "La fecha de entrada debe ser estrictamente anterior a la fecha de salida." });
        }

        if (!numeroHuespedes.HasValue || numeroHuespedes.Value <= 0)
        {
            return BadRequest(new { mensaje = "El número de huéspedes debe ser un entero positivo mayor que cero." });
        }

        try
        {
            var parametros = new ConsultaDisponibilidadParametros
            {
                SedeId = sedeId.Value,
                FechaEntrada = fechaEntrada.Value,
                FechaSalida = fechaSalida.Value,
                NumeroHuespedes = numeroHuespedes.Value,
                TipoHabitacion = tipoHabitacion
            };

            var categoriasDisponibles = await _servicioDisponibilidad
                .ConsultarDisponibilidadAsync(parametros, cancelacion);

            return Ok(categoriasDisponibles);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("cotizar")]
    public async Task<IActionResult> CotizarEstadia(
        [FromQuery] int? sedeId,
        [FromQuery] int? categoriaId,
        [FromQuery] string? tipoHabitacion,
        [FromQuery] DateOnly? fechaEntrada,
        [FromQuery] DateOnly? fechaSalida,
        [FromQuery] int? numeroHuespedes,
        [FromQuery] int? cantidadHabitaciones,
        CancellationToken cancelacion)
    {
        if (!sedeId.HasValue)
        {
            return BadRequest(new { codigo = "PARAMETROS_INVALIDOS", mensaje = "El parámetro sedeId es obligatorio." });
        }

        if (!fechaEntrada.HasValue || !fechaSalida.HasValue)
        {
            return BadRequest(new { codigo = "PARAMETROS_INVALIDOS", mensaje = "Las fechas de entrada y salida son obligatorias." });
        }

        if (fechaEntrada.Value >= fechaSalida.Value)
        {
            return BadRequest(new { codigo = "PARAMETROS_INVALIDOS", mensaje = "La fecha de entrada debe ser estrictamente anterior a la fecha de salida." });
        }

        if (!numeroHuespedes.HasValue || numeroHuespedes.Value <= 0)
        {
            return BadRequest(new { codigo = "PARAMETROS_INVALIDOS", mensaje = "El número de huéspedes debe ser un entero positivo mayor que cero." });
        }

        var cantHabitaciones = cantidadHabitaciones.GetValueOrDefault(1);
        if (cantHabitaciones <= 0)
        {
            return BadRequest(new { codigo = "PARAMETROS_INVALIDOS", mensaje = "La cantidad de habitaciones debe ser un entero positivo mayor que cero." });
        }

        try
        {
            var parametros = new ConsultaCotizacionParametros
            {
                SedeId = sedeId.Value,
                CategoriaId = categoriaId,
                TipoHabitacion = tipoHabitacion,
                FechaEntrada = fechaEntrada.Value,
                FechaSalida = fechaSalida.Value,
                NumeroHuespedes = numeroHuespedes.Value,
                CantidadHabitaciones = cantHabitaciones
            };

            var cotizacion = await _servicioCotizacion.CotizarEstadiaAsync(parametros, cancelacion);
            return Ok(cotizacion);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { codigo = "RECURSO_NO_ENCONTRADO", mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { codigo = "SOLICITUD_INVALIDA", mensaje = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Disponibilidad insuficiente"))
        {
            return Conflict(new { codigo = "DISPONIBILIDAD_INSUFICIENTE", mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { codigo = "TARIFA_NO_DISPONIBLE", mensaje = ex.Message });
        }
    }
}
