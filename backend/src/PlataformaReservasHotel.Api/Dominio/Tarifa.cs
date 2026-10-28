namespace PlataformaReservasHotel.Api.Dominio;

public class Tarifa
{
    public int Id { get; set; }
    public int CategoriaHabitacionId { get; set; }
    public CategoriaHabitacion CategoriaHabitacion { get; set; } = null!;
    public int TemporadaId { get; set; }
    public Temporada Temporada { get; set; } = null!;
    public int NumeroHuespedes { get; set; }
    public decimal PrecioPorNoche { get; set; }
}
