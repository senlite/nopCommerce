using System.Collections.Generic;
using FluentAssertions;
using Nop.Plugin.Payments.Paymob.Services;
using NUnit.Framework;

namespace Nop.Plugin.Payments.Paymob.Tests;

[TestFixture]
public class PaymobCallbackFieldReaderTests
{
    [Test]
    public void FromPairs_Should_Strip_Obj_Prefix_And_Read_Order_Guid()
    {
        var fields = PaymobCallbackFieldReader.FromPairs([
            ("obj.success", "true"),
            ("obj.merchant_order_id", "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            ("hmac", "abc")
        ]);

        PaymobCallbackFieldReader.IsSuccessful(fields).Should().BeTrue();
        PaymobCallbackFieldReader.TryReadOrderGuid(fields).Should().Be(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        PaymobCallbackFieldReader.ReadHmac(fields, null).Should().Be("abc");
    }
}
