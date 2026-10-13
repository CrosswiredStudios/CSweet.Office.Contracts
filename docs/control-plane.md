# Control Plane

Two transports share one vocabulary: HTTPS JSON for enrollment/heartbeat/certificates, and gRPC (`office_control.proto`) for the long-lived gateway.

Source files: `ControlPlane/EnrollmentContracts.cs`, `ControlPlane/OfficeCertificateRecovery.cs`, `ControlPlane/office_control.proto` (`GrpcServices="Both"`), `ProtocolVersions.cs`.

## Protocol version

`ProtocolVersions.ControlPlaneV1 = "1.0"`. Sent as `protocol_version` on every `OfficeControlMessage`, `HeadquartersControlMessage`, and `ArtifactDownloadRequest`. Unknown protobuf fields must be preserved on round-trip (covered by `ContractTests.Generated_gateway_preserves_unknown_fields`).

## gRPC service `OfficeGateway` (`csweet.office.v1`)

```proto
service OfficeGateway {
  rpc Connect(stream OfficeControlMessage) returns (stream HeadquartersControlMessage);
  rpc OpenWorkloadTunnel(stream WorkloadTunnelFrame) returns (stream WorkloadTunnelFrame);
  rpc DownloadArtifact(ArtifactDownloadRequest) returns (stream ArtifactChunk);
}
```

| RPC | Shape | Purpose |
|---|---|---|
| `Connect` | bidi `OfficeControlMessage` ↔ `HeadquartersControlMessage` | Heartbeats up; assignments/fence/drain/hello down. `session_epoch` detects stale reconnects. |
| `OpenWorkloadTunnel` | bidi `WorkloadTunnelFrame` | Opaque per-assignment byte tunnel (`sequence`, `content`, `completed`). |
| `DownloadArtifact` | unary request → streamed `ArtifactChunk` | Chunked artifact fetch (`offset`, `content`, `completed`, `total_size`, `sha256`). |

## Gateway messages

`OfficeControlMessage`: `protocol_version=1`, `office_id=2`, `session_epoch=3`, oneof `heartbeat=10`, `lease_renewal=11`, `assignment_status=12`, `assignment_stopped=13`.

`HeadquartersControlMessage`: same envelope, oneof `assignment=10`, `fence=11`, `drain=12`, `hello=13`, `assignment_stop_receipt=14`.

| Message | Key fields |
|---|---|
| `GatewayHello` | `assignment_signing_key_id`, `assignment_verification_public_key` (raw bytes). First trust anchor after connect. |
| `OfficeHeartbeat` | `occurred_at_unix_seconds`, `allocatable_cpu_count`, `allocatable_memory_mb`, `allocatable_disk_mb`, `maximum_concurrent_workloads`, `providers[]`, `security_posture?`, `office_version` (added 0.7.0, optional; empty = older sender). |
| `OfficeSecurityPosture` | `profile`, `mixed_use_host`, `development_assignments_allowed`, `enabled_controls[]`, `missing_controls[]`, `evaluated_at_unix_seconds`. |
| `OfficeProviderInventory` | `provider_id`, `provider_version`, `broker_protocol_version`, `guest_image_digest`, `certification_suite_version`, `certification_evidence_digest`, `certified_at_unix_seconds`, `certification_expires_at_unix_seconds` (0 = none), `supports_builder_workloads`, `supports_runtime_workloads`, `supports_toolchain_build_workloads=13`, `is_available`, `unavailable_reason`. |
| `WorkloadAssignment` | `assignment_id`, `workload_id`, `fencing_epoch`, `provider_id`, `specification_json`, `specification_sha256`, `signature_key_id`, `signature` (bytes), `lease_expires_at_unix_seconds`, `artifact_read_token`, `issued_at_unix_seconds`, `authorization_version`. Verify signature via [Security](security.md) before executing. |
| `AssignmentLeaseRenewal` | `assignment_id`, `fencing_epoch`, `requested_expiry_unix_seconds`. |
| `AssignmentStatusUpdate` | `assignment_id`, `fencing_epoch`, `status`, `failure_code`, `sanitized_failure`, `provider_instance_id`, `log_excerpt=13`, `result_artifact_locator=14`, `result_artifact_digest=15`, `result_artifact_signature=16`, `result_artifact_format_version=17`, `result_artifact_operating_system=18`, `result_artifact_architecture=19`. Never put secrets in `log_excerpt`/`sanitized_failure`. |
| `FenceAssignment` | `assignment_id`, `fencing_epoch`, `reason`. Higher epoch supersedes; stop work for stale epochs. |
| `DrainOffice` | `drain`, `reason`. `drain=false` clears. |
| `WorkloadTunnelFrame` | `office_id`, `assignment_id`, `fencing_epoch`, `sequence`, `content`, `completed`, `session_epoch`. |
| `ArtifactDownloadRequest` | `protocol_version`, `office_id`, `session_epoch`, `assignment_id`, `fencing_epoch`, `artifact_digest`, `artifact_read_token`, `transfer_id`. |
| `ArtifactChunk` | `offset`, `content`, `completed`, `total_size`, `sha256`. Reassemble by `offset`; verify `sha256`. |

