# Security and Data

Use this file for authentication, browser mutations, booking, payment, ticket issuance, observability, or configuration involving sensitive values.

- Never commit secrets, `.env` files, connection strings, tokens, certificates, or user-specific IDE files. Use .NET user secrets for local AppHost secrets; its project already has a `UserSecretsId`.
- Never log personal data, session cookies, payment details, or tokens.
- Keep customer and staff authentication separate. A customer cookie must never grant administrative privileges.
- For state-changing browser requests, account for CSRF, secure cookie attributes, and policy-based authorization. These controls are not implemented yet.
- Booking, payment webhooks, and ticket issuance require concurrency, idempotency, and redelivery tests; happy-path coverage is insufficient.
- `ServiceDefaults` exposes health endpoints only in Development intentionally. Do not expose them in production without reviewing access controls and the data they reveal.
