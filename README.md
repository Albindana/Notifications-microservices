# Notification & Messaging Platform — .NET Microservices

A **Notification & Messaging Platform** built with a **microservices architecture**: three
ASP.NET Core services that are fully decoupled, each owning its own database, communicating
synchronously through an API Gateway (Ocelot) and asynchronously over RabbitMQ (MassTransit).

> **Framework note:** the original brief targeted .NET 8; this implementation targets **.NET 10**
> (the SDK installed on the build machine). Package versions were chosen accordingly, and
> **MassTransit is pinned to 8.x** / **MediatR to 12.x** — the last freely-licensed lines of each.

---

## Architecture

```
┌─────────────┐     HTTP      ┌──────────────┐
│   Client    │──────────────▶│  API Gateway │  (Ocelot, :5080)
└─────────────┘               │   (Ocelot)   │
                              └──────┬───────┘
                                     │ HTTP routing
                    ┌────────────────┼─────────────────┐
                    ▼                                   ▼
          ┌─────────────────┐               ┌────────────────────┐
          │   UserService   │               │ NotificationService │
          │ (Auth + Users)  │               │  (Notifications)    │
          │      :5001      │               │       :5002         │
          └────────┬────────┘               └─────────┬──────────┘
                   │ publishes events                  │ consumes events
                   ▼                                   │
          ┌─────────────────┐                          │
          │    RabbitMQ     │◀─────────────────────────┘
          │  (MassTransit)  │  :5672  (UI :15672)
          └─────────────────┘

Sync:  Client → Gateway → UserService / NotificationService (HTTP)
Async: UserService → RabbitMQ → NotificationService (fire-and-forget)
```

### Database isolation (the key microservices principle)

| Service              | Database                  | Owns                          |
|----------------------|---------------------------|-------------------------------|
| UserService          | `userservice.db`          | users, refresh tokens         |
| NotificationService  | `notificationservice.db`  | notification logs, templates  |
| ApiGateway           | none                      | routing only                  |

No service ever reads or writes another service's database.

---

## Tech Stack

| Concern        | Technology                                            |
|----------------|-------------------------------------------------------|
| Framework      | ASP.NET Core (.NET 10) Web API per service            |
| Message Bus    | RabbitMQ + MassTransit 8                              |
| API Gateway    | Ocelot                                                |
| ORM            | Entity Framework Core 10                              |
| Database       | SQLite (dev) / SQL Server (prod) — auto-detected      |
| Auth           | ASP.NET Core Identity + JWT (UserService)             |
| Validation     | FluentValidation (MediatR pipeline behavior)          |
| CQRS           | MediatR                                               |
| Docs           | Swagger per service                                   |
| Orchestration  | Docker + Docker Compose                               |
| Testing        | xUnit + Moq + MassTransit in-memory test harness      |

---

## Solution structure

```
NotificationPlatform.slnx
├── services/
│   ├── UserService/            (.API / .Application / .Domain / .Infrastructure)
│   ├── NotificationService/    (.API / .Application / .Domain / .Infrastructure)
│   └── ApiGateway/             (.API)
├── shared/
│   └── Shared.Contracts/       # event records only — no business logic
├── tests/
│   ├── UserService.Tests/
│   └── NotificationService.Tests/
├── docker-compose.yml
├── docker-compose.override.yml
└── README.md
```

Each service follows a clean-architecture layering: **Domain** (entities) → **Application**
(CQRS handlers, interfaces, validators) → **Infrastructure** (EF Core, Identity, repositories,
email stub) → **API** (controllers, DI wiring, MassTransit).

---

## Events (in `Shared.Contracts`)

| Event                        | Published by                | Consumed by (NotificationService) |
|------------------------------|-----------------------------|-----------------------------------|
| `UserRegisteredEvent`        | register                    | `UserRegisteredConsumer` → Welcome |
| `PasswordResetRequestedEvent`| forgot-password             | `PasswordResetRequestedConsumer`  |
| `UserProfileUpdatedEvent`    | update profile              | `UserProfileUpdatedConsumer`      |

