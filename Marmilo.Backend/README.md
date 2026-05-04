# Marmilo Backend

Backend ASP.NET Core para el ecosistema Marmilo.

Por ahora, `Brush` sigue existiendo como nombre temporal de la app de lavado de dientes dentro del código, pero el producto y la plataforma se refieren a `Marmilo`.

## Estructura inicial

- `src/Marmilo.Api`: API HTTP de Marmilo
- `src/Marmilo.Domain`: reglas y entidades de negocio
- `src/Marmilo.Infrastructure`: persistencia e integraciones

## Primeros pasos

```bash
dotnet restore Marmilo.Backend/Marmilo.Backend.slnx
dotnet build Marmilo.Backend/Marmilo.Backend.slnx
dotnet run --project Marmilo.Backend/src/Marmilo.Api
```

Endpoint inicial:

- `GET /health`