### Durable stop evidence (0.8.0)

`OfficeControlMessage.assignment_stopped = 13` contains `AssignmentStopped(assignment_id, fencing_epoch, provider_id, provider_instance_id, never_created)`. `never_created` requires an empty instance ID and means creation was durably prevented; otherwise a nonempty exact instance ID is required. The outer envelope supplies the authenticated Office/session binding. This is teardown evidence, never a lease renewal or execution grant. Headquarters may fence an active attempt whose terminal status was lost.

`HeadquartersControlMessage.assignment_stop_receipt = 14` contains `AssignmentStopReceipt(assignment_id, fencing_epoch, accepted)`. The receiver acknowledges only a persisted, matching report. A sender retains unacknowledged reports for replay and preserves local replay protection after acceptance. Duplicate and out-of-order delivery are expected; implementations retain attempt history across assignment fencing/reassignment. Older peers can preserve unknown protobuf fields but cannot supply this evidence. Source: `src/CSweet.Office.Contracts/ControlPlane/office_control.proto`; verification: `AssignmentStopContractTests`.

### Missing-stop reconciliation (0.10.0)

`HeadquartersControlMessage.reconcile_assignment_stop = 15` contains `ReconcileAssignmentStop(assignment_id, fencing_epoch, provider_id)`. It requests teardown evidence for a retired exact attempt under the authenticated Office/session envelope. The receiver rechecks durable local ownership and creation state; the hint never permits execution or invents proof of teardown. Delivery may be repeated, missed, or reordered. Source: `src/CSweet.Office.Contracts/ControlPlane/office_control.proto`; verification: `AssignmentStopContractTests.RecoveryHintPreservesExactAttemptAndSessionWithoutExecutionAuthority`.

## HTTPS JSON contracts (`EnrollmentContracts.cs`)

All records are `sealed record` (System.Text.Json round-trip tested). `DateTimeOffset` serializes as ISO-8601; GUIDs as `D` strings.

### Claim / heartbeat / certificate

- `ClaimOfficeRequest(EnrollmentToken, Name, MachineName, OperatingSystem, Architecture, OfficeVersion, ProtocolVersion, CertificateThumbprint, CertificateSerialNumber, CertificateExpiresAt, CertificateSigningRequestPem, AllocatableCpuCount, AllocatableMemoryMb, AllocatableDiskMb, MaximumConcurrentWorkloads, Providers[], SecurityPosture?, AssistedSetupSessionId?)`
- `ClaimOfficeResponse(Succeeded, ErrorCode?, Message, OfficeId?, EnrollmentReceipt?, AssignmentSigningKeyId?, AssignmentVerificationPublicKeyBase64?)`
- `HeadquartersAssignmentTrustResponse(AssignmentSigningKeyId, AssignmentVerificationPublicKeyBase64)` — same trust anchor as `GatewayHello` but over HTTPS.
- `OfficeHeartbeatRequest(EnrollmentReceipt, SessionEpoch, AllocatableCpuCount, AllocatableMemoryMb, AllocatableDiskMb, MaximumConcurrentWorkloads, Providers[], SecurityPosture?, OfficeVersion?)` — `OfficeVersion` optional (0.7.0); HQ refreshes version after in-place update.
- `OfficeCertificateRequest(EnrollmentReceipt)`
- `OfficeCertificateResponse(Succeeded, ErrorCode?, Message, CertificateBase64?, CertificateThumbprint?, ExpiresAt?)`

