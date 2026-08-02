using System;
using FluentAssertions;
using FluentMigrator;
using Nop.Data.Migrations;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class MigrationConventionsTests
{
    [Test]
    public void InitialBaselineMigration_Should_Be_AutoReversing()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.InitialBaselineMigration);

        migrationType.IsSubclassOf(typeof(AutoReversingMigration)).Should().BeTrue();
    }

    [Test]
    public void InitialBaselineMigration_Should_Target_Installation_Process()
    {
        var migrationType = typeof(TwinParticles.CheckEngine.Infrastructure.Migrations.InitialBaselineMigration);
        var attribute = Attribute.GetCustomAttribute(migrationType, typeof(NopMigrationAttribute)) as NopMigrationAttribute;

        attribute.Should().NotBeNull();
        attribute!.TargetMigrationProcess.Should().Be(MigrationProcessType.Installation);
    }
}
