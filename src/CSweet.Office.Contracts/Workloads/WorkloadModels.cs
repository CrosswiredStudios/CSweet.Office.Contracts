namespace CSweet.Office.Contracts.Workloads;

public enum AgentTrustLevel
{
    BuiltIn,
    PublisherTrusted,
    OrganizationApproved,
    UntrustedRepository,
    UntrustedMarketplace
}

public enum WorkloadKind
{
    Builder,
    Runtime,
    ToolchainBuild
}

public sealed record WorkloadResourceLimits(
    int VirtualCpuCount,
    int CpuPercent,
    int MemoryMegabytes,
    int WritableDiskMegabytes,
    int MaximumProcessCount,
    int MaximumLogBytes,
    TimeSpan MaximumDuration)
{
    public void Validate()
    {
        if (VirtualCpuCount is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(VirtualCpuCount));
        if (CpuPercent is < 1 or > 6400) throw new ArgumentOutOfRangeException(nameof(CpuPercent));
        if (MemoryMegabytes is < 128 or > 1_048_576) throw new ArgumentOutOfRangeException(nameof(MemoryMegabytes));
        if (WritableDiskMegabytes is < 64 or > 1_048_576) throw new ArgumentOutOfRangeException(nameof(WritableDiskMegabytes));
        if (MaximumProcessCount is < 1 or > 1_000_000) throw new ArgumentOutOfRangeException(nameof(MaximumProcessCount));
        if (MaximumLogBytes is < 1 or > 1_073_741_824) throw new ArgumentOutOfRangeException(nameof(MaximumLogBytes));
        if (MaximumDuration <= TimeSpan.Zero || MaximumDuration > TimeSpan.FromDays(7))
            throw new ArgumentOutOfRangeException(nameof(MaximumDuration));
    }
}

public sealed record GuestImageReference(
    string ImageId,
    string Version,
    string Digest,
    string OperatingSystem,
    string Architecture);

public sealed record AgentArtifactReference(
    string Digest,
    string Signature,
    string FormatVersion,
    string OperatingSystem,
    string Architecture);

public sealed record BrokerChannelLease(
    Guid ChannelId,
    string ProtocolVersion,
    string BootToken,
    string ExpectedGuestImageDigest,
    string? ExpectedArtifactDigest,
    DateTimeOffset ExpiresAt);

public sealed record RepositoryDescriptor(
    string RepositoryUrl,
    string CommitSha,
    bool IncludeSubmodules,
    string BuildProfileId,
    string BuildProfileVersion);

public sealed record RuntimeAgentIdentity(
    Guid InstallationId,
    string BusinessId,
    Guid TickId,
    string? AgentDisplayName = null,
    string? AgentRoleName = null);

public abstract record WorkloadSpecification(
    Guid WorkloadId,
    WorkloadKind Kind,
    GuestImageReference GuestImage,
    WorkloadResourceLimits ResourceLimits,
    BrokerChannelLease BrokerLease);

public sealed record BuilderWorkloadSpecification(
    Guid WorkloadId,
    GuestImageReference GuestImage,
    WorkloadResourceLimits ResourceLimits,
    BrokerChannelLease BrokerLease,
    RepositoryDescriptor Repository,
    long MaximumArtifactBytes)
    : WorkloadSpecification(WorkloadId, WorkloadKind.Builder, GuestImage, ResourceLimits, BrokerLease);

public sealed record RuntimeWorkloadSpecification(
    Guid WorkloadId,
    GuestImageReference GuestImage,
    WorkloadResourceLimits ResourceLimits,
    BrokerChannelLease BrokerLease,
    AgentArtifactReference Artifact,
    RuntimeAgentIdentity Identity,
    IReadOnlyList<string> Entrypoint)
    : WorkloadSpecification(WorkloadId, WorkloadKind.Runtime, GuestImage, ResourceLimits, BrokerLease);

/// <summary>
/// Executes an immutable toolchain adapter against an exact source revision in a disposable Office VM.
/// Source retrieval and dependency restore are brokered; repository and registry credentials never enter the guest.
/// </summary>
public sealed record ToolchainBuildWorkloadSpecification(
    Guid WorkloadId,
    GuestImageReference GuestImage,
    WorkloadResourceLimits ResourceLimits,
    BrokerChannelLease BrokerLease,
    AgentArtifactReference AdapterArtifact,
    RuntimeAgentIdentity Identity,
    RepositoryDescriptor SourceRepository,
    Guid DeliveryBuildId,
    long ExpectedBuildRevision,
    string RecipeKey,
    string TargetKey,
    string ConfigurationJson,
    IReadOnlyList<string> Entrypoint,
    IReadOnlyList<string> AllowedDependencyRegistryHosts,
    long MaximumSourceBytes,
    long MaximumOutputBytes)
    : WorkloadSpecification(WorkloadId, WorkloadKind.ToolchainBuild, GuestImage, ResourceLimits, BrokerLease);
