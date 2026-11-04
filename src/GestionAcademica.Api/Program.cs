using System.Security.Claims;
using System.Text;
using GestionAcademica.Api.Authorization;
using GestionAcademica.Application;
using GestionAcademica.Application.Common;
using GestionAcademica.Infrastructure;
using GestionAcademica.Infrastructure.Security;
using GestionAcademica.Shared.Contracts.Constants;
using GestionAcademica.Shared.Contracts.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumMemberConverter()));

// JWT
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Configuración 'Jwt' no encontrada.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Administrativo,
        p => Policies.RequireActiveRole(p, AppRoles.Administrativo));
    options.AddPolicy(Policies.Alumno,
        p => Policies.RequireActiveRole(p, AppRoles.Alumno));
    options.AddPolicy(Policies.Profesor,
        p => Policies.RequireActiveRole(p, AppRoles.Profesor));
    options.AddPolicy(Policies.AdministrativoOAlumno,
        p => Policies.RequireActiveRole(p, AppRoles.Administrativo, AppRoles.Alumno));
    options.AddPolicy(Policies.AdministrativoOProfesor,
        p => Policies.RequireActiveRole(p, AppRoles.Administrativo, AppRoles.Profesor));
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "GestionAcademica API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresar el token JWT obtenido en /api/auth/login.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

// CORS (desarrollo): permite clientes de escritorio y web futura
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<GestionAcademica.Infrastructure.Seeding.DbSeeder>();
    await seeder.SeedAsync();
}

app.UseMiddleware<GestionAcademica.Api.Middlewares.ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
