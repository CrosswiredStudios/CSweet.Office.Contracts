using System.Text.Json;
using CSweet.Office.Contracts.ControlPlane;
using CSweet.Office.Contracts.Guest;
using CSweet.Office.Contracts.Security;
using CSweet.Office.Contracts.Workloads;
using Google.Protobuf;
using Xunit;

namespace CSweet.Office.Contracts.Tests;

public sealed class GuestProtocol12ContractTests
{
    private static RuntimeWorkloadSpecification Specification()
    {
        var digest = "sha256:" + new string('a', 64);
        return new RuntimeWorkloadSpecification(Guid.NewGuid(), new("image", "1", digest, "linux", "x64"),
            new(1, 100, 512, 512, 128, 1024, TimeSpan.FromMinutes(1)),
            new(Guid.NewGuid(), ProtocolVersions.GuestBrokerV12, "test-token-not-a-secret", digest, digest,
                DateTimeOffset.UtcNow.AddMinutes(1)),
            new(digest, "signature", "1", "linux", "x64"), new(Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid()), ["agent"]);
    }

    [Fact]
    public void PersistentDiskDefaultsAbsentAndEveryFieldChangesTheSignedDigest()
    {
        var spec = Specification();
        var none = JsonSerializer.Serialize(spec);
        Assert.Null(JsonSerializer.Deserialize<RuntimeWorkloadSpecification>(none)!.PersistentDisk);

        var disk = new RuntimePersistentDisk(Guid.NewGuid(), 1, 4096);
        disk.Validate();
        var attached = JsonSerializer.Serialize(spec with { PersistentDisk = disk });
        Assert.Equal(disk, JsonSerializer.Deserialize<RuntimeWorkloadSpecification>(attached)!.PersistentDisk);
        Assert.NotEqual(AssignmentEnvelope.Digest(none), AssignmentEnvelope.Digest(attached));
        foreach (var changed in new[] { disk with { DiskId = Guid.NewGuid() }, disk with { Generation = 2 },
                     disk with { MaximumMegabytes = 8192 } })
            Assert.NotEqual(AssignmentEnvelope.Digest(attached),
                AssignmentEnvelope.Digest(JsonSerializer.Serialize(spec with { PersistentDisk = changed })));
    }

    [Fact]
    public void SpecificationWrittenBeforePersistentDisksStillReads()
    {
        var legacy = JsonSerializer.Serialize(Specification()).Replace(",\"PersistentDisk\":null", "", StringComparison.Ordinal);
        Assert.DoesNotContain("PersistentDisk", legacy, StringComparison.Ordinal);
        Assert.Null(JsonSerializer.Deserialize<RuntimeWorkloadSpecification>(legacy)!.PersistentDisk);
    }

    [Theory]
    [InlineData(0L, 4096)]
    [InlineData(-1L, 4096)]
    [InlineData(1L, 63)]
    [InlineData(1L, 1_048_577)]
    public void PersistentDiskRejectsOutOfRangeValues(long generation, int megabytes) =>
        Assert.Throws<InvalidDataException>(() => new RuntimePersistentDisk(Guid.NewGuid(), generation, megabytes).Validate());

    [Fact]
    public void PersistentDiskRejectsAnEmptyIdentifier() =>
        Assert.Throws<InvalidDataException>(() => new RuntimePersistentDisk(Guid.Empty, 1, 4096).Validate());

    [Fact]
    public void ProviderSupportDefaultsFalseOnBothTransports()
    {
        Assert.False(new OfficeProviderInventory().SupportsPersistentDisk);
        var request = new RegisterOfficeProviderRequest("provider", "1", ProtocolVersions.GuestBrokerV12,
            "sha256:" + new string('a', 64), "suite", "sha256:" + new string('b', 64), DateTimeOffset.UtcNow, null,
            true, true, true, true, null);
        Assert.False(request.SupportsPersistentDisk);
        Assert.True(JsonSerializer.Deserialize<RegisterOfficeProviderRequest>(
            JsonSerializer.Serialize(request with { SupportsPersistentDisk = true }))!.SupportsPersistentDisk);
    }

