namespace GestionAcademica.Application.Interfaces;

/// <summary>Hash y verificación de contraseñas (implementación BCrypt en Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