### Provider + posture

- `RegisterOfficeProviderRequest(ProviderId, ProviderVersion, BrokerProtocolVersion, GuestImageDigest, CertificationSuiteVersion, CertificationEvidenceDigest, CertifiedAt, CertificationExpiresAt?, SupportsBuilderWorkloads, SupportsRuntimeWorkloads, SupportsToolchainBuildWorkloads, IsAvailable, UnavailableReason?)`
- `OfficeSecurityPostureReport(Profile, MixedUseHost, DevelopmentAssignmentsAllowed, EnabledControls[], MissingControls[], EvaluatedAt)`

### Assisted setup (handoff-secret flow)

1. `AssistedOfficePreflightRequest(HandoffSecret, MachineName, OperatingSystem, Architecture, OfficeVersion, ExistingInstallationState)` → `AssistedOfficePreflightResponse(Succeeded, ErrorCode?, Message, AssistedSetupSessionId?, ExistingInstallationAction, ProceedToRedemption, SetupReceipt?)`
2. `RedeemAssistedOfficeSetupRequest(HandoffSecret, MachineName, OperatingSystem, Architecture, OfficeVersion)` → `RedeemAssistedOfficeSetupResponse(Succeeded, ErrorCode?, Message, AssistedSetupSessionId?, EnrollmentToken?, ControlPlaneUrl?, ControlPlaneCertificateSha256?, AllocatableCpuCount, AllocatableMemoryMb, AllocatableDiskMb, MaximumConcurrentWorkloads, MixedUseHost, ExistingInstallationAction="none", SetupReceipt?)`
3. `ReportAssistedOfficeSetupResultRequest(AssistedSetupSessionId, SetupReceipt, ResultCode, MachineName, OperatingSystem, Architecture)` — binds receipt + session; preserves `ExistingInstallationAction`/`SetupReceipt` (regression-tested).
4. `CompleteAssistedOfficeRemovalRequest(HandoffSecret, MachineName, OperatingSystem, Architecture)`

## Certificate recovery (`OfficeCertificateRecovery.cs`)

Already-approved Office with expired cert proves possession of the original private key:

- `OfficeCertificateChallengeResponse(Challenge, ExpiresAt)`
- `OfficeCertificateRecoveryRequest(Challenge, SignatureBase64)` — `SignatureBase64` is IEEE P1363 ECDSA P-256/SHA-256 over `OfficeCertificateRecoveryProof.Payload`.
- `OfficeCertificateRecoveryProof.Payload(officeId, challenge)` = UTF-8 `"csweet-office-certificate-recovery-v1\n{officeId:D}\n{challenge}"`.

HQ owns one-use challenge enforcement and authorization; helpers grant no trust. See [Security](security.md).

## Runtime public internet (0.10.0)

`RuntimeWorkloadSpecification.NetworkPolicy` optionally carries `RuntimeNetworkPolicy` (`public-internet`, grant revision, blocked CIDRs); omission means restricted execution. `EnvironmentProfile` identifies the requested development profile. Both fields are bound by the original signed specification digest; authorization envelope encoding remains unchanged.

Guest protocol 1.1 adds envelope field 22 (`EgressFrame`) and boot field 16 (`public_internet`). Egress uses flow-scoped open/opened/data/half-close/close/error operations for TCP and connected UDP. Office inventory field 14 (`supports_public_internet`) and the corresponding enrollment DTO property default false. Neither a manifest declaration nor inventory alone grants internet authority. Guest protocol 1.0 and all frozen signing vectors remain supported.
