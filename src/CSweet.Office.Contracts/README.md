# C-Sweet Office Contracts

Dependency-light wire contracts shared by C-Sweet Headquarters and C-Sweet Office.

This package contains only cross-process messages, workload specifications, protocol constants,
framing, and signature canonicalization. Platform persistence, scheduling, provider interfaces,
RuntimeHost RPC, and virtualization implementations do not belong here.

## Version 0.12.0

Adds optional installer storage locations to assisted preflight/redemption and optional existing
storage reporting for reassignment. Installers verify mounted volume identity, paths, and required
free space locally. Legacy messages may omit storage; workload authorization is unchanged.
