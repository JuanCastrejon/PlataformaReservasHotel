namespace PlataformaReservasHotel.Api.Dominio;

public class CategoriaHabitacion
{
    public int Id { get; set; }
    public int SedeId { get; set; }
    public Sede Sede { get; set; } = null!;
    public string Tipo { get; set; } = string.Empty;
    public int CapacidadMaxima { get; set; }

    public List<Habitacion> Habitaciones { get; set; } = [];
    public List<Tarifa> Tarifas { get; set; } = [];
}
