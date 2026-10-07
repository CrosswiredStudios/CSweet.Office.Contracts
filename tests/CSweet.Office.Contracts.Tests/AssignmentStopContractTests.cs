using CSweet.Office.Contracts.ControlPlane;
using Google.Protobuf;
using Xunit;

namespace CSweet.Office.Contracts.Tests;

public sealed class AssignmentStopContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StopReportAndReceiptPreserveBindings(bool neverCreated)
    {
        var report = new OfficeControlMessage
        {
            OfficeId = Guid.NewGuid().ToString("D"), SessionEpoch = 9, ProtocolVersion = "1.0",
            AssignmentStopped = new AssignmentStopped
            {
                AssignmentId = Guid.NewGuid().ToString("D"), FencingEpoch = 42,
                ProviderId = "hyperv", ProviderInstanceId = neverCreated ? "" : "vm-123",
                NeverCreated = neverCreated
            }
        };
        Assert.Equal(report, OfficeControlMessage.Parser.ParseFrom(report.ToByteArray()));
        var receipt = new HeadquartersControlMessage
        {
            OfficeId = report.OfficeId, SessionEpoch = report.SessionEpoch, ProtocolVersion = "1.0",
            AssignmentStopReceipt = new AssignmentStopReceipt
            {
                AssignmentId = report.AssignmentStopped.AssignmentId,
                FencingEpoch = report.AssignmentStopped.FencingEpoch, Accepted = true
            }
        };
        Assert.Equal(receipt, HeadquartersControlMessage.Parser.ParseFrom(receipt.ToByteArray()));
        // An unknown future field must survive an older reader's forwarding round trip.
        var bytes = report.ToByteArray().Concat(new byte[] { 0xf8, 0x07, 0x01 }).ToArray();
        Assert.Equal(bytes, OfficeControlMessage.Parser.ParseFrom(bytes).ToByteArray());
    }
}
