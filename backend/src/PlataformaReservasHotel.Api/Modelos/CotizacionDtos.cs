namespace PlataformaReservasHotel.Api.Modelos;

public class ConsultaCotizacionParametros
{
    public int SedeId { get; set; }
    public int? CategoriaId { get; set; }
    public string? TipoHabitacion { get; set; }
    public DateOnly FechaEntrada { get; set; }
    public DateOnly FechaSalida { get; set; }
    public int NumeroHuespedes { get; set; }
    public int CantidadHabitaciones { get; set; } = 1;
}

public class CotizacionEstadiaDto
{
    public int SedeId { get; set; }
    public string Sede { get; set; } = string.Empty;
    public string SedeNombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public string TipoHabitacion { get; set; } = string.Empty;
    public int CapacidadMaximaPorHabitacion { get; set; }
    public DateOnly FechaEntrada { get; set; }
    public DateOnly FechaSalida { get; set; }
    public int NumeroNoches { get; set; }
    public int NumeroHuespedes { get; set; }
    public int CantidadHabitaciones { get; set; }
    public string Moneda { get; set; } = "COP";
    public decimal TotalEstadia { get; set; }
    public List<DistribucionHabitacionCotizacionDto> Habitaciones { get; set; } = [];
    public List<DesgloseNocheCotizacionDto> DesgloseNoches { get; set; } = [];
}

public class DistribucionHabitacionCotizacionDto
{
    public int NumeroHabitacion { get; set; }
    public int HuespedesAsignados { get; set; }
    public decimal SubtotalHabitacion { get; set; }
}

public class DesgloseNocheCotizacionDto
{
    public DateOnly Fecha { get; set; }
    public string Temporada { get; set; } = string.Empty;
    public decimal TarifaTotalNoche { get; set; }
    public List<DesgloseHabitacionNocheDto> DetalleHabitaciones { get; set; } = [];
}

public class DesgloseHabitacionNocheDto
{
    public int NumeroHabitacion { get; set; }
    public int Huespedes { get; set; }
    public decimal TarifaAplicada { get; set; }
}
