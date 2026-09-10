using System.Text;

namespace CSweet.Office.Contracts.ControlPlane;

public sealed record OfficeCertificateChallengeResponse(string Challenge, DateTimeOffset ExpiresAt);
public sealed record OfficeCertificateRecoveryRequest(string Challenge, string SignatureBase64);

/// <summary>Domain-separated ECDSA P-256/SHA-256 proof, using IEEE P1363 signatures.</summary>
public static class OfficeCertificateRecoveryProof
{
    public static byte[] Payload(Guid officeId, string challenge) =>
        Encoding.UTF8.GetBytes($"csweet-office-certificate-recovery-v1\n{officeId:D}\n{challenge}");
}
