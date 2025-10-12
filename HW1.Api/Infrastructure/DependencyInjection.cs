using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Contracts.Security;
using HW1.Api.Infrastructure.Database;
using HW1.Api.Infrastructure.Database.Repositories;
using HW1.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace HW1.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher, DefaultPasswordHasher>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgresConnection"));
        }); 
        
        return services;
    }
}