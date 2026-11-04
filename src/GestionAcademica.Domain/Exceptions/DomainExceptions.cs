namespace GestionAcademica.Domain.Exceptions;

/// <summary>Excepción base de negocio. El middleware global la traduce a un 409/400 adecuado.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>Se intenta inscribir en una comisión sin cupo disponible.</summary>
public sealed class CupoExcedidoException : DomainException
{
    public CupoExcedidoException(string comision, int cupoMaximo)
        : base($"La comisión '{comision}' alcanzó su cupo máximo de {cupoMaximo} inscriptos.") { }
}

/// <summary>La lista de espera de la comisión está completa.</summary>
public sealed class ListaEsperaLlenaException : DomainException
{
    public ListaEsperaLlenaException(string comision, int listaEsperaMaxima)
        : base($"La comisión '{comision}' tiene su lista de espera completa ({listaEsperaMaxima} lugares).") { }
}

/// <summary>El alumno no cumple una correlatividad requerida.</summary>
public sealed class CorrelativaIncumplidaException : DomainException
{
    public CorrelativaIncumplidaException(string materiaRequisito, string tipoRequisito)
        : base($"No cumple la correlatividad con '{materiaRequisito}' (requisito: {tipoRequisito}).") { }
}

/// <summary>La ventana de inscripción del período está cerrada.</summary>
public sealed class VentanaInscripcionCerradaException : DomainException
{
    public VentanaInscripcionCerradaException(string detalle)
        : base($"La inscripción no está disponible: {detalle}") { }
}

/// <summary>Recurso no encontrado.</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string recurso)
        : base($"No se encontró el recurso '{recurso}'.") { }
}

/// <summary>Credenciales inválidas en el login.</summary>
public sealed class CredencialesInvalidasException : DomainException
{
    public CredencialesInvalidasException()
        : base("Usuario o contraseña incorrectos.") { }
}

/// <summary>Violación de una regla de negocio genérica.</summary>
public sealed class ReglaDeNegocioException : DomainException
{
    public ReglaDeNegocioException(string message) : base(message) { }
}

/// <summary>El usuario autenticado no tiene permiso para acceder al recurso.</summary>
public sealed class AccesoDenegadoException : DomainException
{
    public AccesoDenegadoException(string message) : base(message) { }
}
