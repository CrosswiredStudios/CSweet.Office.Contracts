using CSweet.Office.Contracts.Security;
using Xunit;

namespace CSweet.Office.Contracts.Tests;

public sealed class AssignmentEncodingCompatibilityTests
{
    [Fact]
    public void Shared_encoder_preserves_the_published_office_v1_bytes()
    {
        // Captured from the original 0.5.0 implementation before extraction.
        const string expected = "AAAAI0NTd2VldC5PZmZpY2UuV29ya2xvYWRBdXRob3JpemF0aW9uAREREREREREREREREREREREiIiIiIiIiIiIiIiIiIiIiMzMzMzMzMzMzMzMzMzMzMwAAAAAAAAAEAAAAC2h5cGVydi1nZW4yAAAAR3NoYTI1NjphYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhAAAAAGtJ0NQAAAAAa0nSAA==";
        var actual = AssignmentEnvelope.Payload(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("33333333-3333-3333-3333-333333333333"),
            4, "hyperv-gen2", "sha256:" + new string('a', 64),
            DateTimeOffset.FromUnixTimeSeconds(1799999700), DateTimeOffset.FromUnixTimeSeconds(1800000000));
        Assert.Equal(expected, Convert.ToBase64String(actual));
    }
}
