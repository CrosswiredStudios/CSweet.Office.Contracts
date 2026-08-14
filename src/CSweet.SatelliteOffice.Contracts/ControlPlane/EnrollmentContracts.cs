namespace CSweet.SatelliteOffice.Contracts.ControlPlane;

public sealed record ClaimSatelliteOfficeRequest(
    string EnrollmentToken,
    string Name,
    string MachineName,
    string OperatingSystem,
    string Architecture,
    string SatelliteOfficeVersion,
    string ProtocolVersion,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    DateTimeOffset CertificateExpiresAt,
    string CertificateSigningRequestPem,
    int AllocatableCpuCount,
    int AllocatableMemoryMb,
    int AllocatableDiskMb,
    int MaximumConcurrentWorkloads,
    IReadOnlyList<RegisterSatelliteOfficeProviderRequest> Providers,
    SatelliteOfficeSecurityPostureReport? SecurityPosture = null);

public sealed record SatelliteOfficeSecurityPostureReport(
    string Profile,
    bool MixedUseHost,
    bool DevelopmentAssignmentsAllowed,
    IReadOnlyList<string> EnabledControls,
    IReadOnlyList<string> MissingControls,
    DateTimeOffset EvaluatedAt);

public sealed record RegisterSatelliteOfficeProviderRequest(
    string ProviderId,
    string ProviderVersion,
    string BrokerProtocolVersion,
    string GuestImageDigest,
    string CertificationSuiteVersion,
    string CertificationEvidenceDigest,
    DateTimeOffset CertifiedAt,
    DateTimeOffset? CertificationExpiresAt,
    bool SupportsBuilderWorkloads,
    bool SupportsRuntimeWorkloads,
    bool IsAvailable,
    string? UnavailableReason);

public sealed record ClaimSatelliteOfficeResponse(
    bool Succeeded,
    string? ErrorCode,
    string Message,
    Guid? SatelliteOfficeId,
    string? EnrollmentReceipt,
    string? AssignmentSigningKeyId = null,
    string? AssignmentVerificationPublicKeyBase64 = null);

public sealed record HeadquartersAssignmentTrustResponse(
    string AssignmentSigningKeyId,
    string AssignmentVerificationPublicKeyBase64);

public sealed record SatelliteOfficeHeartbeatRequest(
    string EnrollmentReceipt,
    long SessionEpoch,
    int AllocatableCpuCount,
    int AllocatableMemoryMb,
    int AllocatableDiskMb,
    int MaximumConcurrentWorkloads,
    IReadOnlyList<RegisterSatelliteOfficeProviderRequest> Providers,
    SatelliteOfficeSecurityPostureReport? SecurityPosture = null);

public sealed record SatelliteOfficeCertificateRequest(string EnrollmentReceipt);

public sealed record SatelliteOfficeCertificateResponse(
    bool Succeeded,
    string? ErrorCode,
    string Message,
    string? CertificateBase64,
    string? CertificateThumbprint,
    DateTimeOffset? ExpiresAt);
