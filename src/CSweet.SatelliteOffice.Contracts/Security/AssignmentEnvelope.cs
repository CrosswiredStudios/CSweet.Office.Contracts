using System.Security.Cryptography;
using System.Text;

namespace CSweet.SatelliteOffice.Contracts.Security;

public static class AssignmentEnvelope
{
    public static string Digest(string specificationJson) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(specificationJson))).ToLowerInvariant();

    public static byte[] Payload(
        Guid satelliteOfficeId,
        Guid assignmentId,
        long fencingEpoch,
        string specificationSha256,
        DateTimeOffset leaseExpiresAt,
        string artifactReadToken) => Encoding.UTF8.GetBytes(string.Join('\n',
            satelliteOfficeId.ToString("D"),
            assignmentId.ToString("D"),
            fencingEpoch.ToString(System.Globalization.CultureInfo.InvariantCulture),
            specificationSha256,
            leaseExpiresAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
            artifactReadToken));
}
