using System.Collections.Generic;
using FluentAssertions;
using Nop.Plugin.Payments.Paymob.Services;
using NUnit.Framework;

namespace Nop.Plugin.Payments.Paymob.Tests;

[TestFixture]
public class PaymobHmacValidatorTests
{
    private static Dictionary<string, string?> SampleFields() => new()
    {
        ["amount_cents"] = "15000",
        ["created_at"] = "2026-08-13T21:00:00",
        ["currency"] = "EGP",
        ["error_occured"] = "false",
        ["has_parent_transaction"] = "false",
        ["id"] = "123456",
        ["integration_id"] = "99887",
        ["is_3d_secure"] = "true",
        ["is_auth"] = "false",
        ["is_capture"] = "false",
        ["is_refunded"] = "false",
        ["is_standalone_payment"] = "true",
        ["is_voided"] = "false",
        ["order.id"] = "654321",
        ["owner"] = "42",
        ["pending"] = "false",
        ["source_data.pan"] = "2346",
        ["source_data.sub_type"] = "MasterCard",
        ["source_data.type"] = "card",
        ["success"] = "true"
    };

    [Test]
    public void Compute_Should_Be_Deterministic_For_Same_Input()
    {
        var validator = new PaymobHmacValidator();
        var a = validator.Compute(SampleFields(), "secret-key");
        var b = validator.Compute(SampleFields(), "secret-key");

        a.Should().Be(b);
        a.Should().HaveLength(128, "HMAC-SHA512 hex is 128 characters");
        a.Should().MatchRegex("^[0-9a-f]+$", "output is lowercase hex");
    }

    [Test]
    public void Verify_Should_Accept_A_Correct_Signature_Case_Insensitively()
    {
        var validator = new PaymobHmacValidator();
        var fields = SampleFields();
        var signature = validator.Compute(fields, "secret-key");

        validator.Verify(fields, "secret-key", signature).Should().BeTrue();
        validator.Verify(fields, "secret-key", signature.ToUpperInvariant()).Should().BeTrue();
    }

    [Test]
    public void Verify_Should_Reject_A_Tampered_Field()
    {
        var validator = new PaymobHmacValidator();
        var fields = SampleFields();
        var signature = validator.Compute(fields, "secret-key");

        // An attacker flips the transaction to success without the secret.
        fields["success"] = "true ";
        fields["amount_cents"] = "1";
        validator.Verify(fields, "secret-key", signature).Should().BeFalse();
    }

    [Test]
    public void Verify_Should_Reject_A_Wrong_Secret()
    {
        var validator = new PaymobHmacValidator();
        var fields = SampleFields();
        var signature = validator.Compute(fields, "secret-key");

        validator.Verify(fields, "attacker-key", signature).Should().BeFalse();
    }

    [Test]
    public void Verify_Should_Reject_Missing_Signature()
    {
        var validator = new PaymobHmacValidator();
        validator.Verify(SampleFields(), "secret-key", null).Should().BeFalse();
        validator.Verify(SampleFields(), "secret-key", "").Should().BeFalse();
    }

    [Test]
    public void Field_Order_Should_Match_Paymob_Contract()
    {
        // Order is contractual: a change silently breaks every callback signature.
        PaymobHmacValidator.SignedFieldOrder.Should().ContainInOrder(
            "amount_cents", "created_at", "currency", "id", "integration_id", "order.id", "success");
        PaymobHmacValidator.SignedFieldOrder.Should().HaveCount(20);
    }
}
