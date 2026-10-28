using Microsoft.EntityFrameworkCore;
using PlataformaReservasHotel.Api.Dominio;

namespace PlataformaReservasHotel.Api.Datos;

public class ContextoHotel : DbContext
{
    public ContextoHotel(DbContextOptions<ContextoHotel> opciones) : base(opciones)
    {
    }

    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<CategoriaHabitacion> CategoriasHabitacion => Set<CategoriaHabitacion>();
    public DbSet<Habitacion> Habitaciones => Set<Habitacion>();
    public DbSet<Ocupacion> Ocupaciones => Set<Ocupacion>();
    public DbSet<Temporada> Temporadas => Set<Temporada>();
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();

    protected override void OnModelCreating(ModelBuilder constructorModelos)
    {
        base.OnModelCreating(constructorModelos);

        constructorModelos.Entity<Sede>(entidad =>
        {
            entidad.ToTable("sedes");
            entidad.HasKey(s => s.Id);
            entidad.Property(s => s.Nombre).IsRequired().HasMaxLength(100);
            entidad.Property(s => s.Ciudad).IsRequired().HasMaxLength(100);
        });

        constructorModelos.Entity<CategoriaHabitacion>(entidad =>
        {
            entidad.ToTable("categorias_habitacion");
            entidad.HasKey(c => c.Id);
            entidad.Property(c => c.Tipo).IsRequired().HasMaxLength(50);
            entidad.Property(c => c.CapacidadMaxima).IsRequired();

            entidad.HasOne(c => c.Sede)
                   .WithMany(s => s.Categorias)
                   .HasForeignKey(c => c.SedeId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        constructorModelos.Entity<Habitacion>(entidad =>
        {
            entidad.ToTable("habitaciones");
            entidad.HasKey(h => h.Id);
            entidad.Property(h => h.Identificador).IsRequired().HasMaxLength(50);
            entidad.HasIndex(h => h.Identificador).IsUnique();

            entidad.HasOne(h => h.CategoriaHabitacion)
                   .WithMany(c => c.Habitaciones)
                   .HasForeignKey(h => h.CategoriaHabitacionId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        constructorModelos.Entity<Ocupacion>(entidad =>
        {
            entidad.ToTable("ocupaciones");
            entidad.HasKey(o => o.Id);
            entidad.Property(o => o.FechaEntrada).IsRequired();
            entidad.Property(o => o.FechaSalida).IsRequired();

            entidad.HasOne(o => o.Habitacion)
                   .WithMany(h => h.Ocupaciones)
                   .HasForeignKey(o => o.HabitacionId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        constructorModelos.Entity<Temporada>(entidad =>
        {
            entidad.ToTable("temporadas");
            entidad.HasKey(t => t.Id);
            entidad.Property(t => t.Nombre).IsRequired().HasMaxLength(100);
        });

        constructorModelos.Entity<Tarifa>(entidad =>
        {
            entidad.ToTable("tarifas");
            entidad.HasKey(t => t.Id);
            entidad.Property(t => t.PrecioPorNoche).HasColumnType("decimal(18,2)").IsRequired();
            entidad.Property(t => t.NumeroHuespedes).IsRequired();

            entidad.HasIndex(t => new { t.CategoriaHabitacionId, t.TemporadaId, t.NumeroHuespedes }).IsUnique();

            entidad.HasOne(t => t.CategoriaHabitacion)
                   .WithMany(c => c.Tarifas)
                   .HasForeignKey(t => t.CategoriaHabitacionId)
                   .OnDelete(DeleteBehavior.Cascade);

            entidad.HasOne(t => t.Temporada)
                   .WithMany(temp => temp.Tarifas)
                   .HasForeignKey(t => t.TemporadaId)
                   .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
