namespace PlataformaReservasHotel.Api.Dominio;

public class Temporada
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public bool EsTemporadaBase { get; set; }

    public List<Tarifa> Tarifas { get; set; } = [];

    public bool AplicaEnFecha(DateOnly fecha)
    {
        if (EsTemporadaBase)
        {
            return false;
        }

        return FechaInicio.HasValue && FechaFin.HasValue &&
               fecha >= FechaInicio.Value && fecha <= FechaFin.Value;
    }
}
