# NutritionAdvisor

NutritionAdvisor is a full-stack nutrition platform that combines:

- **ASP.NET Core API** for authentication, recipes, meal plans, subscriptions, and daily logs
- **Blazor WebAssembly frontend** for the user interface
- **FastAPI AI service** for AI-powered recommendations, meal plans, and coaching
- **PostgreSQL** for persistent storage

## Repository Structure

- `/NutritionAdvisor.API` – ASP.NET Core backend (JWT auth, Swagger, EF Core)
- `/NutritionAdvisor.Application` – application logic and use cases
- `/NutritionAdvisor.Domain` – domain entities and contracts
- `/NutritionAdvisor.Infrastructure` – persistence, repositories, integrations
- `/NutritionAdvisor.Tests` – .NET test project
- `/Frontend` – Blazor WebAssembly client
- `/NutritionAdvisor.AI` – Python FastAPI AI microservice
- `/docker-compose.yml` – local multi-service orchestration

## Main Features

- User registration and login (`/api/auth`)
- Recipe CRUD and filtering (`/api/recipes`)
- Ingredient search and management (`/api/ingredients`)
- Meal plan generation and management (`/api/mealplans`)
- Daily meal logging (`/api/dailylog`)
- Food preferences and allergies (`/api/foodpreferences`, `/api/allergies`)
- Subscription and payments integration (`/api/subscription`, `/api/payments`)
- AI endpoints for recommendations/coach/meal plans (`/api/ai/*`)

## Prerequisites

- **.NET SDK 10**
- **Python 3.11+** (3.12 works)
- **Docker + Docker Compose** (recommended for full stack)

## Environment Configuration

1. Copy the example:

```bash
cp .env.example .env
```

2. Fill required values in `.env`:
   - `Jwt__Key`
   - `GEMINI_API_KEY`
   - `Stripe__SecretKey`
   - `Stripe__WebhookSecret`

> Do not commit `.env` to source control.

## Quick Start (Docker Compose)

Run all services (Postgres + API + Frontend + AI):

```bash
docker compose up --build
```

Default local URLs:

- Frontend: `http://localhost:5210`
- API: `http://localhost:5066`
- AI service: `http://localhost:8000`
- PostgreSQL: `localhost:5433`

## Run Locally Without Docker

### 1) Start dependencies

You need a running PostgreSQL instance and valid `.env` values.

### 2) Run AI service

```bash
cd NutritionAdvisor.AI
python -m pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8000
```

### 3) Run API

```bash
dotnet run --project NutritionAdvisor.API
```

### 4) Run Frontend

```bash
dotnet run --project Frontend
```

## Testing

### .NET tests

```bash
dotnet test NutritionAdvisor.sln --configuration Release
```

### Python tests

```bash
cd NutritionAdvisor.AI
pytest
```

## API Documentation

When running in development, Swagger/OpenAPI is available from the API host:

- Swagger UI: `/swagger`
- OpenAPI: `/openapi/v1.json`

## Notes

- Local uploaded recipe files are served from `NutritionAdvisor.API/wwwroot/UploadedFiles`.
- The AI service uses Gemini configuration from environment variables.
