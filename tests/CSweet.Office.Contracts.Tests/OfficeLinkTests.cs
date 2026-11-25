using System.Security.Cryptography;
using System.Text;
using CSweet.Office.Contracts.ControlPlane;
using Xunit;

namespace CSweet.Office.Contracts.Tests;

public class OfficeLinkTests
{
    [Fact]
    public void LinkBindsTheDisplayedMachineAndExpiresAndOnlyItsCreatorCanOpenTheGrant()
    {
        using var key = RSA.Create(2048);
        using var other = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var request = new OfficeLinkRequest("Office 02", Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()), now.AddMinutes(15).ToUnixTimeSeconds());
        Assert.True(request.IsValid(now));
        Assert.False(request.IsValid(now.AddMinutes(15)));
        Assert.False((request with { Key = "wrong" }).IsValid(now));
        Assert.NotEqual(request.Id, (request with { Machine = "Office 03" }).Id);
        var encrypted = Convert.FromBase64String(request.SealToken("one-use"));
        Assert.ThrowsAny<CryptographicException>(() => other.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256));
        Assert.Equal("one-use", Encoding.UTF8.GetString(key.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256)));
    }
}
