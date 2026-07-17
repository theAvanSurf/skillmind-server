# SkillMind — Server

The backend for **SkillMind**, an online learning platform where professors publish courses (with seasons/lessons, exams, and live streaming) and students enroll, learn, get certified, and pay via Stripe. Built as a set of **Docker Compose–orchestrated microservices** behind a single API Gateway.

The corresponding front end lives in [skillmind-client](https://github.com/theAvanSurf/skillmind-client) (Next.js).

## Architecture

```
                         ┌───────────────────┐
   Client (Next.js) ───▶ │    API Gateway     │  (NestJS, :3000)
                         └─────────┬─────────┘
                                   │
              ┌────────────────────┼──────────────────────┐
              ▼                    ▼                       ▼
     ┌────────────────┐   ┌───────────────────┐   ┌──────────────────────┐
     │  Core service   │   │ Recommendation svc │   │ Notification service │
     │ (.NET, :5001)   │   │ (FastAPI, :8000)   │   │   (NestJS, :3001)    │
     └────────┬────────┘   └─────────┬──────────┘   └──────────┬───────────┘
              │                      │                          │
              ▼                      ▼                          ▼
        PostgreSQL              PostgreSQL                 SMTP / Push
              │                      │
              └──────────┬───────────┘
                          ▼
                    Redis · Kafka
```

- **API Gateway** (`api-gateway/`) — NestJS. Single entry point for the client: auth, courses, payments, profiles, sessions, media upload, professor endpoints, and recommendations proxying. Uses Redis for caching, Cloudinary for media, and talks to the Core service and Recommendation service internally.
- **Core** (`Core/SkillmindCore/`) — the main .NET Web API, structured as Clean Architecture:
  - `SkillMind.Core.Domain` — entities (Course, Season, Lesson, Enrollment, Exam, ExamAttempt, Certificate, LiveSession, ProfessorProfile, Profiles, etc.) and domain interfaces
  - `SkillMind.Core.Application` — business logic, services, DTOs
  - `SkillMind.Infrastructure.Persistence` — EF Core + PostgreSQL
  - `SkillMind.Infrastructure.Identity` — auth (JWT-based)
  - `SkillMind.Infrastructure.Shared` — cross-cutting services (email, Stripe, Google/YouTube OAuth for live streaming)
  - `SkillMind.WebAPI` — the HTTP entry point
- **Notification service** (`notification-service/`) — NestJS. Consumes events from Kafka and sends emails (SMTP) and push notifications (Firebase).
- **Recommendation service** (`recommendation-service/`) — Python/FastAPI. Consumes Kafka events and serves course recommendations, backed by its own Postgres access and Redis.
- **Redis** — shared cache.
- **Kafka + Zookeeper** — event bus connecting Core, the notification service, and the recommendation service (e.g. enrollment/progress events trigger notifications and feed the recommendation engine).

## Tech stack

| Component | Stack |
|---|---|
| API Gateway | NestJS, TypeORM, Redis cache, Cloudinary, Swagger |
| Core | .NET 10 (preview), ASP.NET Core Web API, EF Core, PostgreSQL, JWT auth, Stripe, Google OAuth |
| Notification service | NestJS, Kafka consumer, Nodemailer/SMTP, Firebase (push) |
| Recommendation service | FastAPI, SQLAlchemy (async), asyncpg, Redis, kafka-python |
| Infra | Docker Compose, Redis, Kafka, Zookeeper, PostgreSQL (external, e.g. Render) |

## Getting started

Full setup instructions (env vars, Docker Compose, running EF Core migrations inside the container) are in **[`GUIDE.md`](./GUIDE.md)**. Summary:

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- A PostgreSQL database (e.g. a free instance on Render.com)

### Setup

```bash
git clone https://github.com/theAvanSurf/skillmind-server.git
cd skillmind-server
cp .env.example .env
# edit .env with your own database, JWT, Cloudinary, and email credentials
docker-compose up --build
```

This starts Redis, Zookeeper, Kafka, the Core service, the API Gateway, the notification service, and the recommendation service.

### Access points

| Service | URL |
|---|---|
| API Gateway | http://localhost:3000 |
| Core service | http://localhost:5001 (container port 8080) |
| Notification service | http://localhost:3001 |
| Recommendation service | http://localhost:8000 |
| Redis | localhost:6379 |
| Kafka | localhost:29092 (internal), localhost:9092 (host) |

### Database migrations

EF Core migrations must be run inside the running `skillmind-core` container so they pick up the container's environment/network config — see the exact commands (Persistence and Identity contexts) in [`GUIDE.md`](./GUIDE.md#database-migrations).

## Repository layout

```
skillmind-server/
├── api-gateway/                 # NestJS API Gateway
├── Core/SkillmindCore/          # .NET Core service (Clean Architecture)
├── notification-service/        # NestJS notification service (email/push via Kafka)
├── recommendation-service/      # FastAPI recommendation engine
├── analytics/                   # (analytics-related assets)
├── docker-compose.yml           # Orchestrates all services
├── GUIDE.md                     # Detailed setup & migration guide
└── PROFESSOR_FEATURES_PLAN.md   # Planning doc for professor-facing features
```

## Notes

- No license file is currently included in the repository.
- `PROFESSOR_FEATURES_PLAN.md` documents planned/in-progress professor-facing functionality (exams, live streaming, earnings) and is worth reading alongside the code for context on where things are headed.