The email integration is a **stub** (`ConsoleEmailSender`) — it logs `[EMAIL STUB] ...` instead
of sending real mail, so the swappable integration point is obvious and no credentials are needed.

---

## API endpoints

### UserService (`/api/auth`, `/api/users`)
```
POST /api/auth/register          register, returns { accessToken, refreshToken, expiresAt }
POST /api/auth/login             login
POST /api/auth/refresh           rotate refresh token
POST /api/auth/logout            invalidate refresh token            [Authorized]
GET  /api/users/me               current profile                     [Authorized]
PUT  /api/users/me               update profile (publishes event)    [Authorized]
POST /api/users/forgot-password  request reset (publishes event)
POST /api/users/reset-password   confirm reset with token
```

### NotificationService (`/api/notifications`, `/api/templates`)
```
GET  /api/notifications              list all (admin)
GET  /api/notifications/user/{id}    list for a user
GET  /api/notifications/{id}         single notification
PUT  /api/notifications/{id}/retry   retry a failed notification
GET  /api/templates                  list templates (admin)
PUT  /api/templates/{id}             update template subject/body (admin)
```

All routes are reachable through the gateway at `http://localhost:5080` using the same paths.
`/api/users`, `/api/notifications` and `/api/templates` are protected by JWT validation at the gateway.

---

## Running with Docker (recommended)

```bash
docker compose up --build
```

Then:

| What                       | URL                                |
|----------------------------|------------------------------------|
| API Gateway                | http://localhost:5080              |
| UserService (direct)       | http://localhost:5001/swagger      |
| NotificationService (direct)| http://localhost:5002/swagger     |
| RabbitMQ management UI      | http://localhost:15672 (guest/guest)|

### End-to-end smoke test

```bash
# 1) Register a user through the gateway
curl -X POST http://localhost:5080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"jane@test.com","password":"Passw0rd","firstName":"Jane","lastName":"Doe"}'

# 2) Watch the NotificationService consume UserRegisteredEvent and "send" the welcome email
docker compose logs -f notificationservice
# → look for:  [EMAIL STUB] To: jane@test.com | Subject: Welcome to Notification Platform, Jane! | ...

# 3) Confirm the notification was logged (this route is JWT-protected at the gateway)
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"jane@test.com","password":"Passw0rd"}' | jq -r .accessToken)
curl http://localhost:5080/api/notifications/user/<userId> -H "Authorization: Bearer $TOKEN"
```

### Seed accounts (UserService, created on startup)
- `admin@notify.com` / `Admin123!`
- `user@notify.com` / `User123!`

NotificationService seeds 3 templates on startup: Welcome, PasswordReset, ProfileUpdate.

---

## Running locally (without Docker)

You still need RabbitMQ — the quickest way is just the broker container:

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Then run each service (each uses its own SQLite file and applies migrations on startup):

```bash
dotnet run --project services/UserService/UserService.API           # :5001 (see launchSettings)
dotnet run --project services/NotificationService/NotificationService.API
dotnet run --project services/ApiGateway/ApiGateway.API
```

The gateway loads `ocelot.json` (Docker service-name routes) by default and overrides hosts/ports
from `ocelot.Development.json` (localhost:5001/5002) when `ASPNETCORE_ENVIRONMENT=Development`.

---

## Tests

```bash
dotnet test
```

- **UserService.Tests** — register (publishes event, rejects duplicate), login (valid / wrong
  password), refresh-token rotation (rotate / reject expired).
- **NotificationService.Tests** — consumers create the right notification, the dispatch service
  marks notifications `Sent`/`Failed` around the email stub, and a **MassTransit in-memory harness**
  test verifies the full publish → consume flow without a real broker.

---

## Database provider auto-detection

Both services pick their EF Core provider from the connection string at startup: a SQL Server-style
string (`Server=`, `Database=`, `Initial Catalog=`) uses SQL Server; anything else falls back to
SQLite (`Data Source=...`). Switch to SQL Server purely via `ConnectionStrings__DefaultConnection`.
