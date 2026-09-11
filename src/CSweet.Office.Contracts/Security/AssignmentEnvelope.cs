using CSweet.Isolation.Security;

namespace CSweet.Office.Contracts.Security;

/// <summary>Office v1 wire adapter. The purpose and byte layout remain unchanged.</summary>
public static class AssignmentEnvelope
{
    public const int CurrentAuthorizationVersion = 1;
    public static string Digest(string specificationJson) => WorkloadAuthorizationEnvelope.Digest(specificationJson);
    public static byte[] Payload(Guid officeId, Guid assignmentId, Guid workloadId, long fencingEpoch,
        string providerId, string specificationSha256, DateTimeOffset issuedAt, DateTimeOffset leaseExpiresAt) =>
        WorkloadAuthorizationEnvelope.Encode("CSweet.Office.WorkloadAuthorization", CurrentAuthorizationVersion,
            officeId, assignmentId, workloadId, fencingEpoch, providerId, specificationSha256, issuedAt, leaseExpiresAt);
}
