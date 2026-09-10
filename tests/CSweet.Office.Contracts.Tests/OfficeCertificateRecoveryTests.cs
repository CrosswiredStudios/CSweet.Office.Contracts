using System.Text;
using System.Text.Json;
using CSweet.Office.Contracts.ControlPlane;
using Xunit;

namespace CSweet.Office.Contracts.Tests;

public sealed class OfficeCertificateRecoveryTests
{
    [Fact]
    public void ProofHasStableDomainAndOfficeBinding()
    {
        var id = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Assert.Equal("csweet-office-certificate-recovery-v1\n12345678-1234-1234-1234-123456789abc\nnonce",
            Encoding.UTF8.GetString(OfficeCertificateRecoveryProof.Payload(id, "nonce")));
        var proof = new OfficeCertificateRecoveryRequest("nonce", "signature");
        Assert.Equal(proof, JsonSerializer.Deserialize<OfficeCertificateRecoveryRequest>(JsonSerializer.Serialize(proof)));
    }
}
