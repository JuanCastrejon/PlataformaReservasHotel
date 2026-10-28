namespace PlataformaReservasHotel.Api.Modelos;

public class CategoriaDisponibilidadDto
{
    public int SedeId { get; set; }
    public string Sede { get; set; } = string.Empty;
    public string SedeNombre { get; set; } = string.Empty;
    public string TipoHabitacion { get; set; } = string.Empty;
    public int CapacidadMaxima { get; set; }
    public int HabitacionesDisponibles { get; set; }
}

public class ConsultaDisponibilidadParametros
{
    public int SedeId { get; set; }
    public DateOnly FechaEntrada { get; set; }
    public DateOnly FechaSalida { get; set; }
    public int NumeroHuespedes { get; set; }
    public string? TipoHabitacion { get; set; }
}
