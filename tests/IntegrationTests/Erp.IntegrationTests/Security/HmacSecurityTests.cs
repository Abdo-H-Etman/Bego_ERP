using Erp.Application.Abstractions.Security;
using FluentAssertions;
using Xunit;

namespace Erp.IntegrationTests.Security;

public class HmacSecurityTests
{
    [Fact]
    public void HmacService_Should_GenerateAndValidate_CorrectSignature()
    {
        var hmac = new HmacService();
        var payload = "{\"event\":\"invoice.sync\",\"invoiceId\":\"12345\"}";
        var secret = "super-secret-key-begohub-2026";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var signature = hmac.ComputeSignature(payload, secret, timestamp);
        signature.Should().NotBeNullOrWhiteSpace();

        var isValid = hmac.ValidateSignature(payload, secret, signature, timestamp);
        isValid.Should().BeTrue("Matching payload and secret within tolerance must validate");
    }

    [Fact]
    public void HmacService_Should_Reject_TamperedPayload()
    {
        var hmac = new HmacService();
        var payload = "{\"amount\": 100}";
        var secret = "test-secret";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var signature = hmac.ComputeSignature(payload, secret, timestamp);

        var isValid = hmac.ValidateSignature("{\"amount\": 999}", secret, signature, timestamp);
        isValid.Should().BeFalse("Tampered payload must fail validation");
    }

    [Fact]
    public void HmacService_Should_Reject_ExpiredTimestamp_AntiReplay()
    {
        var hmac = new HmacService();
        var payload = "{\"test\": true}";
        var secret = "test-secret";
        // 10 minutes ago
        var expiredTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();

        var signature = hmac.ComputeSignature(payload, secret, expiredTimestamp);

        var isValid = hmac.ValidateSignature(payload, secret, signature, expiredTimestamp);
        isValid.Should().BeFalse("Expired timestamps outside the 5-minute window must be rejected for anti-replay");
    }
}
