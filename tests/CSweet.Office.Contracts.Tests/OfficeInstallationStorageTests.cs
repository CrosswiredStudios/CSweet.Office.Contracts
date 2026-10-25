using System.Text.Json;
using CSweet.Office.Contracts.ControlPlane;
using Xunit;

namespace CSweet.Office.Contracts.Tests;

public sealed class OfficeInstallationStorageTests
{
    [Fact]
    public void StorageRoundTripsWithoutChangingLegacyHandoffs()
    {
        var legacy = new AssistedOfficePreflightResponse(true, null, "ready", Guid.NewGuid(), "none", true);
        Assert.Null(JsonSerializer.Deserialize<AssistedOfficePreflightResponse>(JsonSerializer.Serialize(legacy))!.Storage);
        var current = legacy with { Storage = new("volume-guid", @"D:\CSweet\Office\program", @"D:\CSweet\Office\data") };
        Assert.Equal(current, JsonSerializer.Deserialize<AssistedOfficePreflightResponse>(JsonSerializer.Serialize(current)));
        var oldJson = """{"Succeeded":true,"ErrorCode":null,"Message":"ready","AssistedSetupSessionId":null,"ExistingInstallationAction":"none","ProceedToRedemption":true}""";
        Assert.Null(JsonSerializer.Deserialize<AssistedOfficePreflightResponse>(oldJson)!.Storage);
    }
}
