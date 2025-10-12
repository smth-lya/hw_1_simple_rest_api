namespace HW.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher, DefaultPasswordHasher>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        
        return services;
    }
}