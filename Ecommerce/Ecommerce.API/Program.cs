using ECommerceAPI.API.Extensions;
using ECommerceAPI.API.Middleware;
using ECommerceAPI.Application;
using ECommerceAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication + Authorization policies
builder.Services.AddJwtAuthentication(builder.Configuration);

// Swagger with JWT button
builder.Services.AddSwaggerWithJwt();

// CORS — update origins for production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ─── Pipeline ─────────────────────────────────────────────────────────────────

var app = builder.Build();

// Global exception handler — always first
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Order matters: Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto-apply migrations on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider
        .GetRequiredService<ECommerceAPI.Infrastructure.Persistence.AppDbContext>();
    db.Database.EnsureCreated(); // Use Migrate() after you add migrations
}

app.Run();
