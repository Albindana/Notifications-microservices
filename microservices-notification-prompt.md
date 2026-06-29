# Notification & Messaging Platform — .NET Microservices Project Brief

> Use this file as a prompt for Claude Code. Paste it or reference it at the start of your session.

---

## Project Overview

Build a **Notification & Messaging Platform** using a **Microservices Architecture** with 3 services communicating via a message bus. This project demonstrates service isolation, async messaging, Docker orchestration, and API Gateway routing.

**Goal:** A working microservices system where services are fully decoupled, each owns its own database, and communication happens either via HTTP (sync) through a gateway or via RabbitMQ (async) through MassTransit.

---

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 8 Web API (per service) |
| Message Bus | RabbitMQ + MassTransit 8 |
| API Gateway | Ocelot |
| ORM | Entity Framework Core 8 |
| Database | SQLite (dev) / SQL Server (prod) — auto-detected |
| Authentication | ASP.NET Core Identity + JWT (UserService only) |
| Validation | FluentValidation |
| Docs | Swagger (per service) |
| Orchestration | Docker + Docker Compose |
| Testing | xUnit + Moq |

---

## Solution Structure

```
NotificationPlatform.sln
├── services/
│   ├── UserService/
│   │   ├── UserService.API/
│   │   ├── UserService.Application/
│   │   ├── UserService.Domain/
│   │   └── UserService.Infrastructure/
│   │
│   ├── NotificationService/
│   │   ├── NotificationService.API/
│   │   ├── NotificationService.Application/
│   │   ├── NotificationService.Domain/
│   │   └── NotificationService.Infrastructure/
│   │
│   └── ApiGateway/
│       └── ApiGateway.API/
│
├── shared/
│   └── Shared.Contracts/         # Shared event contracts ONLY — no business logic
│
├── docker-compose.yml
├── docker-compose.override.yml
└── README.md
```

### Critical Rule — Database Isolation
Each service has its **own database**. No service ever reads or writes another service's database. This is the most important microservices principle.

```
UserService      → userservice.db   (users, refresh tokens)
NotificationService → notifications.db  (notification logs, templates)
ApiGateway       → no database
RabbitMQ         → runs in Docker
```

---

## Service 1 — UserService

### Responsibility
Handles all user identity concerns: registration, login, JWT issuance, refresh tokens, and profile management. Publishes domain events to RabbitMQ when significant things happen.

### Domain Entities

**AppUser (extends IdentityUser)**
```
Id (string)
FirstName (string)
LastName (string)
CreatedAt (DateTime)
RefreshToken (string?)
RefreshTokenExpiry (DateTime?)
```

### API Endpoints

```
POST   /api/auth/register        → register new user
POST   /api/auth/login           → returns { accessToken, refreshToken }
POST   /api/auth/refresh         → rotate refresh token
POST   /api/auth/logout          → invalidate refresh token

GET    /api/users/me             → current user profile [Authorized]
PUT    /api/users/me             → update profile [Authorized]
POST   /api/users/forgot-password → request password reset
POST   /api/users/reset-password  → confirm reset with token
```

### Events Published to RabbitMQ

Define these in `Shared.Contracts`:

```csharp
// Shared.Contracts/Events/UserRegisteredEvent.cs
public record UserRegisteredEvent
{
    public string UserId { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public DateTime RegisteredAt { get; init; }
}

// Shared.Contracts/Events/PasswordResetRequestedEvent.cs
public record PasswordResetRequestedEvent
{
    public string UserId { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string ResetToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}

// Shared.Contracts/Events/UserProfileUpdatedEvent.cs
public record UserProfileUpdatedEvent
{
    public string UserId { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
}
```

### Publishing Events (MassTransit)

```csharp
// In RegisterCommandHandler
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        // ... create user logic ...

        await _publishEndpoint.Publish(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            RegisteredAt = DateTime.UtcNow
        }, ct);

        return authResponse;
    }
}
```

### MassTransit Configuration (UserService)

```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });
    });
});
```

---

## Service 2 — NotificationService

### Responsibility
Listens for events from RabbitMQ and sends notifications (email, in-app). Stores all notifications in its own database for history and audit.

### Domain Entities

**Notification**
```
Id (Guid)
UserId (string)
RecipientEmail (string)
Type (enum: Welcome, PasswordReset, ProfileUpdate, OrderConfirmation)
Channel (enum: Email, InApp, SMS)
Subject (string)
Body (string)
Status (enum: Pending, Sent, Failed)
SentAt (DateTime?)
CreatedAt (DateTime)
ErrorMessage (string?)
RetryCount (int)
```

