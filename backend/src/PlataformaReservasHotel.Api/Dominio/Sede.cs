namespace PlataformaReservasHotel.Api.Dominio;

public class Sede
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;

    public List<CategoriaHabitacion> Categorias { get; set; } = [];
}
