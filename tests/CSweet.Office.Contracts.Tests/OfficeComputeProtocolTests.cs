using System.Text;
using CSweet.Office.Contracts.ControlPlane;
using CSweet.Office.Contracts.Security;
using Google.Protobuf;
using Xunit;

namespace CSweet.Office.Contracts.Tests;

public sealed class OfficeComputeProtocolTests
{
    [Fact]
    public void Receipt_encoding_binds_both_identities_sequence_and_exact_digest()
    {
        var office = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var operation = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var digest = "sha256:" + new string('a', 64);
        var expected = "CSweet.Office.ComputeReceipt.v1\n11111111-1111-1111-1111-111111111111\n" +
            "22222222-2222-2222-2222-222222222222\n123\nsha256:" + new string('a', 64);
        Assert.Equal(Encoding.ASCII.GetBytes(expected), OfficeComputeReceiptEncoding.Payload(office, operation, 123, digest));
        Assert.NotEqual(OfficeComputeReceiptEncoding.Payload(office, operation, 1, digest),
            OfficeComputeReceiptEncoding.MaintenancePayload(office, operation, digest));
        Assert.Throws<InvalidDataException>(() => OfficeComputeReceiptEncoding.Payload(office, operation, 0, digest));
        Assert.Throws<InvalidDataException>(() => OfficeComputeReceiptEncoding.Payload(office, operation, 1, digest.ToUpperInvariant()));
    }

    [Fact]
    public void Offer_is_optional_and_compute_dispatch_bytes_survive_the_control_envelope()
    {
        Assert.Null(OfficeHeartbeat.Parser.ParseFrom(Array.Empty<byte>()).ComputeOffer);
        var heartbeat = new OfficeHeartbeat { ComputeOffer = new() { TemplateId = "isolated-linux", CpuCount = 2 } };
        Assert.Equal(heartbeat, OfficeHeartbeat.Parser.ParseFrom(heartbeat.ToByteArray()));
        var dispatch = new HeadquartersControlMessage { ComputeDispatch = new() { PacketJson = ByteString.CopyFrom(0, 17, 255) } };
        Assert.Equal(dispatch, HeadquartersControlMessage.Parser.ParseFrom(dispatch.ToByteArray()));
    }
}