**NotificationTemplate**
```
Id (Guid)
Type (enum — matches Notification.Type)
Subject (string)
BodyTemplate (string)  // uses {{FirstName}}, {{ResetLink}} placeholders
CreatedAt (DateTime)
UpdatedAt (DateTime)
```

### API Endpoints

```
GET    /api/notifications              → list notifications (admin)
GET    /api/notifications/user/{userId} → notifications for a user
GET    /api/notifications/{id}         → single notification detail
PUT    /api/notifications/{id}/retry   → retry a failed notification
GET    /api/templates                  → list templates (admin)
PUT    /api/templates/{id}             → update template body (admin)
```

### Event Consumers (MassTransit)

```csharp
// Consumers/UserRegisteredConsumer.cs
public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly INotificationService _notificationService;

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;

        await _notificationService.SendAsync(new SendNotificationCommand
        {
            UserId = evt.UserId,
            RecipientEmail = evt.Email,
            Type = NotificationType.Welcome,
            Channel = NotificationChannel.Email,
            TemplateData = new Dictionary<string, string>
            {
                { "FirstName", evt.FirstName },
                { "RegisteredAt", evt.RegisteredAt.ToString("f") }
            }
        });
    }
}

// Consumers/PasswordResetRequestedConsumer.cs
public class PasswordResetRequestedConsumer : IConsumer<PasswordResetRequestedEvent>
{
    public async Task Consume(ConsumeContext<PasswordResetRequestedEvent> context)
    {
        // Send password reset email with reset link
    }
}
```

### MassTransit Configuration (NotificationService)

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddConsumer<PasswordResetRequestedConsumer>();
    x.AddConsumer<UserProfileUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

### Email Sending (Stubbed)

Do not integrate a real email provider. Create an interface and a console-logging stub:

```csharp
// Application/Interfaces/IEmailSender.cs
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

// Infrastructure/Services/ConsoleEmailSender.cs
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] To: {To} | Subject: {Subject} | Body: {Body}",
            to, subject, body);

        await Task.CompletedTask;
    }
}
```

This is the right approach — no real credentials needed, and it clearly signals to reviewers that the integration point is clean and swappable.

---

## Service 3 — API Gateway

### Responsibility
Single entry point for all clients. Routes HTTP requests to the correct downstream service. Handles no business logic.

### Structure

```
ApiGateway.API/
├── Program.cs
├── ocelot.json          # routing configuration
├── ocelot.Development.json
└── Dockerfile
```

### ocelot.json

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/auth/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "userservice", "Port": 8080 }
      ],
      "UpstreamPathTemplate": "/api/auth/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
    },
    {
      "DownstreamPathTemplate": "/api/users/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "userservice", "Port": 8080 }
      ],
      "UpstreamPathTemplate": "/api/users/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer"
      }
    },
    {
      "DownstreamPathTemplate": "/api/notifications/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "notificationservice", "Port": 8080 }
      ],
      "UpstreamPathTemplate": "/api/notifications/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer"
      }
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### Program.cs (Gateway)

```csharp
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot();

// Add JWT validation so the gateway can protect upstream routes
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options => { /* same JWT config as UserService */ });

var app = builder.Build();
await app.UseOcelot();
app.Run();
```

---

## Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"   # management UI at http://localhost:15672
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  userservice:
    build:
      context: ./services/UserService
      dockerfile: UserService.API/Dockerfile
    container_name: userservice
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Data Source=/data/userservice.db
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Username=guest
      - RabbitMQ__Password=guest
      - JwtSettings__SecretKey=your-super-secret-key-min-32-chars
    depends_on:
      rabbitmq:
        condition: service_healthy
    volumes:
      - userservice-data:/data

  notificationservice:
    build:
      context: ./services/NotificationService
      dockerfile: NotificationService.API/Dockerfile
    container_name: notificationservice
    ports:
      - "5002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Data Source=/data/notificationservice.db
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Username=guest
      - RabbitMQ__Password=guest
    depends_on:
      rabbitmq:
        condition: service_healthy
    volumes:
      - notificationservice-data:/data

  apigateway:
    build:
      context: ./services/ApiGateway
      dockerfile: ApiGateway.API/Dockerfile
    container_name: apigateway
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - JwtSettings__SecretKey=your-super-secret-key-min-32-chars
    depends_on:
      - userservice
      - notificationservice

volumes:
  userservice-data:
  notificationservice-data:
```

### Dockerfile (same pattern for each service)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["UserService.API/UserService.API.csproj", "UserService.API/"]
COPY ["UserService.Application/UserService.Application.csproj", "UserService.Application/"]
COPY ["UserService.Domain/UserService.Domain.csproj", "UserService.Domain/"]
COPY ["UserService.Infrastructure/UserService.Infrastructure.csproj", "UserService.Infrastructure/"]
RUN dotnet restore "UserService.API/UserService.API.csproj"
COPY . .
RUN dotnet build "UserService.API/UserService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserService.API/UserService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.API.dll"]
```

