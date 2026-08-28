# identity/domain-and-persistence Specification

## Purpose
Defines the durable system-identity model and module-owned persistence guarantees required by later authentication and authorization capabilities.

## Requirements

### Requirement: System user identity is durable and lifecycle controlled
The system SHALL persist each system user with a stable identifier, normalized unique login, password hash, personal and contact details, lifecycle status, login tracking fields, audit metadata, and a concurrency version. The system MUST NOT persist a plaintext password or physically delete a user as part of normal lifecycle management.

#### Scenario: Persist an active system user
- **WHEN** a valid system user with a normalized login and password hash is saved
- **THEN** the user and all required lifecycle, audit, and concurrency data are durably stored

#### Scenario: Reject a duplicate normalized login
- **WHEN** a system user is saved with a normalized login already assigned to another user
- **THEN** persistence rejects the operation as a uniqueness conflict

#### Scenario: Retain an archived user
- **WHEN** a system user reaches the archived lifecycle state
- **THEN** the user remains stored and identifiable rather than being physically deleted

### Requirement: Roles and permissions are independent identity records
The system SHALL persist roles and permissions as separate records with unique stable codes. The system SHALL support many-to-many user-role and role-permission assignments and SHALL prevent duplicate assignments.

#### Scenario: Assign multiple roles to a user
- **WHEN** distinct roles are assigned to one system user
- **THEN** each assignment is stored independently with its assignment metadata

#### Scenario: Reject a duplicate user-role assignment
- **WHEN** the same role is assigned to the same system user more than once
- **THEN** persistence rejects the duplicate assignment

#### Scenario: Associate permissions with a role
- **WHEN** distinct permissions are associated with a role
- **THEN** each role-permission association is stored independently and duplicate associations are rejected

### Requirement: Identity persistence is module isolated
The system SHALL store all Identity-owned records in the `identity` SQL schema and SHALL expose their persistence only through the Identity module boundary. Referential constraints SHALL be limited to records owned by Identity.

#### Scenario: Create the Identity database schema
- **WHEN** the initial Identity migration is applied to an empty supported database
- **THEN** it creates the Identity schema, all five Identity tables, their keys, constraints, and required indexes

#### Scenario: Preserve module ownership
- **WHEN** another module needs to reference a system user identifier
- **THEN** it can retain the identifier without receiving direct access to Identity tables or persistence sets

### Requirement: Concurrent modifications are detected
The system SHALL use an explicit `Version bigint` optimistic concurrency token for mutable Identity records and SHALL reject stale writes with a controlled concurrency failure instead of silently overwriting a newer value.

#### Scenario: Reject a stale system-user update
- **WHEN** two operations load the same system user and the second operation saves after the first has already changed it
- **THEN** the second save fails as a concurrency conflict and does not overwrite the first change

### Requirement: Known persistence conflicts are controlled
The Identity module SHALL translate known PostgreSQL uniqueness violations and optimistic concurrency failures into controlled module errors without exposing provider exceptions as expected business outcomes. Unknown database failures MUST remain distinguishable from known conflicts.

#### Scenario: Report a duplicate normalized login
- **WHEN** PostgreSQL rejects a write against the normalized-login unique constraint
- **THEN** Identity reports a controlled duplicate-login conflict

#### Scenario: Preserve an unknown database failure
- **WHEN** persistence fails for a reason that is not a recognized Identity uniqueness or concurrency conflict
- **THEN** the failure is not mislabeled as a known domain conflict

### Requirement: Local deployment topologies are equivalent
The AppHost and root Docker Compose topology SHALL both provide the API with the same shared PostgreSQL database capability. Deployment configuration MUST NOT embed production secrets in source control.

#### Scenario: Run through AppHost
- **WHEN** the distributed application starts through AppHost
- **THEN** the API receives a healthy PostgreSQL database reference for Identity persistence

#### Scenario: Run through Docker Compose
- **WHEN** the root Docker Compose topology starts with deployment-provided credentials
- **THEN** PostgreSQL becomes healthy, retains data in a persistent volume, and the API receives the equivalent connection-string setting

### Requirement: Identity records support required lookups
The system SHALL enforce unique indexes for normalized user login, role code, and permission code. It SHALL provide indexes for user email and status, and unique composite indexes for user-role and role-permission assignments.

#### Scenario: Query users by lifecycle status
- **WHEN** Identity queries users by status
- **THEN** the database can use the configured status index

#### Scenario: Enforce unique authorization codes
- **WHEN** a role or permission is saved with a code already in use by the same record type
- **THEN** persistence rejects the operation as a uniqueness conflict
