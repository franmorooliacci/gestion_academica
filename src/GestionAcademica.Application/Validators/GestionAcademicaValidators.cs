using FluentValidation;
using GestionAcademica.Application.UseCases.Auth;
using GestionAcademica.Application.UseCases.GestionAcademica;

namespace GestionAcademica.Application.Validators;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Client).NotNull();
    }
}

public sealed class CreateCarreraCommandValidator : AbstractValidator<CreateCarreraCommand>
{
    public CreateCarreraCommandValidator()
    {
        RuleFor(x => x.Request.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Request.TituloOtorgado).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Request.DuracionAnios).GreaterThan((short)0);
    }
}

public sealed class UpdateCarreraCommandValidator : AbstractValidator<UpdateCarreraCommand>
{
    public UpdateCarreraCommandValidator()
    {
        RuleFor(x => x.Request.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Request.TituloOtorgado).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Request.DuracionAnios).GreaterThan((short)0);
    }
}

public sealed class CreateMateriaCommandValidator : AbstractValidator<CreateMateriaCommand>
{
    public CreateMateriaCommandValidator()
    {
        RuleFor(x => x.Request.IdCarrera).GreaterThan(0);
        RuleFor(x => x.Request.Codigo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Request.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Request.AnioEnCarrera).GreaterThan((short)0);
        RuleFor(x => x.Request.CargaHorariaTotal).GreaterThan(0);
    }
}

public sealed class UpdateMateriaCommandValidator : AbstractValidator<UpdateMateriaCommand>
{
    public UpdateMateriaCommandValidator()
    {
        RuleFor(x => x.Request.IdCarrera).GreaterThan(0);
        RuleFor(x => x.Request.Codigo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Request.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Request.AnioEnCarrera).GreaterThan((short)0);
        RuleFor(x => x.Request.CargaHorariaTotal).GreaterThan(0);
    }
}

public sealed class CreatePeriodoCommandValidator : AbstractValidator<CreatePeriodoCommand>
{
    public CreatePeriodoCommandValidator()
    {
        RuleFor(x => x.Request.Anio).InclusiveBetween((short)2000, (short)2100);
        RuleFor(x => x.Request.FechaFin).GreaterThanOrEqualTo(x => x.Request.FechaInicio);
    }
}

public sealed class UpdatePeriodoCommandValidator : AbstractValidator<UpdatePeriodoCommand>
{
    public UpdatePeriodoCommandValidator()
    {
        RuleFor(x => x.Request.Anio).InclusiveBetween((short)2000, (short)2100);
        RuleFor(x => x.Request.FechaFin).GreaterThanOrEqualTo(x => x.Request.FechaInicio);
    }
}

public sealed class CreateComisionCommandValidator : AbstractValidator<CreateComisionCommand>
{
    public CreateComisionCommandValidator()
    {
        RuleFor(x => x.Request.IdMateria).GreaterThan(0);
        RuleFor(x => x.Request.IdPeriodo).GreaterThan(0);
        RuleFor(x => x.Request.Codigo).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Request.CupoMaximo).GreaterThan(0).When(x => x.Request.CupoMaximo.HasValue);
        RuleFor(x => x.Request.ListaEsperaMaxima).GreaterThanOrEqualTo(0).When(x => x.Request.ListaEsperaMaxima.HasValue);
    }
}

public sealed class UpdateComisionCommandValidator : AbstractValidator<UpdateComisionCommand>
{
    public UpdateComisionCommandValidator()
    {
        RuleFor(x => x.Request.IdMateria).GreaterThan(0);
        RuleFor(x => x.Request.IdPeriodo).GreaterThan(0);
        RuleFor(x => x.Request.Codigo).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Request.CupoMaximo).GreaterThan(0).When(x => x.Request.CupoMaximo.HasValue);
        RuleFor(x => x.Request.ListaEsperaMaxima).GreaterThanOrEqualTo(0).When(x => x.Request.ListaEsperaMaxima.HasValue);
    }
}

