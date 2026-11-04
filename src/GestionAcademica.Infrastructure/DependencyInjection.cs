using GestionAcademica.Application.Interfaces;
using GestionAcademica.Infrastructure.FileStorage;
using GestionAcademica.Infrastructure.Persistence;
using GestionAcademica.Infrastructure.Seeding;
using GestionAcademica.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace GestionAcademica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' no configurada.");

        services.AddDbContext<GestionAcademicaDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<GestionAcademicaDbContext>());

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IJwtTokenParser, JwtTokenParser>();
        services.AddScoped<IInscripcionComisionService, InscripcionComisionService>();
        services.AddScoped<DbSeeder>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Certificados: PDF (QuestPDF) + QR + almacenamiento local protegido.
        QuestPDF.Settings.License = LicenseType.Community;

        var certificadoSettings = configuration
            .GetSection(CertificadoSettings.SectionName)
            .Get<CertificadoSettings>() ?? new CertificadoSettings();

        services.AddSingleton(certificadoSettings);
        services.AddSingleton<ICertificadoSettings>(certificadoSettings);
        services.AddSingleton<ICertificadoFileStorage, CertificadoFileStorage>();
        services.AddSingleton<ICertificadoPdfGenerator, CertificadoPdfGenerator>();

        return services;
    }
}
