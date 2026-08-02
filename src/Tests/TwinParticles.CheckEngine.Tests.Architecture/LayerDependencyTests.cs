using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class LayerDependencyTests
{
    [Test]
    public void Domain_Should_Not_Reference_Host_Or_Infrastructure_Assemblies()
    {
        var referenceNames = typeof(TwinParticles.CheckEngine.Domain.AssemblyReference).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        referenceNames.Should().NotContain(name => name!.StartsWith("Nop."));
        referenceNames.Should().NotContain(name => name!.StartsWith("Microsoft.AspNetCore"));
        referenceNames.Should().NotContain("TwinParticles.CheckEngine.Application");
        referenceNames.Should().NotContain("TwinParticles.CheckEngine.Infrastructure");
    }

    [Test]
    public void Application_Should_Not_Reference_Infrastructure()
    {
        var referenceNames = typeof(TwinParticles.CheckEngine.Application.DependencyInjection.ServiceCollectionExtensions).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        referenceNames.Should().NotContain("TwinParticles.CheckEngine.Infrastructure");
    }

    [Test]
    public void Infrastructure_Should_Not_Reference_Application_Or_Host()
    {
        var referenceNames = typeof(TwinParticles.CheckEngine.Infrastructure.DependencyInjection.ServiceCollectionExtensions).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        referenceNames.Should().NotContain("TwinParticles.CheckEngine.Application");
        referenceNames.Should().NotContain("TwinParticles.CheckEngine");
    }
}
