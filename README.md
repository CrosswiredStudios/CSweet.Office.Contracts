# CSweet.Office.Contracts

## Version 0.15.0

Adds the public Office linking request, matching code and encrypted enrollment grant. Existing claim and assisted installation messages remain compatible.

Dependency-light wire contracts shared by C-Sweet Headquarters and C-Sweet Office.

The package owns only cross-process protocol concerns: headquarters enrollment and control-plane gRPC messages, guest broker framing and authentication, portable workload specifications, and canonical assignment signatures. Privileged provider interfaces and RuntimeHost local RPC remain private to the Office repository.

## Version 0.14.1

The Office control stream carries an optional compute capacity offer, a signed compute dispatch, a signed provider result and its receipt.

## Version 0.13.0

Adds guest protocol 1.2. Proxy request and response bodies can be streamed in bounded, credited
`ProxyBodyFrame` messages, so a transfer is no longer limited to one envelope; inline bodies and
protocol 1.0/1.1 peers are unchanged. `RuntimeWorkloadSpecification.PersistentDisk` optionally
authorizes one Office-kept disk by opaque identifier and generation, and providers report
`supports_persistent_disk`. Omission preserves existing behaviour; signed assignment bytes are unchanged.

## Version 0.12.0

Adds optional `OfficeInstallationLocation` storage to assisted preflight/redemption responses and
an optional existing-location report to preflight requests. Volume identity, resolved roots, and
installation free-space requirements travel through the authenticated setup flow. Missing storage
preserves legacy wire compatibility; no assignment bytes or workload authorization change.

## Version 0.11.0

Adds machine-bound automatic-update preparation contracts in `ControlPlane/OfficeUpdatePreparation.cs`. A short-lived handoff is exchanged once for a bounded preparation receipt; verified preparation and confirmed workload teardown precede a fresh installation handoff. These receipts grant no workload execution authority. Existing enrollment and signed assignment formats remain unchanged.

## Version 0.10.0

Adds the teardown-only `ReconcileAssignmentStop` hint for the exact Office-bound assignment epoch and provider. It supports bounded heartbeat/reconnect discovery of missing stop evidence, including cancelled or superseded attempts. It grants no execution authority and leaves signed assignment bytes unchanged. Coordinated Headquarters and Office deployment is required.

## Version 0.8.0

Adds `AssignmentStopped` reports and `AssignmentStopReceipt` acknowledgements. Reports identify the exact Office-bound assignment epoch and provider instance, or explicitly attest that creation was durably prevented. These messages grant no execution authority. The signed assignment format and protocol version remain unchanged. Coordinated Headquarters and Office implementation is required for confirmed shutdown; older peers do not provide stop evidence.

## Packages

`CSweet.Office.Contracts` targets .NET 10 and is released with SourceLink and symbol packages. Releases follow semantic versioning. Breaking wire or public API changes require a major version; additive compatible changes require a minor version; compatible fixes require a patch version.

## Local development

```powershell
dotnet test CSweet.Office.Contracts.slnx -c Release
dotnet pack src/CSweet.Office.Contracts/CSweet.Office.Contracts.csproj -c Release -o artifacts/packages
```

Consumers automatically detect a sibling checkout. This package also detects a sibling CSweet.Isolation checkout; explicit false UseLocal properties retain package-only checks. Release validation must also run with that property disabled against the exact package version.

## Version 0.6.0

Adds Office certificate recovery challenge/response contracts and a domain-separated ECDSA P-256/SHA-256 proof payload. The additive wire protocol lets an already-approved Office recover an expired certificate using its original private key. Headquarters owns one-use challenge enforcement and authorization; the wire helpers grant no trust by themselves.

## Version 0.6.1

Compatible maintenance: reuse the neutral isolation signature encoder with the original Office byte format, detect sibling isolation source automatically, and preserve the 0.6.0 certificate-recovery contracts. Office and Headquarters pins must remain synchronized.

## Version 0.7.0

Adds the optional OfficeVersion heartbeat field to HTTP and gRPC control messages. Headquarters can refresh the reported runtime version after an in-place update. Older senders remain compatible.

## Version 0.7.1

Compatible maintenance: NuGet packaging script plus CI/publish alignment with `CSweet.Agent.Sdk` (package-only validation, version-bump publish). No wire change.

## Runtime public internet (0.10.0)

`RuntimeWorkloadSpecification.NetworkPolicy` optionally carries `RuntimeNetworkPolicy` (`public-internet`, grant revision, blocked CIDRs); omission means restricted execution. `EnvironmentProfile` identifies the requested development profile. Both fields are bound by the original signed specification digest; authorization envelope encoding remains unchanged.

Guest protocol 1.1 adds envelope field 22 (`EgressFrame`) and boot field 16 (`public_internet`). Egress uses flow-scoped open/opened/data/half-close/close/error operations for TCP and connected UDP. Office inventory field 14 (`supports_public_internet`) and the corresponding enrollment DTO property default false. Neither a manifest declaration nor inventory alone grants internet authority. Guest protocol 1.0 and all frozen signing vectors remain supported.
