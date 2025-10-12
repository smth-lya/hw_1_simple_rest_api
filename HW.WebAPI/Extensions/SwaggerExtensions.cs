using Microsoft.OpenApi.Models;

namespace HW.WebAPI.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCustomSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
        });

        return services;
    }

    public static IApplicationBuilder UseCustomSwaggerUi(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service v1");
            c.RoutePrefix = "swagger";
        });
        
        return app;
    }
}