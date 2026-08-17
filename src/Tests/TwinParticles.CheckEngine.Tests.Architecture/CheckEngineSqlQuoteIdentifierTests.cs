using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CheckEngineSqlQuoteIdentifierTests
{
    [Test]
    public void QuoteIdentifier_Should_Use_Dialect_Quotes()
    {
        var quoted = CheckEngineSql.QuoteIdentifier("RowCount");

        if (CheckEngineSql.IsMySql())
            quoted.Should().Be("`RowCount`");
        else
            quoted.Should().Be("[RowCount]");
    }
}
