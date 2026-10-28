using Microsoft.EntityFrameworkCore;
using PlataformaReservasHotel.Api.Datos;
using PlataformaReservasHotel.Api.Servicios;

var constructor = WebApplication.CreateBuilder(args);

var cadenaConexion = constructor.Configuration.GetConnectionString("HotelDb")
    ?? "Data Source=hotel.db";

constructor.Services.AddDbContext<ContextoHotel>(opciones =>
    opciones.UseSqlite(cadenaConexion));

constructor.Services.AddScoped<IServicioDisponibilidad, ServicioDisponibilidad>();
constructor.Services.AddScoped<IServicioCotizacion, ServicioCotizacion>();

constructor.Services.AddControllers();
constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddSwaggerGen();

var aplicacion = constructor.Build();

using (var alcance = aplicacion.Services.CreateScope())
{
    var contexto = alcance.ServiceProvider.GetRequiredService<ContextoHotel>();
    contexto.Database.EnsureCreated();
    InicializadorDatos.Inicializar(contexto);
}

if (aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseSwagger();
    aplicacion.UseSwaggerUI();
}

aplicacion.UseHttpsRedirection();

aplicacion.UseAuthorization();

aplicacion.MapControllers();

aplicacion.Run();
