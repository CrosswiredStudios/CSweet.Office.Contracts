using System.Text.Json;
using CSweet.Office.Contracts.ControlPlane;
using Xunit;

namespace CSweet.Office.Contracts.Tests;

public sealed class OfficeUpdatePreparationTests
{
    [Fact]
    public void PreparationReceiptAndInstallationHandoffRoundTripSeparately()
    {
        var begin = new BeginOfficeUpdatePreparationRequest("handoff", "machine", "windows", "x64");
        Assert.Equal(begin, JsonSerializer.Deserialize<BeginOfficeUpdatePreparationRequest>(JsonSerializer.Serialize(begin)));
        var poll = new OfficeUpdatePreparationRequest(Guid.NewGuid(), "receipt", "machine", "windows", "x64", true);
        Assert.Equal(poll, JsonSerializer.Deserialize<OfficeUpdatePreparationRequest>(JsonSerializer.Serialize(poll)));
        var preparation = new OfficeUpdatePreparationResponse(true, null, "preparing", "0.12.0", "receipt");
        Assert.Null(preparation.LaunchUri);
        var installation = preparation with { State = "installing", Receipt = null, LaunchUri = "csweet-office://enroll/v1#handoff=next" };
        Assert.Null(installation.Receipt);
        Assert.Equal(installation, JsonSerializer.Deserialize<OfficeUpdatePreparationResponse>(JsonSerializer.Serialize(installation)));
    }
}
