# Workloads

Portable, serializable workload specifications HQ signs and the Office executes. No behavior here — just data + `Validate()` bounds.

Source file: `Workloads/WorkloadModels.cs`.

## Kinds

`WorkloadKind`: `Builder` (build an agent artifact from source), `Runtime` (run an agent artifact), `ToolchainBuild` (run an immutable toolchain adapter against an exact source revision; repo/registry credentials never enter the guest).

`AgentTrustLevel`: `BuiltIn`, `PublisherTrusted`, `OrganizationApproved`, `UntrustedRepository`, `UntrustedMarketplace` (advisory classification; enforcement lives outside this package).

## Base record

`ComputeEnvironmentWorkloadSpecification` binds a Linux environment to its business and installation in the native Hyper-V helper. Its authority comes from the signed compute dispatch on the Office control stream. Generic execution assignments continue to accept the existing builder, runtime and toolchain kinds.

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

## Runtime public internet (0.10.0)

`RuntimeWorkloadSpecification.NetworkPolicy` optionally carries `RuntimeNetworkPolicy` (`public-internet`, grant revision, blocked CIDRs); omission means restricted execution. `EnvironmentProfile` identifies the requested development profile. Both fields are bound by the original signed specification digest; authorization envelope encoding remains unchanged.

Guest protocol 1.1 adds envelope field 22 (`EgressFrame`) and boot field 16 (`public_internet`). Egress uses flow-scoped open/opened/data/half-close/close/error operations for TCP and connected UDP. Office inventory field 14 (`supports_public_internet`) and the corresponding enrollment DTO property default false. Neither a manifest declaration nor inventory alone grants internet authority. Guest protocol 1.0 and all frozen signing vectors remain supported.

## Streamed proxy bodies and persistent workload disk (0.13.0)

Guest protocol 1.2 (`ProtocolVersions.GuestBrokerV12`) adds envelope field 23 (`ProxyBodyFrame`), `body_streamed` on `ProxyRequest` (field 7) and `ProxyResponse` (field 6), and boot field 17 (`persistent_disk`). A streamed request or response carries an empty inline `body`; its content follows as `ProxyBodyFrame` messages for the same `request_id`: `DATA` frames of at most `ProxyBodyStreaming.MaximumDataBytes` with a zero-based contiguous `sequence` per direction, then `END` whose `sequence` equals the number of `DATA` frames sent. Each direction starts with `ProxyBodyStreaming.InitialCredits`; the receiver returns one `CREDIT`, naming the direction it credits, per consumed `DATA` frame. `ABORT` ends the request with an `error_code`. `ProxyBodyStreaming.Validate` rejects frames that are malformed on their own; sequence and credit accounting are receiver state. Inline bodies are unchanged, so protocol 1.0 and 1.1 peers interoperate as before, and a sender streams only to a peer that negotiated 1.2.

`RuntimeWorkloadSpecification.PersistentDisk` optionally carries `RuntimePersistentDisk(DiskId, Generation, MaximumMegabytes)`; omission means no state outlives the virtual machine. The Office derives the disk location from `DiskId`; no path, device or mount option is carried. A `Generation` above the one the Office holds for the disk requires a blank replacement, and a lower one is stale and refused. The field is bound by the signed specification digest; authorization envelope encoding is unchanged. `OfficeProviderInventory` field 15 (`supports_persistent_disk`) and `RegisterOfficeProviderRequest.SupportsPersistentDisk` default false, and Headquarters sets `PersistentDisk` only for a provider that reports support. All frozen signing vectors remain valid.
