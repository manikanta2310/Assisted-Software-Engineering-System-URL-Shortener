# Requirements (Interpretation, Ambiguities, Spec)

Covers requirement #1 (Requirement Understanding) and feeds requirement #2 (Task
Decomposition).

## 1. Interpreting the assignment

The brief says "Build an AI-Assisted Software Engineering System." Read literally
that could mean building an autonomous agent that writes code. The objective
corrects this: *"engineer-led execution accelerated by AI, not autonomous
orchestration."* So the deliverable is a **URL shortener built by the engineer
with AI assistance**, plus the evidence of a disciplined, traceable process. The
URL shortener is the vehicle; the engineering method is what is assessed.

## 2. Personas (who uses it)

- **End user** — creates short links and is redirected by them.
- **Link owner / marketer** — inspects analytics (clicks, recency, trend).
- **Operator / SRE** — relies on health, reliability, abuse resistance.
- **Reviewer (primary audience here)** — runs the code and reads the docs.

## 3. Ambiguities surfaced, and the decisions taken

| # | Ambiguity | Decision | Rationale |
|---|-----------|----------|-----------|
| A1 | Code length / alphabet | 7-char Base62, configurable | ~3.5 trillion keyspace; short yet collision-safe |
| A2 | Collision handling | Generate → check → retry (bounded); unique index as backstop | Simple, correct, keyspace makes retries rare |
| A3 | Custom aliases allowed? | Yes, validated (chars, length, reserved words) | Common shortener feature; low cost |
| A4 | Expiry supported? | Yes, optional per-link; configurable default | Expiry is a core reliability/space concern |
| A5 | Expired link response | `410 Gone` (distinct from `404`) | Semantically precise; aids clients |
| A6 | Analytics depth | Total clicks + last access + 7-day daily series | Useful without over-building; privacy-aware |
| A7 | Privacy of analytics | Store **hashed** IP only, never raw | IPs are personal data (GDPR-style caution) |
| A8 | Auth? | None in prototype; documented as production item | Out of scope; keeps prototype runnable |
| A9 | Storage | SQLite, behind a repository interface | Zero-setup; swappable to a server DB |
| A10 | Open-redirect / SSRF risk | Validate scheme + block internal hosts | Signature risk of a redirect service |

## 4. Functional requirements

- **F1** Create a short URL from a long URL (`POST /api/urls`).
- **F2** Optional custom alias and optional expiry on creation.
- **F3** Redirect a short code to its target (`GET /{code}`) with `302`.
- **F4** Return `404` for unknown codes, `410` for expired codes.
- **F5** Report analytics for a code (`GET /api/urls/{code}/stats`).
- **F6** Record a click (count, last access, time-series) per resolution.
- **F7** Expose a health endpoint, an OpenAPI document, and an interactive
  Swagger UI (`/swagger`) for exploring the API.

## 5. Non-functional requirements

- **N1 Security** — input validation, injection-safe data access, abuse control,
  non-guessable codes, clean errors, privacy-aware analytics (see `risks.md`).
- **N2 Reliability** — expiry honored, health check, graceful error handling,
  click tracking never breaks a redirect.
- **N3 Performance** — redirect served through a short-lived cache; click tracking
  off the hot path.
- **N4 Maintainability** — layered architecture, SOLID, tests, analyzers.
- **N5 Testability** — core logic testable with no infrastructure (interfaces,
  injectable clock).
- **N6 Portability** — runs with only the .NET SDK; no Docker or server DB.

## 6. Task decomposition (requirement #2)

Ordered by dependency; each task has a clear acceptance check.

1. **Domain model** — `ShortUrl`, `ClickEvent`, `ShortCodeRules`, exceptions.
   *Done when:* expiry/click rules are unit-tested.
2. **Application contracts** — repository/generator/clock/tracker interfaces, DTOs.
   *Done when:* the core compiles with no infrastructure dependency.
3. **Code generation (Strategy)** — Base62, crypto RNG.
   *Done when:* length + alphabet + variety tests pass.
4. **Validation** — URL safety + alias rules (FluentValidation).
   *Done when:* scheme/host/alias tests pass.
5. **Services** — `UrlService` (create/resolve), `AnalyticsService`.
   *Done when:* create/alias/collision/expiry/resolve tests pass.
6. **Infrastructure** — EF Core context + configs, repository, cache decorator,
   click tracker, clock.
   *Done when:* schema builds; repository behaves under integration tests.
7. **API** — controllers, exception handler, security headers, rate limiting,
   OpenAPI, DI wiring.
   *Done when:* end-to-end integration tests pass.
8. **Quality gates + docs** — analyzers, tests green, vuln scan, documentation.
   *Done when:* `dotnet build -warnaserror` and `dotnet test` are clean.

Dependency sketch: `1 → 2 → {3,4,5} → 6 → 7 → 8`.
