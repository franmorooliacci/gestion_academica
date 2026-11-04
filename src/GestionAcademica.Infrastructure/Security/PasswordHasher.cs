using GestionAcademica.Application.Interfaces;

namespace GestionAcademica.Infrastructure.Security;

/// <summary>Hash de contraseñas con BCrypt (work factor 12).</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
