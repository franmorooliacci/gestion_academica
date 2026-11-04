using System.Text.Json;
using FluentValidation;
using GestionAcademica.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

// Nota: ArgumentException/JsonException se producen al deserializar un valor inválido
// (p. ej. un enum inexistente en el body). Se mapean a 400 en lugar de 500.

namespace GestionAcademica.Api.Middlewares;

/// <summary>Convierte excepciones de dominio/validación en ProblemDetails con el código HTTP adecuado.</summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado: {Message}", ex.Message);
            await WriteResponseAsync(context, ex);
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, Exception ex)
    {
        var statusCode = ex switch
        {
            CredencialesInvalidasException => StatusCodes.Status401Unauthorized,
            AccesoDenegadoException => StatusCodes.Status403Forbidden,
            NotFoundException => StatusCodes.Status404NotFound,
            CupoExcedidoException => StatusCodes.Status409Conflict,
            ListaEsperaLlenaException => StatusCodes.Status409Conflict,
            CorrelativaIncumplidaException => StatusCodes.Status409Conflict,
            VentanaInscripcionCerradaException => StatusCodes.Status409Conflict,
            ReglaDeNegocioException => StatusCodes.Status400BadRequest,
            DomainException => StatusCodes.Status400BadRequest,
            ValidationException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            JsonException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ex is ValidationException or ArgumentException or JsonException
                ? "Solicitud inválida"
                : ex is DomainException ? "Error de negocio" : "Error interno",
            Detail = ex is ValidationException
                ? "Uno o más campos son inválidos."
                : ex is ArgumentException or JsonException
                    ? "El cuerpo de la solicitud contiene un valor inválido."
                    : ex is DomainException ? ex.Message : "Ocurrió un error inesperado.",
            Instance = context.Request.Path,
        };

        if (ex is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
