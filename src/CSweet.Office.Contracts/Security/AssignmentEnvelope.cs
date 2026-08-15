using System.Security.Cryptography;
using System.Text;
using System.Buffers.Binary;

namespace CSweet.Office.Contracts.Security;

public static class AssignmentEnvelope
{
    public const int CurrentAuthorizationVersion = 1;
    private static readonly byte[] Purpose = "CSweet.Office.WorkloadAuthorization"u8.ToArray();

    public static string Digest(string specificationJson) =>
        "sha256:" + Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(specificationJson))).ToLowerInvariant();

    public static byte[] Payload(
        Guid officeId,
        Guid assignmentId,
        Guid workloadId,
        long fencingEpoch,
        string providerId,
        string specificationSha256,
        DateTimeOffset issuedAt,
        DateTimeOffset leaseExpiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(specificationSha256);
        if (!specificationSha256.StartsWith("sha256:", StringComparison.Ordinal) || specificationSha256.Length != 71)
            throw new ArgumentException("The workload specification digest must be a canonical SHA-256 digest.", nameof(specificationSha256));
        if (leaseExpiresAt <= issuedAt)
            throw new ArgumentOutOfRangeException(nameof(leaseExpiresAt), "The authorization must expire after it is issued.");

        using var stream = new MemoryStream(256);
        WriteBytes(stream, Purpose);
        stream.WriteByte(CurrentAuthorizationVersion);
        WriteGuid(stream, officeId);
        WriteGuid(stream, assignmentId);
        WriteGuid(stream, workloadId);
        WriteInt64(stream, fencingEpoch);
        WriteString(stream, providerId);
        WriteString(stream, specificationSha256);
        WriteInt64(stream, issuedAt.ToUnixTimeSeconds());
        WriteInt64(stream, leaseExpiresAt.ToUnixTimeSeconds());
        return stream.ToArray();
    }

    private static void WriteGuid(Stream stream, Guid value)
    {
        Span<byte> buffer = stackalloc byte[16];
        if (!value.TryWriteBytes(buffer, bigEndian: true, out var written) || written != buffer.Length)
            throw new InvalidOperationException("The authorization identifier could not be encoded.");
        stream.Write(buffer);
    }

    private static void WriteString(Stream stream, string value) => WriteBytes(stream, Encoding.UTF8.GetBytes(value));

    private static void WriteBytes(Stream stream, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, value.Length);
        stream.Write(length);
        stream.Write(value);
    }

    private static void WriteInt64(Stream stream, long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }
}