---

## Communication Architecture Summary

```
┌─────────────┐     HTTP      ┌──────────────┐
│   Client    │──────────────▶│  API Gateway │
└─────────────┘               │   (Ocelot)   │
                              └──────┬───────┘
                                     │ HTTP routing
                    ┌────────────────┼─────────────────┐
                    ▼                                   ▼
          ┌─────────────────┐               ┌───────────────────┐
          │   UserService   │               │ NotificationService│
          │  (Auth + Users) │               │  (Notifications)  │
          └────────┬────────┘               └─────────┬─────────┘
                   │ publishes events                  │ consumes events
                   ▼                                   │
          ┌─────────────────┐                          │
          │    RabbitMQ     │◀─────────────────────────┘
          │  (MassTransit)  │
          └─────────────────┘

Sync:  Client → Gateway → UserService (HTTP)
Async: UserService → RabbitMQ → NotificationService (fire and forget)
```

---

## Tests to Write

### UserService Unit Tests
1. `RegisterCommandHandlerTests` — registers user, publishes UserRegisteredEvent, rejects duplicate email
2. `LoginCommandHandlerTests` — returns tokens on valid credentials, throws on wrong password
3. `RefreshTokenHandlerTests` — rotates token, rejects expired token

### NotificationService Unit Tests
1. `UserRegisteredConsumerTests` — creates Welcome notification when event received
2. `PasswordResetConsumerTests` — creates PasswordReset notification with correct template data
3. `NotificationServiceTests` — marks notification as Sent after email stub called, marks as Failed on exception

### Integration Test (optional but impressive)
Use MassTransit's `InMemoryTestHarness` to test the full publish → consume flow without a real RabbitMQ instance:

```csharp
[Fact]
public async Task WhenUserRegisters_NotificationServiceReceivesEvent()
{
    await using var harness = new InMemoryTestHarness();
    var consumerHarness = harness.Consumer<UserRegisteredConsumer>();

    await harness.Start();

    await harness.InputQueueSendEndpoint.Send(new UserRegisteredEvent
    {
        UserId = "user-1",
        Email = "test@example.com",
        FirstName = "Test",
        RegisteredAt = DateTime.UtcNow
    });

    Assert.True(await consumerHarness.Consumed.Any<UserRegisteredEvent>());
}
```

---

## Seed Data

UserService seeds on startup:
- Admin user: `admin@notify.com` / `Admin123!`
- Test user: `user@notify.com` / `User123!`

NotificationService seeds on startup:
- 3 notification templates (Welcome, PasswordReset, ProfileUpdate) with placeholder bodies

---

## Project Setup Instructions for Claude Code

Build in this exact order to avoid wiring issues:

1. Create solution and all projects, add to `.sln`
2. Create `Shared.Contracts` class library — define all event records here first
3. Build **UserService** completely (Domain → Infrastructure → Application → API) including MassTransit publish wiring
4. Build **NotificationService** completely — consumers, notification logging, email stub
5. Build **ApiGateway** — Ocelot config, JWT validation, `ocelot.json`
6. Write `docker-compose.yml` and Dockerfiles for all 3 services
7. Write unit tests for both services
8. Verify end-to-end: `docker compose up` → register user → check NotificationService logs for email stub output

---

## What This Project Demonstrates to Interviewers

- Microservices fundamentals (service isolation, database-per-service)
- Async messaging with RabbitMQ + MassTransit (publish/subscribe pattern)
- API Gateway routing with Ocelot
- Docker Compose orchestration of multiple services
- Shared contracts without shared business logic
- Event-driven architecture (UserService fires events, NotificationService reacts)
- Clean integration points (email stub is swappable with real provider)
- Health checks and service dependency ordering in Docker
- Eventual consistency understanding

## Git Workflow

After every completed feature or milestone, commit and push using:

```bash
git add . && git commit -m "your message here" && git push
```

### Recommended commit points
- After scaffolding solution structure and Shared.Contracts
- After UserService compiles and runs standalone
- After NotificationService compiles and runs standalone
- After API Gateway and ocelot.json configured
- After docker-compose.yml and all Dockerfiles written
- After `docker compose up` works end-to-end
- After unit tests written and passing

### Commit message format
Use short descriptive messages:
```
feat: add UserRegisteredEvent consumer
feat: scaffold UserService domain and infrastructure
fix: RabbitMQ healthcheck in docker-compose
test: add MassTransit InMemoryTestHarness integration test
chore: add Dockerfiles for all 3 services
```