# Level 2 — Medium: How It Actually Works

> Now we go one layer deeper: the architecture inside each service, the event flow,
> and the patterns used. This is where most interview questions live.

---

## Clean Architecture — the 4 layers inside each service

Both UserService and NotificationService are split into **4 projects** (layers). The rule:
**dependencies only point inward** — outer layers know about inner layers, never the reverse.

```
API  ──▶  Application  ──▶  Domain
 │             │
 └──▶  Infrastructure  ──▶  (implements interfaces defined in Application)
```

| Layer | Contains | Example files |
|---|---|---|
| **Domain** | The core entities. No dependencies on anything. | `AppUser`, `Notification`, `NotificationTemplate`, enums |
| **Application** | Business logic, CQRS handlers, **interfaces** (contracts), validators. | `RegisterCommandHandler`, `IIdentityService`, `INotificationService` |
| **Infrastructure** | The "how": EF Core DbContext, repositories, JWT generation, the email stub. | `AppDbContext`, `IdentityService`, `JwtTokenService`, `ConsoleEmailSender` |
| **API** | The entry point: controllers, DI wiring, middleware, `Program.cs`. | `AuthController`, `Program.cs` |

**Why this matters (interview answer):** The Application layer defines *interfaces* like
`IIdentityService`. The Infrastructure layer *implements* them. This is **Dependency Inversion** —
business logic doesn't depend on database/framework details, it depends on abstractions. That's
also what makes it **unit-testable**: in tests we swap the real implementation for a mock.

---

## CQRS with MediatR

Instead of fat controllers, every operation is a **Command** (changes data) or **Query** (reads
data), each with its own **Handler**.

```
Controller → sends RegisterCommand → MediatR → RegisterCommandHandler runs the logic
```

- `RegisterCommand` / `LoginCommand` / `RefreshTokenCommand` (commands)
- `GetCurrentUserQuery` / `GetNotificationsQuery` (queries)

**Why:** Each handler does exactly one thing → easy to read, easy to test in isolation. The
controller stays thin — it just translates an HTTP request into a command and returns the result.

There's also a **MediatR pipeline behavior** (`ValidationBehavior`) that automatically runs
FluentValidation rules **before** any handler executes. So validation is cross-cutting — you
write the rules once, they run for every command automatically.

---

## The event-driven flow (the heart of the project)

### Events live in `Shared.Contracts`
A tiny shared library holding **only** the event definitions (plain C# records). Both services
reference it so they agree on the message shape. It has **no business logic** — that's deliberate.

```csharp
public record UserRegisteredEvent {
    public string UserId { get; init; }
    public string Email { get; init; }
    public string FirstName { get; init; }
    public DateTime RegisteredAt { get; init; }
}
```

### Publishing (UserService side)
Inside `RegisterCommandHandler`, after creating the user:
```csharp
await _publishEndpoint.Publish(new UserRegisteredEvent { ... });
```
`IPublishEndpoint` is from MassTransit. `Publish` drops the message onto RabbitMQ and returns
immediately — UserService doesn't know or care who's listening.

### Consuming (NotificationService side)
A **Consumer** class subscribes to that event type:
```csharp
public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent> {
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context) {
        // build a Welcome notification and send it
    }
}
```
When a `UserRegisteredEvent` lands in RabbitMQ, MassTransit calls this consumer automatically.

### The three events / consumers

| Event | Published when | Consumer | Result |
|---|---|---|---|
| `UserRegisteredEvent` | user registers | `UserRegisteredConsumer` | Welcome email |
| `PasswordResetRequestedEvent` | forgot-password | `PasswordResetRequestedConsumer` | Reset-link email |
| `UserProfileUpdatedEvent` | profile updated | `UserProfileUpdatedConsumer` | "profile updated" email |

---

## How a notification actually gets "sent"

The `NotificationDispatchService` (implements `INotificationService`) does the work:

1. Look up the **template** for the type (e.g. Welcome) from the DB.
2. **Render** it — replace `{{FirstName}}` placeholders with real data.
3. Create a `Notification` record with status **Pending**, save it.
4. Call `IEmailSender.SendAsync(...)`.
5. If it succeeds → mark **Sent** (+ timestamp). If it throws → mark **Failed** (+ error message).
6. Save again.

The `IEmailSender` implementation is `ConsoleEmailSender` — it just logs
`[EMAIL STUB] To: ... | Subject: ... | Body: ...`. **Why a stub?** No real email credentials
needed, and the interface means you can swap in SendGrid/SES later without touching any caller.

---

## The API Gateway (Ocelot)

The gateway is the **single entry point**. Clients only ever talk to it (port 5080); they never
hit the services directly. Its config (`ocelot.json`) maps incoming paths to downstream services:

```
/api/auth/*          → userservice:8080
/api/users/*         → userservice:8080          (requires JWT)
/api/notifications/* → notificationservice:8080  (requires JWT)
/api/templates/*     → notificationservice:8080  (requires JWT)
```

The gateway also **validates the JWT** before forwarding protected routes — so an unauthenticated
request gets a 401 at the gateway and never reaches the service.

**Why a gateway?** One public address, central auth, and clients don't need to know how many
services exist or where they live.

---

## Authentication: JWT + refresh tokens

1. **Login** → UserService checks the password (via ASP.NET Core Identity) and returns:
   - an **access token** (JWT, short-lived ~60 min) — sent with every request as a Bearer token.
   - a **refresh token** (long-lived ~7 days) — stored on the user record in the DB.
2. The JWT is **signed** with a secret key. Anyone can read it, but nobody can forge it without
   the key. It carries the user's id and email as "claims."
3. When the access token expires, the client calls **`/api/auth/refresh`** with the refresh token
   to get a new pair — **and the old refresh token is rotated (replaced)** for security.
4. **Logout** clears the refresh token so it can't be reused.

---

## Databases & EF Core

- Each service has its own `DbContext` and its own SQLite file (`userservice.db`,
  `notificationservice.db`).
- **Migrations** (versioned schema changes) are generated with `dotnet ef migrations add`.
- On startup, each service runs `db.Database.MigrateAsync()` to auto-create/update its schema,
  then **seeds** initial data (admin/test users; 3 notification templates).
- The DB provider is **auto-detected** from the connection string: SQL Server-style strings use
  SQL Server, everything else uses SQLite. So dev = SQLite, prod could be SQL Server with zero
  code changes.

---

## Docker Compose

`docker compose up --build` starts everything: RabbitMQ + the 3 services, on one network.

- Services find RabbitMQ by the hostname `rabbitmq` (Docker's internal DNS).
- `depends_on` + a **healthcheck** make the services wait until RabbitMQ is actually ready
  (not just started) before they boot.
- Each service is published on a host port: Gateway 5080, UserService 5001, NotificationService 5002,
  RabbitMQ UI 15672.

---

## What you should be able to explain now

- The 4 clean-architecture layers and why dependencies point inward.
- CQRS: commands vs queries, why handlers are thin and testable.
- The full publish → RabbitMQ → consume → email → save flow, with class names.
- Why `Shared.Contracts` has no logic.
- JWT access token vs refresh token, and rotation.
- Why each service has its own database, and how data crosses boundaries (via events, not DB reads).

➡️ **Next:** `03-hard.md` — deep technical details, trade-offs, and a Q&A drill.
