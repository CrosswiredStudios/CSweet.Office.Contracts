namespace CSweet.Office.Contracts.ControlPlane;

public sealed record ClaimOfficeRequest(
    string EnrollmentToken,
    string Name,
    string MachineName,
    string OperatingSystem,
    string Architecture,
    string OfficeVersion,
    string ProtocolVersion,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    DateTimeOffset CertificateExpiresAt,
    string CertificateSigningRequestPem,
    int AllocatableCpuCount,
    int AllocatableMemoryMb,
    int AllocatableDiskMb,
    int MaximumConcurrentWorkloads,
    IReadOnlyList<RegisterOfficeProviderRequest> Providers,
    OfficeSecurityPostureReport? SecurityPosture = null,
    Guid? AssistedSetupSessionId = null);

public sealed record RedeemAssistedOfficeSetupRequest(
    string HandoffSecret,
    string MachineName,
    string OperatingSystem,
    string Architecture,
    string OfficeVersion);

public sealed record RedeemAssistedOfficeSetupResponse(
    bool Succeeded,
    string? ErrorCode,
    string Message,
    Guid? AssistedSetupSessionId,
    string? EnrollmentToken,
    string? ControlPlaneUrl,
    string? ControlPlaneCertificateSha256,
    int AllocatableCpuCount,
    int AllocatableMemoryMb,
    int AllocatableDiskMb,
    int MaximumConcurrentWorkloads,
    bool MixedUseHost,
    string ExistingInstallationAction = "none",
    string? SetupReceipt = null,
    OfficeInstallationLocation? Storage = null);

/// <summary>Installer-selected host storage, never an assignment-supplied path.</summary>
public sealed record OfficeInstallationLocation(string VolumeId, string InstallRoot, string DataRoot, int RequiredDiskMb = 0);

public sealed record AssistedOfficePreflightRequest(
    string HandoffSecret,
    string MachineName,
    string OperatingSystem,
    string Architecture,
    string OfficeVersion,
    string ExistingInstallationState,
    OfficeInstallationLocation? ExistingStorage = null);

public sealed record AssistedOfficePreflightResponse(
    bool Succeeded,
    string? ErrorCode,
    string Message,
    Guid? AssistedSetupSessionId,
    string ExistingInstallationAction,
    bool ProceedToRedemption,
    string? SetupReceipt = null,
    OfficeInstallationLocation? Storage = null);

public sealed record ReportAssistedOfficeSetupResultRequest(
    Guid AssistedSetupSessionId,
    string SetupReceipt,
    string ResultCode,
    string MachineName,
    string OperatingSystem,
    string Architecture);

public sealed record CompleteAssistedOfficeRemovalRequest(
    string HandoffSecret,
    string MachineName,
    string OperatingSystem,
    string Architecture);

public sealed record OfficeSecurityPostureReport(
    string Profile,
    bool MixedUseHost,
    bool DevelopmentAssignmentsAllowed,
    IReadOnlyList<string> EnabledControls,
    IReadOnlyList<string> MissingControls,
    DateTimeOffset EvaluatedAt);

public sealed record RegisterOfficeProviderRequest(
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
    bool SupportsToolchainBuildWorkloads,
    bool IsAvailable,
    string? UnavailableReason)
{
    public bool SupportsPublicInternet { get; init; }
    public bool SupportsPublicInternetIpv6 => SupportsPublicInternet && CertificationSuiteVersion == "csweet-hardware-vm-smoke-v15";
    public bool SupportsPersistentDisk { get; init; }
}

public sealed record ClaimOfficeResponse(
    bool Succeeded,
    string? ErrorCode,
    string Message,
    Guid? OfficeId,
    string? EnrollmentReceipt,
    string? AssignmentSigningKeyId = null,
    string? AssignmentVerificationPublicKeyBase64 = null);

public sealed record HeadquartersAssignmentTrustResponse(
    string AssignmentSigningKeyId,
    string AssignmentVerificationPublicKeyBase64);

public sealed record OfficeHeartbeatRequest(
    string EnrollmentReceipt,
    long SessionEpoch,
    int AllocatableCpuCount,
    int AllocatableMemoryMb,
    int AllocatableDiskMb,
    int MaximumConcurrentWorkloads,
    IReadOnlyList<RegisterOfficeProviderRequest> Providers,
    OfficeSecurityPostureReport? SecurityPosture = null,
    string? OfficeVersion = null);

public sealed record OfficeCertificateRequest(string EnrollmentReceipt);

public sealed record OfficeCertificateResponse(
    bool Succeeded,
    string? ErrorCode,
    string Message,
    string? CertificateBase64,
    string? CertificateThumbprint,
    DateTimeOffset? ExpiresAt);
