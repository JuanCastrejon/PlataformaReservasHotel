using Microsoft.EntityFrameworkCore;
using PlataformaReservasHotel.Api.Dominio;

namespace PlataformaReservasHotel.Api.Datos;

public static class InicializadorDatos
{
    public static void Inicializar(ContextoHotel contexto)
    {
        AsegurarEsquema(contexto);
        InicializarInventario(contexto);
        InicializarTemporadasYTarifas(contexto);
    }

    private static void AsegurarEsquema(ContextoHotel contexto)
    {
        contexto.Database.EnsureCreated();

        contexto.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""ocupaciones"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ocupaciones"" PRIMARY KEY AUTOINCREMENT,
                ""HabitacionId"" INTEGER NOT NULL,
                ""FechaEntrada"" TEXT NOT NULL,
                ""FechaSalida"" TEXT NOT NULL,
                CONSTRAINT ""FK_ocupaciones_habitaciones_HabitacionId"" FOREIGN KEY (""HabitacionId"") REFERENCES ""habitaciones"" (""Id"") ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS ""temporadas"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_temporadas"" PRIMARY KEY AUTOINCREMENT,
                ""Nombre"" TEXT NOT NULL,
                ""FechaInicio"" TEXT NULL,
                ""FechaFin"" TEXT NULL,
                ""EsTemporadaBase"" INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ""tarifas"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_tarifas"" PRIMARY KEY AUTOINCREMENT,
                ""CategoriaHabitacionId"" INTEGER NOT NULL,
                ""TemporadaId"" INTEGER NOT NULL,
                ""NumeroHuespedes"" INTEGER NOT NULL,
                ""PrecioPorNoche"" TEXT NOT NULL,
                CONSTRAINT ""FK_tarifas_categorias_habitacion_CategoriaHabitacionId"" FOREIGN KEY (""CategoriaHabitacionId"") REFERENCES ""categorias_habitacion"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_tarifas_temporadas_TemporadaId"" FOREIGN KEY (""TemporadaId"") REFERENCES ""temporadas"" (""Id"") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_tarifas_CategoriaHabitacionId_TemporadaId_NumeroHuespedes"" ON ""tarifas"" (""CategoriaHabitacionId"", ""TemporadaId"", ""NumeroHuespedes"");
        ");
    }

    private static void InicializarInventario(ContextoHotel contexto)
    {
        if (contexto.Sedes.Any())
        {
            return;
        }

        var sedeBarranquilla = new Sede { Nombre = "Hotel Barranquilla Plaza", Ciudad = "Barranquilla" };
        var sedeCali = new Sede { Nombre = "Hotel Cali Real", Ciudad = "Cali" };
        var sedeCartagena = new Sede { Nombre = "Hotel Cartagena Caribe", Ciudad = "Cartagena" };
        var sedeBogota = new Sede { Nombre = "Hotel Bogotá Centro", Ciudad = "Bogotá" };

        contexto.Sedes.AddRange(sedeBarranquilla, sedeCali, sedeCartagena, sedeBogota);
        contexto.SaveChanges();

        var categorias = new List<(CategoriaHabitacion Categoria, string Prefijo, int Cantidad)>
        {
            // Barranquilla: 30 Estándar (cap 4) y 3 Premium (cap 4)
            (new CategoriaHabitacion { SedeId = sedeBarranquilla.Id, Tipo = "Estándar", CapacidadMaxima = 4 }, "BAQ-EST", 30),
            (new CategoriaHabitacion { SedeId = sedeBarranquilla.Id, Tipo = "Premium", CapacidadMaxima = 4 }, "BAQ-PRE", 3),

            // Cali: 20 Premium (cap 6) y 2 VIP (cap 6)
            (new CategoriaHabitacion { SedeId = sedeCali.Id, Tipo = "Premium", CapacidadMaxima = 6 }, "CAL-PRE", 20),
            (new CategoriaHabitacion { SedeId = sedeCali.Id, Tipo = "VIP", CapacidadMaxima = 6 }, "CAL-VIP", 2),

            // Cartagena: 10 Estándar (cap 8) y 1 Premium (cap 8)
            (new CategoriaHabitacion { SedeId = sedeCartagena.Id, Tipo = "Estándar", CapacidadMaxima = 8 }, "CTG-EST", 10),
            (new CategoriaHabitacion { SedeId = sedeCartagena.Id, Tipo = "Premium", CapacidadMaxima = 8 }, "CTG-PRE", 1),

            // Bogotá: 20 Estándar (cap 6), 20 Premium (cap 6) y 2 VIP (cap 6)
            (new CategoriaHabitacion { SedeId = sedeBogota.Id, Tipo = "Estándar", CapacidadMaxima = 6 }, "BOG-EST", 20),
            (new CategoriaHabitacion { SedeId = sedeBogota.Id, Tipo = "Premium", CapacidadMaxima = 6 }, "BOG-PRE", 20),
            (new CategoriaHabitacion { SedeId = sedeBogota.Id, Tipo = "VIP", CapacidadMaxima = 6 }, "BOG-VIP", 2)
        };

        foreach (var (categoria, prefijo, cantidad) in categorias)
        {
            contexto.CategoriasHabitacion.Add(categoria);
            contexto.SaveChanges();

            var habitaciones = new List<Habitacion>();
            for (var i = 1; i <= cantidad; i++)
            {
                habitaciones.Add(new Habitacion
                {
                    CategoriaHabitacionId = categoria.Id,
                    Identificador = $"{prefijo}-{i:D3}"
                });
            }
            contexto.Habitaciones.AddRange(habitaciones);
        }

        contexto.SaveChanges();
    }

