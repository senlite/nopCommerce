using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// The plugin is wired through Autofac, which selects a constructor by parameter count and throws
/// "Cannot choose between multiple constructors with equal length" when a type offers two public
/// constructors of the same arity.
///
/// Unit tests construct these services directly and so never exercised container activation. Two
/// services shipped with an ambiguous pair of single-argument constructors, which made the whole
/// import pipeline and the AI admin controllers fail with HTTP 500 at runtime.
/// </summary>
[TestFixture]
public class DependencyInjectionConstructorAmbiguityTests
{
    private static readonly Assembly[] ResolvableAssemblies =
    [
        typeof(AiContentCandidateService).Assembly,
        typeof(IAllowedSpecificationKeyCatalog).Assembly,
        typeof(CheckEngineAiOptions).Assembly
    ];

    [Test]
    public void No_Resolvable_Service_Should_Expose_Ambiguous_Same_Arity_Constructors()
    {
        var offenders = new List<string>();

        foreach (var assembly in ResolvableAssemblies.Distinct())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract || !type.IsPublic || type.IsGenericTypeDefinition)
                    continue;

                var ambiguousArities = type
                    .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .GroupBy(constructor => constructor.GetParameters().Length)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();

                foreach (var arity in ambiguousArities)
                    offenders.Add($"{type.FullName} has {arity}-argument constructors that the container cannot choose between");
            }
        }

        offenders.Should().BeEmpty(
            "Autofac picks a constructor by parameter count; expose extra shapes as static factory methods instead");
    }

    [Test]
    public void AllowedSpecificationKeyCatalog_Should_Expose_Explicit_Keys_Through_A_Factory()
    {
        var catalog = AllowedSpecificationKeyCatalog.ForKeys(["Material", "Position"]);

        catalog.IsAllowed("Material").Should().BeTrue();
        catalog.IsAllowed("Sparkle Factor").Should().BeFalse();

        typeof(AllowedSpecificationKeyCatalog)
            .GetConstructors()
            .Should().HaveCount(1, "a single public constructor keeps the type container-resolvable");
    }

    [Test]
    public void AutomotiveGlossaryService_Should_Expose_Explicit_Terms_Through_A_Factory()
    {
        var glossary = TwinParticles.CheckEngine.Application.L10n.AutomotiveGlossaryService.ForTerms(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["brake pad"] = "فحمات مخصصة" });

        glossary.GetTerms()["brake pad"].Should().Be("فحمات مخصصة");

        typeof(TwinParticles.CheckEngine.Application.L10n.AutomotiveGlossaryService)
            .GetConstructors()
            .Should().HaveCount(1, "a single public constructor keeps the type container-resolvable");
    }
}
