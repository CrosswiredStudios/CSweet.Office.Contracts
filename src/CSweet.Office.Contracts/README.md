# C-Sweet Office Contracts

Dependency-light wire contracts shared by C-Sweet Headquarters and C-Sweet Office.

This package contains only cross-process messages, workload specifications, protocol constants,
framing, and signature canonicalization. Platform persistence, scheduling, provider interfaces,
RuntimeHost RPC, and virtualization implementations do not belong here.

## Version 0.13.0

Adds guest protocol 1.2. Proxy request and response bodies can be streamed in bounded, credited
`ProxyBodyFrame` messages, so a transfer is no longer limited to one envelope; inline bodies and
protocol 1.0/1.1 peers are unchanged. `RuntimeWorkloadSpecification.PersistentDisk` optionally
authorizes one Office-kept disk by opaque identifier and generation, and providers report
`supports_persistent_disk`. Omission preserves existing behaviour; signed assignment bytes are unchanged.

## Version 0.12.0

Adds optional installer storage locations to assisted preflight/redemption and optional existing
storage reporting for reassignment. Installers verify mounted volume identity, paths, and required
free space locally. Legacy messages may omit storage; workload authorization is unchanged.