public sealed class CreatePersonaCommandValidator : AbstractValidator<CreatePersonaCommand>
{
    public CreatePersonaCommandValidator()
    {
        RuleFor(x => x.Request.Nombre).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Request.Apellido).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Request.Dni).NotEmpty().MaximumLength(15);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(120);
        RuleFor(x => x.Request.Username).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Request.FechaNacimiento).LessThan(DateOnly.FromDateTime(DateTime.Today));
    }
}

public sealed class UpdatePersonaCommandValidator : AbstractValidator<UpdatePersonaCommand>
{
    public UpdatePersonaCommandValidator()
    {
        RuleFor(x => x.Request.Nombre).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Request.Apellido).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Request.Dni).NotEmpty().MaximumLength(15);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(120);
        RuleFor(x => x.Request.Username).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Request.FechaNacimiento).LessThan(DateOnly.FromDateTime(DateTime.Today));
    }
}

public sealed class CreateAsignacionDocenteCommandValidator : AbstractValidator<CreateAsignacionDocenteCommand>
{
    public CreateAsignacionDocenteCommandValidator()
    {
        RuleFor(x => x.Request.IdMateria).GreaterThan(0);
        RuleFor(x => x.Request.IdPersona).GreaterThan(0);
        RuleFor(x => x.Request.FechaHasta)
            .GreaterThanOrEqualTo(x => x.Request.FechaDesde)
            .When(x => x.Request.FechaHasta.HasValue);
    }
}

public sealed class CreateCorrelatividadCommandValidator : AbstractValidator<CreateCorrelatividadCommand>
{
    public CreateCorrelatividadCommandValidator()
    {
        RuleFor(x => x.IdMateria).GreaterThan(0);
        RuleFor(x => x.Request.IdMateriaRequisito).GreaterThan(0);
    }
}

public sealed class CreateInscripcionComisionCommandValidator : AbstractValidator<CreateInscripcionComisionCommand>
{
    public CreateInscripcionComisionCommandValidator()
    {
        RuleFor(x => x.Request.IdInscripcionCarrera).GreaterThan(0);
        RuleFor(x => x.Request.IdComision).GreaterThan(0);
    }
}

public sealed class UpdateCondicionInscripcionComisionCommandValidator
    : AbstractValidator<UpdateCondicionInscripcionComisionCommand>
{
    public UpdateCondicionInscripcionComisionCommandValidator()
    {
        RuleFor(x => x.Request.Condicion).IsInEnum();
    }
}

public sealed class CreateExamenCommandValidator : AbstractValidator<CreateExamenCommand>
{
    public CreateExamenCommandValidator()
    {
        RuleFor(x => x.Request.IdMateria).GreaterThan(0);
        RuleFor(x => x.Request.IdPeriodo).GreaterThan(0).When(x => x.Request.IdPeriodo.HasValue);
    }
}

public sealed class UpdateExamenCommandValidator : AbstractValidator<UpdateExamenCommand>
{
    public UpdateExamenCommandValidator()
    {
        RuleFor(x => x.Request.IdMateria).GreaterThan(0);
        RuleFor(x => x.Request.IdPeriodo).GreaterThan(0).When(x => x.Request.IdPeriodo.HasValue);
    }
}

public sealed class CreateInscripcionExamenCommandValidator : AbstractValidator<CreateInscripcionExamenCommand>
{
    public CreateInscripcionExamenCommandValidator()
    {
        RuleFor(x => x.Request.IdInscripcionCarrera).GreaterThan(0);
        RuleFor(x => x.Request.IdExamen).GreaterThan(0);
    }
}

public sealed class CerrarActaExamenCommandValidator : AbstractValidator<CerrarActaExamenCommand>
{
    public CerrarActaExamenCommandValidator()
    {
        RuleFor(x => x.Request.Items).NotNull();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.IdInscripcionExamen).GreaterThan(0);
            item.RuleFor(x => x.Nota).InclusiveBetween(1m, 10m).When(x => x.Nota.HasValue);
        });
    }
}
