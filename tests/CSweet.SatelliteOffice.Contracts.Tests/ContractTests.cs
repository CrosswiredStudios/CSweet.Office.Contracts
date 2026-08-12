using System.Text.Json;
using CSweet.SatelliteOffice.Contracts.ControlPlane;
using CSweet.SatelliteOffice.Contracts.Security;
using CSweet.SatelliteOffice.Contracts.Workloads;
using Xunit;
using Google.Protobuf;
using System.Buffers.Binary;
using CSweet.SatelliteOffice.Contracts.Guest;

namespace CSweet.SatelliteOffice.Contracts.Tests;

public sealed class ContractTests
{
    [Fact]
    public void Runtime_workload_round_trips()
    {
        var value = new RuntimeWorkloadSpecification(
            Guid.NewGuid(), new("guest", "1", "sha256:" + new string('a', 64), "linux", "x64"),
            new(1, 100, 512, 1024, 64, 4096, TimeSpan.FromMinutes(5)),
            new(Guid.NewGuid(), "1.0", new string('b', 32), "sha256:" + new string('a', 64), null, DateTimeOffset.UtcNow.AddMinutes(5)),
            new("sha256:" + new string('c', 64), "signature", "1", "linux", "x64"),
            new(Guid.NewGuid(), "business", Guid.NewGuid()), ["dotnet", "Agent.dll"]);

        var roundTrip = JsonSerializer.Deserialize<RuntimeWorkloadSpecification>(JsonSerializer.Serialize(value));
        Assert.NotNull(roundTrip);
        Assert.Equal(value.WorkloadId, roundTrip.WorkloadId);
        Assert.Equal(value.GuestImage, roundTrip.GuestImage);
        Assert.Equal(value.ResourceLimits, roundTrip.ResourceLimits);
        Assert.Equal(value.BrokerLease, roundTrip.BrokerLease);
        Assert.Equal(value.Artifact, roundTrip.Artifact);
        Assert.Equal(value.Identity, roundTrip.Identity);
        Assert.Equal(value.Entrypoint, roundTrip.Entrypoint);
    }

    [Fact]
    public void Assignment_payload_is_deterministic()
    {
        var office = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var assignment = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var expires = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        Assert.Equal(
            AssignmentEnvelope.Payload(office, assignment, 4, "digest", expires, "token"),
            AssignmentEnvelope.Payload(office, assignment, 4, "digest", expires, "token"));
        Assert.Equal(
            "11111111-1111-1111-1111-111111111111\n22222222-2222-2222-2222-222222222222\n4\ndigest\n1800000000\ntoken",
            System.Text.Encoding.UTF8.GetString(AssignmentEnvelope.Payload(office, assignment, 4, "digest", expires, "token")));
    }

    [Fact]
    public void Generated_gateway_message_round_trips()
    {
        var original = new SatelliteOfficeControlMessage
        {
            ProtocolVersion = ProtocolVersions.ControlPlaneV1,
            SatelliteOfficeId = Guid.NewGuid().ToString("D")
        };
        var parsed = SatelliteOfficeControlMessage.Parser.ParseFrom(original.ToByteArray());
        Assert.Equal(original.ProtocolVersion, parsed.ProtocolVersion);
        Assert.Equal(original.SatelliteOfficeId, parsed.SatelliteOfficeId);
    }

    [Fact]
    public void Generated_gateway_preserves_unknown_fields()
    {
        var known = new SatelliteOfficeControlMessage { ProtocolVersion = "1.0" }.ToByteArray();
        var bytes = known.Concat(new byte[] { 0x98, 0x06, 0x2a }).ToArray(); // field 99, varint 42
        var parsed = SatelliteOfficeControlMessage.Parser.ParseFrom(bytes);

        Assert.Equal(new byte[] { 0x98, 0x06, 0x2a }, parsed.ToByteArray()[^3..]);
        Assert.Equal("SatelliteOfficeGateway", SatelliteOfficeGateway.Descriptor.Name);
    }

    [Fact]
    public async Task Length_delimited_reader_rejects_oversized_frames_before_allocating()
    {
        var prefix = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(prefix, LengthDelimitedProtobuf.DefaultMaximumFrameBytes + 1);
        await using var stream = new MemoryStream(prefix);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            LengthDelimitedProtobuf.ReadAsync(stream, GuestEnvelope.Parser));
    }
}
