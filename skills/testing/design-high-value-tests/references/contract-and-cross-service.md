# Contract and Cross-Service Testing

Use this reference when independently released services, provider policies, or a deployed journey create risks that local application tests cannot establish.

## Assign Policy Ownership

Identify which system owns each rule. Test app-owned decisions locally. For provider-owned rules such as token validity, password policy, delivery acceptance, or rate limits, test the consumer's mapping and compatibility; do not clone the provider's policy into local unit tests. If the client must present provider-owned guidance, consume provider metadata or a shared versioned contract where possible, test that integration for drift, and still treat provider rejection as authoritative.

## Separate Three Kinds of Evidence

- **Local adapter evidence:** Run local translation and capture the final request or command. This proves the application builds the intended artifact, not that the provider still accepts it.
- **Contract evidence:** Verify schema and semantic examples against provider-owned specifications, executable contracts, compatible provider builds, or a faithful sandbox. Cover authentication, versioning, success, and contract-significant failure meanings.
- **Deployed journey evidence:** Exercise only risks that emerge after assembly, configuration, routing, identity, and delivery. Keep edge-case matrices below this level.

Treat a hand-written stub as a test tool, not compatibility proof. Gate provider and consumer releases with contract verification when they can deploy independently. Report which provider version or artifact was exercised.

## Keep UI Coverage at the Right Level

Use component tests for validation presentation, accessibility, loading, duplicate submission, and stable user-visible states. Stub transport and assert roles, labels, focus, and outcomes rather than CSS classes or incidental DOM shape. If a snapshot supplements those assertions, limit it to a stable semantic projection and never let it become the sole evidence. Keep one or a few deployed browser journeys for cross-component wiring.

## Make Deployed Journeys Operable

- Provision isolated test accounts, tenant data, and inbox aliases.
- Use correlation or idempotency identifiers to locate asynchronous work.
- Poll observable completion with a bounded deadline; do not use arbitrary sleeps or whole-test retries.
- Prefer a mailbox or provider API over automating a third-party UI.
- Clean up created state explicitly or use expiring, uniquely named fixtures.
- Treat an unavailable provider as not exercised, never passed.

For security-sensitive journeys, verify externally observable security properties such as account-enumeration equivalence at the responsible boundary. Never place passwords, reset tokens, secrets, or unnecessary personal data in logs, screenshots, traces, snapshots, or CI artifacts.
