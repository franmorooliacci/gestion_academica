using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GestionAcademica.Application.Common;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

/// <summary>Base común de los controllers: resuelve la identidad y el contexto del token.</summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected Solicitante SolicitanteActual
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(raw, out var id))
                throw new AccesoDenegadoException("El token no contiene un id de persona válido.");

            var rol = User.FindFirstValue(AppClaims.ActiveRole);
            if (string.IsNullOrWhiteSpace(rol))
                throw new AccesoDenegadoException("El token no contiene un rol activo.");

            return new Solicitante(id, rol);
        }
    }

    protected ClientApp Cliente
    {
        get
        {
            var raw = User.FindFirstValue(AppClaims.Client);
            if (Enum.TryParse<ClientApp>(raw, ignoreCase: true, out var client))
                return client;

            throw new AccesoDenegadoException("El token no contiene un cliente válido.");
        }
    }
}
