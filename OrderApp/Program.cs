using System.IO;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderApp.Models;
using OrderApp.Services;
using OrderApp.Repositories;

var builder = WebApplication.CreateBuilder(args);

// load configuration (appsettings.json) from output directory
var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// DI registrations
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// JWT configuration
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key") ?? throw new InvalidOperationException("Jwt:Key is not configured");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection.GetValue<string>("Issuer"),
            ValidAudience = jwtSection.GetValue<string>("Audience"),
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("OrderApp starting. Effective log level: {Level}", builder.Configuration["Logging:LogLevel:OrderApp"] ?? builder.Configuration["Logging:LogLevel:Default"]);

// Minimal API: token issuance (demo) and CRUD endpoints for orders
app.MapPost("/login", (LoginRequest req) =>
{
    // Demo-only static credential check. Replace with real user store for production.
    if (req.Username != "demo" || req.Password != "password")
        return Results.Unauthorized();

    var jwtSection2 = app.Configuration.GetSection("Jwt");
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection2.GetValue<string>("Key")));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expires = DateTime.UtcNow.AddMinutes(jwtSection2.GetValue<int>("ExpireMinutes"));

    var token = new JwtSecurityToken(
        issuer: jwtSection2.GetValue<string>("Issuer"),
        audience: jwtSection2.GetValue<string>("Audience"),
        expires: expires,
        signingCredentials: creds
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = tokenString });
}).AllowAnonymous();

app.MapGet("/orders", async (IOrderService svc) => Results.Ok(await svc.ListAsync())).RequireAuthorization();

app.MapGet("/orders/{id}", async (IOrderService svc, Guid id) =>
{
    try
    {
        var order = await svc.GetByIdAsync(id);
        return Results.Ok(order);
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.MapPost("/orders", async (IOrderService svc, Order order) =>
{
    var created = await svc.CreateAsync(order);
    return Results.Created($"/orders/{created.Id}", created);
}).RequireAuthorization();

app.MapDelete("/orders/{id}", async (IOrderService svc, Guid id) =>
{
    try
    {
        await svc.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.Run();

// DTO used by login endpoint
public record LoginRequest(string Username, string Password);
