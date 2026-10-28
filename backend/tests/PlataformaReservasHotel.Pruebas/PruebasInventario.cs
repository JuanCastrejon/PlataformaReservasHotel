using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlataformaReservasHotel.Api.Datos;
using PlataformaReservasHotel.Api.Dominio;

namespace PlataformaReservasHotel.Pruebas;

public class PruebasInventario : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly ContextoHotel _contexto;

    public PruebasInventario()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<ContextoHotel>()
            .UseSqlite(_conexion)
            .Options;

        _contexto = new ContextoHotel(opciones);
        _contexto.Database.EnsureCreated();
        InicializadorDatos.Inicializar(_contexto);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }

    [Fact]
    public void Inventario_DebeContenerExactamente108Habitaciones()
    {
        var totalHabitaciones = _contexto.Habitaciones.Count();
        Assert.Equal(108, totalHabitaciones);
    }

    [Fact]
    public void Inventario_DebeContenerCuatroSedes()
    {
        var sedes = _contexto.Sedes.Select(s => s.Ciudad).OrderBy(c => c).ToList();
        var sedesEsperadas = new List<string> { "Barranquilla", "Bogotá", "Cali", "Cartagena" };

        Assert.Equal(4, sedes.Count);
        Assert.Equal(sedesEsperadas, sedes);
    }

    [Fact]
    public void Inventario_DebeContenerNueveCombinacionesSedeTipo()
    {
        var totalCategorias = _contexto.CategoriasHabitacion.Count();
        Assert.Equal(9, totalCategorias);
    }

    [Fact]
    public void Inventario_Barranquilla_DebeTenerTreintaEstandarYTresPremiumConCapacidadCuatro()
    {
        var categorias = _contexto.CategoriasHabitacion
            .Include(c => c.Habitaciones)
            .Include(c => c.Sede)
            .Where(c => c.Sede.Ciudad == "Barranquilla")
            .ToList();

        var estandar = categorias.Single(c => c.Tipo == "Estándar");
        Assert.Equal(30, estandar.Habitaciones.Count);
        Assert.Equal(4, estandar.CapacidadMaxima);

        var premium = categorias.Single(c => c.Tipo == "Premium");
        Assert.Equal(3, premium.Habitaciones.Count);
        Assert.Equal(4, premium.CapacidadMaxima);
    }

    [Fact]
    public void Inventario_Cali_DebeTenerVeintePremiumYDosVipConCapacidadSeis()
    {
        var categorias = _contexto.CategoriasHabitacion
            .Include(c => c.Habitaciones)
            .Include(c => c.Sede)
            .Where(c => c.Sede.Ciudad == "Cali")
            .ToList();

        var premium = categorias.Single(c => c.Tipo == "Premium");
        Assert.Equal(20, premium.Habitaciones.Count);
        Assert.Equal(6, premium.CapacidadMaxima);

        var vip = categorias.Single(c => c.Tipo == "VIP");
        Assert.Equal(2, vip.Habitaciones.Count);
        Assert.Equal(6, vip.CapacidadMaxima);
    }

    [Fact]
    public void Inventario_Cartagena_DebeTenerDiezEstandarYUnaPremiumConCapacidadOcho()
    {
        var categorias = _contexto.CategoriasHabitacion
            .Include(c => c.Habitaciones)
            .Include(c => c.Sede)
            .Where(c => c.Sede.Ciudad == "Cartagena")
            .ToList();

        var estandar = categorias.Single(c => c.Tipo == "Estándar");
        Assert.Equal(10, estandar.Habitaciones.Count);
        Assert.Equal(8, estandar.CapacidadMaxima);

        var premium = categorias.Single(c => c.Tipo == "Premium");
        Assert.Single(premium.Habitaciones);
        Assert.Equal(8, premium.CapacidadMaxima);
    }

    [Fact]
    public void Inventario_Bogota_DebeTenerVeinteEstandarVeintePremiumYDosVipConCapacidadSeis()
    {
        var categorias = _contexto.CategoriasHabitacion
            .Include(c => c.Habitaciones)
            .Include(c => c.Sede)
            .Where(c => c.Sede.Ciudad == "Bogotá")
            .ToList();

        var estandar = categorias.Single(c => c.Tipo == "Estándar");
        Assert.Equal(20, estandar.Habitaciones.Count);
        Assert.Equal(6, estandar.CapacidadMaxima);

        var premium = categorias.Single(c => c.Tipo == "Premium");
        Assert.Equal(20, premium.Habitaciones.Count);
        Assert.Equal(6, premium.CapacidadMaxima);

        var vip = categorias.Single(c => c.Tipo == "VIP");
        Assert.Equal(2, vip.Habitaciones.Count);
        Assert.Equal(6, vip.CapacidadMaxima);
    }

    [Fact]
    public void Inventario_TodosLosIdentificadoresDeHabitacionSonUnicosYDeterministas()
    {
        var identificadores = _contexto.Habitaciones
            .Select(h => h.Identificador)
            .ToList();

        Assert.Equal(108, identificadores.Count);
        Assert.Equal(108, identificadores.Distinct().Count());
        Assert.All(identificadores, id => Assert.False(string.IsNullOrWhiteSpace(id)));
    }

    [Fact]
    public void Inventario_InicializacionEsIdempotente_NoDuplicaHabitacionesNiSedes()
    {
        InicializadorDatos.Inicializar(_contexto);

        Assert.Equal(4, _contexto.Sedes.Count());
        Assert.Equal(9, _contexto.CategoriasHabitacion.Count());
        Assert.Equal(108, _contexto.Habitaciones.Count());
    }

    [Fact]
    public void Inventario_RelacionesDeNavegacionEstanCorrectamenteVinculadas()
    {
        var habitacion = _contexto.Habitaciones
            .Include(h => h.CategoriaHabitacion)
            .ThenInclude(c => c.Sede)
            .FirstOrDefault(h => h.Identificador == "BAQ-EST-001");

        Assert.NotNull(habitacion);
        Assert.NotNull(habitacion.CategoriaHabitacion);
        Assert.Equal("Estándar", habitacion.CategoriaHabitacion.Tipo);
        Assert.NotNull(habitacion.CategoriaHabitacion.Sede);
        Assert.Equal("Barranquilla", habitacion.CategoriaHabitacion.Sede.Ciudad);
    }
}