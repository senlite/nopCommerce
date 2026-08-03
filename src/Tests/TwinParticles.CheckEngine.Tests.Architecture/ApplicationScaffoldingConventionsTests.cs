using System;
using FluentAssertions;
using FluentValidation;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ApplicationScaffoldingConventionsTests
{
    [Test]
    public void UpsertVehicleAliasCommand_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands.UpsertVehicleAliasCommand);

        type.GetProperty("NodeType").Should().NotBeNull();
        type.GetProperty("NodeId").Should().NotBeNull();
        type.GetProperty("Locale").Should().NotBeNull();
        type.GetProperty("AliasText").Should().NotBeNull();
    }

    [Test]
    public void UpsertVehicleAliasCommandValidator_Should_Inherit_AbstractValidator()
    {
        var validatorType = typeof(TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands.UpsertVehicleAliasCommandValidator);
        validatorType.IsSubclassOf(typeof(AbstractValidator<TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands.UpsertVehicleAliasCommand>)).Should().BeTrue();
    }

    [Test]
    public void VehicleAliasApplicationService_Should_Expose_Cqrs_Operations()
    {
        var type = typeof(TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services.VehicleAliasApplicationService);

        type.GetMethod("UpsertAsync").Should().NotBeNull();
        type.GetMethod("SearchAsync").Should().NotBeNull();
    }
}
