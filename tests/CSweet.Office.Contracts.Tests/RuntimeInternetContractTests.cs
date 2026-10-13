using System.Text.Json;
using CSweet.Office.Contracts.Workloads;
using CSweet.Office.Contracts.Guest;
using CSweet.Office.Contracts.Security;
using Google.Protobuf;
using Xunit;

namespace CSweet.Office.Contracts.Tests;

public sealed class RuntimeInternetContractTests
{
    [Fact]
    public void OptionalPolicyDefaultsClosedAndEveryPolicyChangeChangesTheSignedDigest()
    {
        var now = DateTimeOffset.UtcNow;
        var digest = "sha256:" + new string('a', 64);
        var spec = new RuntimeWorkloadSpecification(Guid.NewGuid(), new("image", "1", digest, "linux", "x64"),
            new(1, 100, 512, 512, 128, 1024, TimeSpan.FromMinutes(1)),
            new(Guid.NewGuid(), "1.1", "test-token-not-a-secret", digest, digest, now.AddMinutes(1)),
            new(digest, "signature", "1", "linux", "x64"), new(Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid()), ["agent"]);
        var restricted = JsonSerializer.Serialize(spec);
        Assert.Null(JsonSerializer.Deserialize<RuntimeWorkloadSpecification>(restricted)!.NetworkPolicy);
        var enabled = JsonSerializer.Serialize(spec with { NetworkPolicy = new(RuntimeNetworkPolicy.PublicInternet, 1) });
        Assert.NotEqual(AssignmentEnvelope.Digest(restricted), AssignmentEnvelope.Digest(enabled));
        new RuntimeNetworkPolicy(RuntimeNetworkPolicy.PublicInternetIpv4, 1).Validate();
        Assert.NotEqual(AssignmentEnvelope.Digest(enabled), AssignmentEnvelope.Digest(JsonSerializer.Serialize(
            spec with { NetworkPolicy = new(RuntimeNetworkPolicy.PublicInternetIpv4, 1) })));
        Assert.NotEqual(AssignmentEnvelope.Digest(enabled), AssignmentEnvelope.Digest(JsonSerializer.Serialize(
            spec with { NetworkPolicy = new(RuntimeNetworkPolicy.PublicInternet, 2) })));
        Assert.Throws<InvalidDataException>(() => new RuntimeNetworkPolicy("private", 1).Validate());
        Assert.Throws<InvalidDataException>(() => new RuntimeNetworkPolicy(RuntimeNetworkPolicy.PublicInternet, 0).Validate());
        Assert.Throws<InvalidDataException>(() => new RuntimeNetworkPolicy(RuntimeNetworkPolicy.PublicInternet, 1, ["invalid"]).Validate());
    }

    [Fact]
    public void GuestInternetFrameRoundTripsWithoutChangingExistingEnvelopeFields()
    {
        var frame = new GuestEnvelope { ProtocolVersion = "1.1", MessageId = Guid.NewGuid().ToString("N"),
            Egress = new EgressFrame { FlowId = Guid.NewGuid().ToString("N"), Address = "8.8.8.8", Port = 53,
                Udp = true, Operation = EgressFrame.Types.Operation.Open } };
        Assert.Equal(frame, GuestEnvelope.Parser.ParseFrom(frame.ToByteArray()));
        var old = new GuestEnvelope { ProtocolVersion = "1.0", MessageId = Guid.NewGuid().ToString("N"), Health = new GuestHealth { State = "running" } };
        Assert.Equal(GuestEnvelope.BodyOneofCase.Health, GuestEnvelope.Parser.ParseFrom(old.ToByteArray()).BodyCase);
    }
}
