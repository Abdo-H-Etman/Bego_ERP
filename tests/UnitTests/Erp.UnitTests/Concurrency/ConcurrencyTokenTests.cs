using Erp.SharedKernel;
using FluentAssertions;
using Xunit;

namespace Erp.UnitTests.Concurrency;

public class ConcurrencyTokenTests
{
    [Fact]
    public void VersionedEntity_Should_UseDateTime_ConcurrencyTimestamp()
    {
        var entity = new TestVersionedEntity();
        var now = DateTime.UtcNow;
        entity.ConcurrencyTimestamp = now;

        entity.ConcurrencyTimestamp.Should().Be(now);
    }

    private class TestVersionedEntity : IVersionedEntity
    {
        public DateTime ConcurrencyTimestamp { get; set; }
    }
}
