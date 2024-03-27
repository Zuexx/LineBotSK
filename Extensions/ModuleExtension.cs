using Linebot.Modules;

namespace SHL.MRDashboard.Api.Extension;

public static class ModuleExtensions
{
    // this could also be added into the DI container
    static readonly List<IModule> registeredModules = new List<IModule>();
    public static IServiceCollection RegisterModules(this IServiceCollection services)
    {
        var modules = DiscoverModules();
        foreach (var module in modules)
        {
            module.RegisterModule(services);
            registeredModules.Add(module);
        }
        return services;
    }
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("/v1");

        foreach (var module in registeredModules)
        {
            module.MapEndpoints(endpoints);
        }
        return app;
    }

    // public static WebApplication StartSeed(this WebApplication app)
    // {
    //     var scopedFactory = app.Services.GetService<IServiceScopeFactory>();

    //     using (var scope = scopedFactory?.CreateScope())
    //     {
    //         var service = scope?.ServiceProvider.GetService<DataSeeder>();
    //         service?.Seed();
    //     }
    //     return app;
    // }

    private static IEnumerable<IModule> DiscoverModules()
    {
        return typeof(IModule).Assembly
            .GetTypes()
            .Where(p => p.IsClass && p.IsAssignableTo(typeof(IModule)))
            .Select(Activator.CreateInstance)
            .Cast<IModule>();
    }
}