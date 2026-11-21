using System.Globalization;
using System.Text;

namespace CSweet.Office.Contracts.Security;

public static class OfficeComputeReceiptEncoding
{
    public static byte[] Payload(Guid officeId, Guid operationId, long sequence, string digest)
        => Encode("CSweet.Office.ComputeReceipt.v1", officeId, operationId, sequence, digest);

    public static byte[] MaintenancePayload(Guid officeId, Guid eventId, string digest)
        => Encode("CSweet.Office.ComputeMaintenanceReceipt.v1", officeId, eventId, 1, digest);

    private static byte[] Encode(string domain, Guid officeId, Guid operationId, long sequence, string digest)
    {
        if (officeId == Guid.Empty || operationId == Guid.Empty || sequence < 1 ||
            digest is not { Length: 71 } || !digest.StartsWith("sha256:", StringComparison.Ordinal) ||
            digest.AsSpan(7).ContainsAnyExcept("0123456789abcdef"))
            throw new InvalidDataException("Invalid Office compute receipt.");
        return Encoding.ASCII.GetBytes($"{domain}\n{officeId:D}\n{operationId:D}\n{sequence.ToString(CultureInfo.InvariantCulture)}\n{digest}");
    }
}
