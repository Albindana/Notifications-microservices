using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot routing. ocelot.json holds the default (Docker) routes; an optional
// environment-specific file (e.g. ocelot.Development.json) overrides hosts/ports for local dev.
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration);

// JWT validation so the gateway can protect upstream routes (must match UserService config).
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSection["SecretKey"] ?? "your-super-secret-key-min-32-chars-long-please-change";
var issuer = jwtSection["Issuer"] ?? "NotificationPlatform.UserService";
var audience = jwtSection["Audience"] ?? "NotificationPlatform";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ApiGateway" }));

app.UseAuthentication();
await app.UseOcelot();

app.Run();