    private static void InicializarTemporadasYTarifas(ContextoHotel contexto)
    {
        if (contexto.Temporadas.Any())
        {
            return;
        }

        var temporadaBaja = new Temporada
        {
            Nombre = "Temporada Baja",
            EsTemporadaBase = true,
            FechaInicio = null,
            FechaFin = null
        };

        var temporadaAltaMitadAno = new Temporada
        {
            Nombre = "Temporada Alta - Mitad de Año",
            EsTemporadaBase = false,
            FechaInicio = new DateOnly(2026, 6, 15),
            FechaFin = new DateOnly(2026, 7, 15)
        };

        var temporadaAltaFinAno = new Temporada
        {
            Nombre = "Temporada Alta - Fin de Año",
            EsTemporadaBase = false,
            FechaInicio = new DateOnly(2026, 12, 15),
            FechaFin = new DateOnly(2027, 1, 15)
        };

        contexto.Temporadas.AddRange(temporadaBaja, temporadaAltaMitadAno, temporadaAltaFinAno);
        contexto.SaveChanges();

        var categorias = contexto.CategoriasHabitacion
            .Include(c => c.Sede)
            .ToList();

        var tarifas = new List<Tarifa>();

        foreach (var cat in categorias)
        {
            decimal ajusteSede = cat.Sede.Ciudad switch
            {
                "Bogotá" => 20000m,
                "Cartagena" => 30000m,
                "Cali" => 10000m,
                _ => 0m
            };

            (decimal baseTipo, decimal factorPersona) = cat.Tipo switch
            {
                "Estándar" => (120000m, 20000m),
                "Premium" => (180000m, 30000m),
                "VIP" => (280000m, 50000m),
                _ => (100000m, 15000m)
            };

            for (var g = 1; g <= cat.CapacidadMaxima; g++)
            {
                decimal precioBaja = baseTipo + ajusteSede + (g * factorPersona);
                decimal precioAlta = decimal.Round(precioBaja * 1.40m, 2);

                // Tarifa en Temporada Baja
                tarifas.Add(new Tarifa
                {
                    CategoriaHabitacionId = cat.Id,
                    TemporadaId = temporadaBaja.Id,
                    NumeroHuespedes = g,
                    PrecioPorNoche = precioBaja
                });

                // Tarifa en Temporada Alta - Mitad de Año
                tarifas.Add(new Tarifa
                {
                    CategoriaHabitacionId = cat.Id,
                    TemporadaId = temporadaAltaMitadAno.Id,
                    NumeroHuespedes = g,
                    PrecioPorNoche = precioAlta
                });

                // Tarifa en Temporada Alta - Fin de Año
                tarifas.Add(new Tarifa
                {
                    CategoriaHabitacionId = cat.Id,
                    TemporadaId = temporadaAltaFinAno.Id,
                    NumeroHuespedes = g,
                    PrecioPorNoche = precioAlta
                });
            }
        }

        contexto.Tarifas.AddRange(tarifas);
        contexto.SaveChanges();
    }
}
