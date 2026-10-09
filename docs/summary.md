# Summary

Covers requirement #8.

## 1. Plan & rationale

Build a production-style URL shortener in **.NET 10** using **Clean Architecture**
(Domain / Application / Infrastructure / API), with **Controllers**, **EF Core +
SQLite**, **FluentValidation**, centralised error handling, an in-memory cache on
the redirect path, and an in-build static-analysis gate. The guiding principle
throughout: *AI assists within each task; the engineer owns correctness,
maintainability, and production readiness.* Roughly half the effort is the
`docs/` folder, because the assignment assesses the engineering method, not only
the code.

## 2. Artifacts produced

- Runnable solution: one application project (Clean Architecture by folders) + one test project (`dotnet run`, `dotnet test`).
- API surface: create, redirect, analytics, health; OpenAPI document at
  `/openapi/v1.json` plus interactive **Swagger UI** at `/swagger`; `.http` file.
- Tests: unit (domain, code-gen, validation, service) + integration (end-to-end).
- Docs: `architecture.md`, `requirements.md`, `scenarios.md`, `ai-usage-log.md`,
  `risks.md`, `decisions.md`, `testing.md`, this summary.

## 3. How the 8 core requirements are met

| # | Requirement | Evidence |
|---|-------------|----------|
| 1 | Requirement understanding | `requirements.md` (ambiguities, assumptions, F/NF spec) |
| 2 | Task decomposition | `requirements.md` §6 (ordered tasks + dependencies) |
| 3 | Codebase reasoning (brownfield) | `scenarios.md` Scenario 2 impact analysis |
| 4 | AI-assisted execution | `ai-usage-log.md` (prompts, edited/rejected, gates, sign-off) |
| 5 | Engineering output | Layered code, OpenAPI, EF schema, unit + integration tests |
| 6 | Validation & risk control | `risks.md` (threat model, guardrails, failure modes) |
| 7 | Controlled oversight | Review tiers + sign-offs in `ai-usage-log.md`; thin controllers; tests as gate |
| 8 | Final engineering summary | This document |

## 4. Risks, trade-offs, validation (headline)

- **Security:** URL/alias validation, injection-safe EF Core, rate limiting,
  non-guessable codes, clean errors, hashed IPs. Full detail and the explicit
  out-of-scope list in `risks.md`.
- **Trade-offs:** SQLite over a server DB; in-memory cache over distributed;
  EnsureCreated over migrations; no CQRS/auth in prototype — each documented with
  a rationale and a production upgrade path.
- **Validation:** unit + integration tests; analyzers; dependency CVE scan;
  high-impact changes sign-off-gated.

## 5. Assumptions

- Single-instance deployment for the prototype.
- Links are public (no per-user ownership) in this scope.
- Reviewers have the .NET 10 SDK; no other tooling required.
- Modest analytics volume (client-side daily grouping is acceptable).

## 6. Limitations

- No authentication/authorization, WAF, secret-manager, or distributed cache
  (all documented as production items).
- No load/performance benchmarks; basic SSRF protection only; no EF migrations.
- Click tracking is eventually consistent with stats (short window).

## 7. If continued (next steps)

1. EF Core migrations replacing EnsureCreated.
2. Authentication + per-user link ownership.
3. Redis distributed cache; horizontal scaling.
4. Full SSRF protection (resolved-IP re-check) + malicious-URL reputation.
5. Load testing (k6) and code-level benchmarks (BenchmarkDotNet).
6. CI pipeline running the strict gate (`-warnaserror`) + SonarQube Server.

## 8. Build/run note

The solution was authored in an environment where the .NET SDK could not be
installed (Microsoft's distribution hosts were blocked by the network policy), so
it was validated by design and review rather than by executing the compiler here.
It targets .NET 10 and is intended to be built and run on the reviewer's machine
with `dotnet restore && dotnet run --project UrlShortener` and `dotnet test`. Package versions are pinned via Central Package Management; if a
feed cannot resolve one, the README notes the one-line fix.
