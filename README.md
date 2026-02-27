# Matamed Backend

ASP.NET Core 8.0 REST API for the NEX Health Intelligence platform.

## Local Development

```bash
docker compose up --build
```

API available at `http://localhost:8080`  
Swagger UI at `http://localhost:8080/swagger`  
Health check at `http://localhost:8080/health`

## Railway Deployment

1. Create a new project on Railway
2. Add a **PostgreSQL** service
3. Connect this repo — Railway auto-detects the `Dockerfile`
4. Set the `DATABASE_URL` variable (auto-populated if you link the Postgres service)
5. Railway uses the `PORT` env var automatically

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `PORT` | HTTP listen port (set by Railway) | `8080` |
| `DATABASE_URL` | PostgreSQL connection string | — |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |

## API Documentation

All endpoints are under `/api`. Full OpenAPI spec available at `/swagger`.
