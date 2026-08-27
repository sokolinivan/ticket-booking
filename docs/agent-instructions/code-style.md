# Code Style

Use this file when editing C#, TypeScript, React, or repository formatting.

The root `.editorconfig` requires UTF-8, LF line endings, a final newline, spaces rather than tabs, and no trailing whitespace.

## C#

- Use four-space indentation and braces on new lines.
- Nullable reference types and implicit usings are enabled.
- Prefer file-scoped namespaces and use `var` where consistent with neighboring code and `.editorconfig`.
- Sort `System` using directives first without separating groups by blank lines.
- Treat warnings, analyzers, and code-style diagnostics as build errors.
- Fix warning causes where possible; never suppress a warning globally when a justified, narrow exclusion is sufficient.

## TypeScript and React

- Use two-space indentation, ESM, and strict unused-local and unused-parameter checks.
- Follow `react/rules-of-hooks`; Oxlint also validates component exports.
- Match the local import and JSX style. Do not introduce a formatter without coordinating the change across both frontends.
- Extract shared UI components only after actual reuse appears.
- Never couple `public-web` and `backoffice-web` with direct relative imports.

Run the formatting and lint checks documented in [Verification and Testing](verification.md) before completing formatting changes.
