using FluentValidation;
using GestionAcademica.Application.UseCases.Certificados;

namespace GestionAcademica.Application.Validators;

public sealed class GenerarCertificadoCommandValidator : AbstractValidator<GenerarCertificadoCommand>
{
    public GenerarCertificadoCommandValidator()
    {
        RuleFor(x => x.Request.IdInscripcionCarrera).GreaterThan(0);
        RuleFor(x => x.Request.Tipo).IsInEnum();
    }
}
