using Microsoft.Extensions.DependencyInjection;
using ProdHelperService.Controllers.Controller;
using ProdHelperService.Controllers.Interface;

namespace ProdHelperService.Controllers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProdHelperControllers(this IServiceCollection services)
    {
        services.AddSingleton<IOeeController, OeeController>();
        services.AddSingleton<IPlannerController, PlannerController>();
        services.AddSingleton<IControllerDispatcher, ControllerDispatcher>();
        return services;
    }
}
