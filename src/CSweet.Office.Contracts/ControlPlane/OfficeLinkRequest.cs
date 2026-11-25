using System.Security.Cryptography;
using System.Text;

namespace CSweet.Office.Contracts.ControlPlane;

/// <summary>The public portion of a short-lived desktop linking request. Only its creator has the decryption key.</summary>
public sealed record OfficeLinkRequest(string Machine, string Key, long Until)
{
    public string Id => Convert.ToHexStringLower(SHA256.HashData(
        Encoding.UTF8.GetBytes(FormattableString.Invariant($"csweet-office-link-v1\n{Machine}\n{Key}\n{Until}"))));
    public string MatchingCode => Id[..4].ToUpperInvariant() + "-" + Id[4..8].ToUpperInvariant();

    public bool IsValid(DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(Machine) || Machine.Length > 80 ||
            Machine.Any(ch => char.IsControl(ch) || char.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.Format) ||
            Key is not { Length: >= 380 and <= 420 } || Until <= now.ToUnixTimeSeconds() ||
            Until > now.AddMinutes(16).ToUnixTimeSeconds()) return false;
        try
        {
            using var rsa = RSA.Create();
            var bytes = Convert.FromBase64String(Key);
            rsa.ImportSubjectPublicKeyInfo(bytes, out var read);
            return rsa.KeySize == 2048 && read == bytes.Length &&
                Key == Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        }
        catch (Exception e) when (e is CryptographicException or FormatException or ArgumentException) { return false; }
    }

    public string SealToken(string token)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(Key), out _);
        return Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(token), RSAEncryptionPadding.OaepSHA256));
    }

    public string Query => FormattableString.Invariant($"machine={Uri.EscapeDataString(Machine)}&key={Uri.EscapeDataString(Key)}&until={Until}");
}

public sealed record OfficeLinkGrant(string Gateway, string? CertificateSha256, string SealedToken, Guid EnrollmentId);

public sealed record OfficeLinkClaimProof(string PublicKey, long Until, string Signature)
{
    private static byte[] Payload(ClaimOfficeRequest claim) => Encoding.UTF8.GetBytes(FormattableString.Invariant(
        $"csweet-office-link-claim-v1\n{claim.EnrollmentToken}\n{claim.MachineName}\n{claim.CertificateThumbprint}\n{claim.CertificateSerialNumber}\n{claim.AllocatableCpuCount}\n{claim.AllocatableMemoryMb}\n{claim.AllocatableDiskMb}\n{claim.MaximumConcurrentWorkloads}\n{claim.CertificateSigningRequestPem}"));

    public static OfficeLinkClaimProof Sign(ClaimOfficeRequest claim, long until, RSA key) => new(
        Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()), until,
        Convert.ToBase64String(key.SignData(Payload(claim), HashAlgorithmName.SHA256, RSASignaturePadding.Pss)));

    public bool Verifies(ClaimOfficeRequest claim, string expectedLinkId, DateTimeOffset now)
    {
        var link = new OfficeLinkRequest(claim.MachineName, PublicKey, Until);
        if (!link.IsValid(now) || link.Id != expectedLinkId || Signature is not { Length: 344 }) return false;
        try
        {
            using var key = RSA.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKey), out _);
            return key.VerifyData(Payload(claim), Convert.FromBase64String(Signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
        catch (Exception e) when (e is FormatException or CryptographicException) { return false; }
    }
}