    [Fact]
    public void StreamedBodyFramesRoundTripAndLeaveExistingMessagesUnchanged()
    {
        var requestId = Guid.NewGuid().ToString("N");
        var frame = new GuestEnvelope { ProtocolVersion = ProtocolVersions.GuestBrokerV12, MessageId = Guid.NewGuid().ToString("N"),
            ProxyBody = new ProxyBodyFrame { RequestId = requestId, Direction = ProxyBodyFrame.Types.Direction.Response,
                Operation = ProxyBodyFrame.Types.Operation.Data, Sequence = 3, Data = ByteString.CopyFrom(new byte[] { 1, 2, 3 }) } };
        var parsed = GuestEnvelope.Parser.ParseFrom(frame.ToByteArray());
        Assert.Equal(frame, parsed);
        Assert.Equal(GuestEnvelope.BodyOneofCase.ProxyBody, parsed.BodyCase);

        var inline = new ProxyRequest { RequestId = requestId, Purpose = "mcp.runtime", Method = "POST", Path = "/mcp",
            Body = ByteString.CopyFromUtf8("{}") };
        var reread = ProxyRequest.Parser.ParseFrom(inline.ToByteArray());
        Assert.Equal(inline, reread);
        Assert.False(reread.BodyStreamed);
        Assert.False(ProxyResponse.Parser.ParseFrom(
            new ProxyResponse { RequestId = requestId, StatusCode = 200 }.ToByteArray()).BodyStreamed);
        Assert.False(GuestBootConfiguration.Parser.ParseFrom(
            new GuestBootConfiguration { PublicInternet = true }.ToByteArray()).PersistentDisk);
    }

    [Fact]
    public void BodyFrameValidationAcceptsEachOperation()
    {
        ProxyBodyStreaming.Validate(Frame(ProxyBodyFrame.Types.Operation.Data,
            data: new byte[ProxyBodyStreaming.MaximumDataBytes], sequence: 7));
        ProxyBodyStreaming.Validate(Frame(ProxyBodyFrame.Types.Operation.End, sequence: 8));
        ProxyBodyStreaming.Validate(Frame(ProxyBodyFrame.Types.Operation.Credit));
        ProxyBodyStreaming.Validate(Frame(ProxyBodyFrame.Types.Operation.Abort, errorCode: "upstream-failed"));
        ProxyBodyStreaming.ValidateHeader(new ProxyRequest { BodyStreamed = true });
        ProxyBodyStreaming.ValidateHeader(new ProxyResponse { BodyStreamed = true });
        ProxyBodyStreaming.ValidateHeader(new ProxyRequest { Body = ByteString.CopyFromUtf8("{}") });
    }

    [Fact]
    public void BodyFrameValidationRejectsMalformedFrames()
    {
        var malformed = new[]
        {
            new ProxyBodyFrame(),
            Frame(ProxyBodyFrame.Types.Operation.Data),
            Frame(ProxyBodyFrame.Types.Operation.Data, data: new byte[ProxyBodyStreaming.MaximumDataBytes + 1]),
            Frame(ProxyBodyFrame.Types.Operation.Data, data: [1], errorCode: "unexpected"),
            Frame(ProxyBodyFrame.Types.Operation.Data, data: [1], sequence: -1),
            Frame(ProxyBodyFrame.Types.Operation.End, data: [1]),
            Frame(ProxyBodyFrame.Types.Operation.Credit, sequence: 1),
            Frame(ProxyBodyFrame.Types.Operation.Credit, data: [1]),
            Frame(ProxyBodyFrame.Types.Operation.Abort),
            Frame(ProxyBodyFrame.Types.Operation.Abort, errorCode: "bad\ncode"),
            Frame(ProxyBodyFrame.Types.Operation.Abort, errorCode: new string('e', ProxyBodyStreaming.MaximumErrorCodeLength + 1)),
            Frame(ProxyBodyFrame.Types.Operation.Abort, errorCode: "failed", sequence: 1),
            Frame((ProxyBodyFrame.Types.Operation)9),
            Frame(ProxyBodyFrame.Types.Operation.Credit, direction: (ProxyBodyFrame.Types.Direction)9),
            Frame(ProxyBodyFrame.Types.Operation.Credit, requestId: new string('r', ProxyBodyStreaming.MaximumRequestIdLength + 1))
        };
        foreach (var frame in malformed)
            Assert.Throws<InvalidDataException>(() => ProxyBodyStreaming.Validate(frame));
        Assert.Throws<InvalidDataException>(() => ProxyBodyStreaming.ValidateHeader(
            new ProxyRequest { BodyStreamed = true, Body = ByteString.CopyFromUtf8("x") }));
        Assert.Throws<InvalidDataException>(() => ProxyBodyStreaming.ValidateHeader(
            new ProxyResponse { BodyStreamed = true, Body = ByteString.CopyFromUtf8("x") }));
    }

    private static ProxyBodyFrame Frame(ProxyBodyFrame.Types.Operation operation, byte[]? data = null, long sequence = 0,
        string errorCode = "", ProxyBodyFrame.Types.Direction direction = ProxyBodyFrame.Types.Direction.Request,
        string requestId = "request") =>
        new()
        {
            RequestId = requestId, Direction = direction, Operation = operation, Sequence = sequence,
            Data = ByteString.CopyFrom(data ?? []), ErrorCode = errorCode
        };
}
