# Plataforma de Reservas Hoteleras

Consulta de disponibilidad de habitaciones y cotización de tarifas por estancia según sede, fechas y número de huéspedes.

El alcance comprende la API HTTP, la persistencia en SQLite, el cálculo de tarifas por temporada y la interfaz web de consulta. No incluye creación de reservas, pagos, autenticación ni administración del catálogo.

## Tecnologías

- **Backend:** .NET 8, ASP.NET Core Web API, C# 12.
- **Persistencia:** Entity Framework Core 8 con proveedor SQLite.
- **Pruebas:** xUnit.
- **Frontend:** React 19, TypeScript, Vite 6, CSS nativo.

## Requisitos

- .NET 8 SDK.
- Node.js 18 o superior y npm.

## Ejecución

### Backend

Desde la raíz del repositorio:

```powershell
dotnet restore backend/PlataformaReservasHotel.sln
dotnet run --project backend/src/PlataformaReservasHotel.Api
```

- API disponible en: `http://localhost:5287`
- Documentación OpenAPI/Swagger en: `http://localhost:5287/swagger`

### Frontend

En otra terminal, desde la raíz del repositorio:

```powershell
npm --prefix frontend install
npm --prefix frontend run dev
```

- Aplicación web en: `http://localhost:5173`
- El servidor de desarrollo de Vite redirige las peticiones `/api/*` al backend (`http://localhost:5287`).

## Compilación y pruebas

### Backend

```powershell
dotnet build backend/PlataformaReservasHotel.sln
dotnet test backend/PlataformaReservasHotel.sln
```

### Frontend

```powershell
npm --prefix frontend run lint
npm --prefix frontend run build
```

## Documentación técnica

- [Especificación funcional y criterios de aceptación (SDD)](docs/SDD-busqueda-disponibilidad.md)
