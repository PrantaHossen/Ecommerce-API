using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Domain.Interfaces;
using ECommerceAPI.Infrastructure.Persistence;
using ECommerceAPI.Infrastructure.Persistence.Repositories;
using ECommerceAPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ─── Database ────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // ─── Repositories ────────────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();

        // ─── Services ────────────────────────────────────────────────────────
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<IPasswordService, PasswordService>();

        return services;
    }
}
