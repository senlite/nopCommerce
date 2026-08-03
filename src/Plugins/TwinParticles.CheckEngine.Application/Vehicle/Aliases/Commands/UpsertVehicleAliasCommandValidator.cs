using FluentValidation;

namespace TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands;

public sealed class UpsertVehicleAliasCommandValidator : AbstractValidator<UpsertVehicleAliasCommand>
{
    public UpsertVehicleAliasCommandValidator()
    {
        RuleFor(x => x.NodeType)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.NodeId)
            .GreaterThan(0);

        RuleFor(x => x.Locale)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.AliasText)
            .NotEmpty()
            .MaximumLength(256);
    }
}
