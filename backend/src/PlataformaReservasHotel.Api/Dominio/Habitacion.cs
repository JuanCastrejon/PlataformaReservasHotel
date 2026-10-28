namespace PlataformaReservasHotel.Api.Dominio;

public class Habitacion
{
    public int Id { get; set; }
    public string Identificador { get; set; } = string.Empty;
    public int CategoriaHabitacionId { get; set; }
    public CategoriaHabitacion CategoriaHabitacion { get; set; } = null!;

    public List<Ocupacion> Ocupaciones { get; set; } = [];
}
