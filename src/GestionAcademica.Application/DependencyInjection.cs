using System.Reflection;
using FluentValidation;
using GestionAcademica.Application.Common.Behaviors;
using GestionAcademica.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAcademica.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IUserProfileService, UserProfileService>();

        return services;
    }
}
