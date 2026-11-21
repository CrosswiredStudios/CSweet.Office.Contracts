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
    ToolchainBuild,
    ComputeEnvironment
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

// The signed compute packet supplies operation and tenant authority. Only the
// offline executor constructs this input to the Office platform helper.
public sealed record ComputeEnvironmentWorkloadSpecification(Guid WorkloadId, GuestImageReference GuestImage,
    WorkloadResourceLimits ResourceLimits, BrokerChannelLease BrokerLease, Guid OrganizationId, Guid InstallationId)
    : WorkloadSpecification(WorkloadId, WorkloadKind.ComputeEnvironment, GuestImage, ResourceLimits, BrokerLease);

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
    : WorkloadSpecification(WorkloadId, WorkloadKind.Runtime, GuestImage, ResourceLimits, BrokerLease)
{
    public RuntimeNetworkPolicy? NetworkPolicy { get; init; }
    public string? EnvironmentProfile { get; init; }
    public RuntimePersistentDisk? PersistentDisk { get; init; }
}

public sealed record RuntimeNetworkPolicy(string Mode, long GrantRevision,
    IReadOnlyList<string>? BlockedCidrs = null)
{
    public const string PublicInternet = "public-internet";
    public const string PublicInternetIpv4 = "public-internet-ipv4";
    public void Validate()
    {
        if (Mode is not (PublicInternet or PublicInternetIpv4) || GrantRevision < 1 || BlockedCidrs?.Count > 256)
            throw new InvalidDataException("The runtime network policy is invalid.");
        if (BlockedCidrs?.Any(cidr => !System.Net.IPNetwork.TryParse(cidr, out _)) == true)
            throw new InvalidDataException("The runtime blocked CIDR policy is invalid.");
    }
}

/// <summary>
/// Authorizes one Office-kept disk that outlives the runtime's virtual machine. Omission means no
/// state persists. The Office derives the disk's location from <see cref="DiskId"/>; no path is carried.
/// A <see cref="Generation"/> above the one the Office holds for the disk requires a blank replacement.
/// </summary>
public sealed record RuntimePersistentDisk(Guid DiskId, long Generation, int MaximumMegabytes)
{
    public void Validate()
    {
        if (DiskId == Guid.Empty || Generation < 1 || MaximumMegabytes is < 64 or > 1_048_576)
            throw new InvalidDataException("The runtime persistent disk is invalid.");
    }
}

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
