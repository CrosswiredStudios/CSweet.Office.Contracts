# Workloads

Portable, serializable workload specifications HQ signs and the Office executes. No behavior here — just data + `Validate()` bounds.

Source file: `Workloads/WorkloadModels.cs`.

## Kinds

`WorkloadKind`: `Builder` (build an agent artifact from source), `Runtime` (run an agent artifact), `ToolchainBuild` (run an immutable toolchain adapter against an exact source revision; repo/registry credentials never enter the guest).

`AgentTrustLevel`: `BuiltIn`, `PublisherTrusted`, `OrganizationApproved`, `UntrustedRepository`, `UntrustedMarketplace` (advisory classification; enforcement lives outside this package).

## Base record

`WorkloadSpecification(WorkloadId, Kind, GuestImage, ResourceLimits, BrokerLease)` — abstract; `Kind` discriminates. Serialized as `specification_json` inside `WorkloadAssignment` with `specification_sha256` = `AssignmentEnvelope.Digest(json)` (see [Security](security.md)).

## Concrete specifications

- `BuilderWorkloadSpecification(..., Repository, MaximumArtifactBytes)` where `Repository: RepositoryDescriptor(RepositoryUrl, CommitSha, IncludeSubmodules, BuildProfileId, BuildProfileId→BuildProfileVersion)`.
- `RuntimeWorkloadSpecification(..., Artifact, Identity, Entrypoint[])` where `Artifact: AgentArtifactReference(Digest, Signature, FormatVersion, OperatingSystem, Architecture)` and `Identity: RuntimeAgentIdentity(InstallationId, BusinessId, TickId, AgentDisplayName?, AgentRoleName?)`.
- `ToolchainBuildWorkloadSpecification(..., AdapterArtifact, Identity, SourceRepository, DeliveryBuildId, ExpectedBuildRevision, RecipeKey, TargetKey, ConfigurationJson, Entrypoint[], AllowedDependencyRegistryHosts[], MaximumSourceBytes, MaximumOutputBytes)` — binds exact adapter + source revision + delivery build; dependency egress limited to `AllowedDependencyRegistryHosts`.

## Shared descriptors

- `GuestImageReference(ImageId, Version, Digest, OperatingSystem, Architecture)` — expected digest must match `GuestHello.guest_image_digest` during the guest handshake.
- `BrokerChannelLease(ChannelId, ProtocolVersion, BootToken, ExpectedGuestImageDigest, ExpectedArtifactDigest?, ExpiresAt)` — feeds `ExpectedGuestIdentity` + `GuestBootConfiguration`; boot token ≥ 16 UTF-8 bytes.
- `WorkloadResourceLimits(VirtualCpuCount, CpuPercent, MemoryMegabytes, WritableDiskMegabytes, MaximumProcessCount, MaximumLogBytes, MaximumDuration)` — `Validate()` bounds: vCPU 1–64, CPU% 1–6400, memory 128–1,048,576 MB, disk 64–1,048,576 MB, processes 1–1,000,000, log 1–1,073,741,824 B, duration (0, 7 days].

## JSON notes

Records round-trip via System.Text.Json (tested for runtime + toolchain). `TimeSpan` uses the default converter; `Guid` as `D` strings. Keep property names stable — `specification_sha256` covers the exact bytes.
