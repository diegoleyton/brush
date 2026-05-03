# Brush.Backend

Backend ASP.NET Core para Brush.

## Estructura inicial

- `src/Brush.Api`: API HTTP
- `src/Brush.Domain`: reglas y entidades de negocio
- `src/Brush.Infrastructure`: persistencia e integraciones

## Primeros pasos

```bash
dotnet restore Brush.Backend/Brush.Backend.slnx
dotnet build Brush.Backend/Brush.Backend.slnx
dotnet run --project Brush.Backend/src/Brush.Api
```

Endpoint inicial:

- `GET /health`
