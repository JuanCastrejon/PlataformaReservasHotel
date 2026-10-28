namespace PlataformaReservasHotel.Api.Dominio;

public class Ocupacion
{
    public int Id { get; set; }
    public int HabitacionId { get; set; }
    public Habitacion Habitacion { get; set; } = null!;
    public DateOnly FechaEntrada { get; set; }
    public DateOnly FechaSalida { get; set; }
}
