using MassTransit;
using NotificationService.API.Middleware;
using NotificationService.Application;
using NotificationService.Application.Consumers;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---- Application & Infrastructure ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Controllers & Swagger ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- MassTransit + RabbitMQ (consumers) ----
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddConsumer<PasswordResetRequestedConsumer>();
    x.AddConsumer<UserProfileUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "NotificationService" }));

// Apply migrations and seed default templates on startup.
await DbSeeder.MigrateAndSeedAsync(app.Services);

app.Run();

public partial class Program { }
