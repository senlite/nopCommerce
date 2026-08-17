using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class PayoutStatementBuilderTests
{
    private readonly PayoutStatementBuilder _builder = new();

    [Test]
    public void Build_Should_Compute_Net_As_Sales_Minus_Commission_Minus_Refunds_Plus_Adjustments()
    {
        var statement = _builder.Build(
            vendorId: 1,
            periodStartUtc: DateTime.UtcNow.AddDays(-30),
            periodEndUtc: DateTime.UtcNow,
            sourceLines:
            [
                new PayoutSourceLine { OrderId = 1, OrderItemId = 10, LineSubtotalExclTax = 100m, CommissionAmount = 10m, RefundAmount = 5m },
                new PayoutSourceLine { OrderId = 2, OrderItemId = 20, LineSubtotalExclTax = 50m, CommissionAmount = 5m, RefundAmount = 0m }
            ],
            adjustments: [new PayoutAdjustment { Amount = 2m, ReasonCode = "goodwill" }]);

        statement.GrossSales.Should().Be(150m);
        statement.TotalCommission.Should().Be(15m);
        statement.TotalRefunds.Should().Be(5m);
        statement.TotalAdjustments.Should().Be(2m);
        statement.NetPayout.Should().Be(132m);
        statement.Lines.Should().HaveCount(2);
    }

    [Test]
    public void Reconcile_Should_Pass_Within_One_Cent()
    {
        var result = _builder.Reconcile(100.005m, 100m);
        result.WithinTolerance.Should().BeTrue();
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void Reconcile_Should_Fail_Outside_Tolerance()
    {
        var result = _builder.Reconcile(100m, 99m);
        result.WithinTolerance.Should().BeFalse();
        result.Succeeded.Should().BeFalse();
    }
}
