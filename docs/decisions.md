# Architecture Decision Records (ADRs)

Short, defensible records of the significant choices. Format: context → decision →
consequences.

---

## ADR-001 — API style: Controllers

**Context:** Minimal APIs, Controllers, or FastEndpoints.
**Decision:** Controllers.
**Why:** Clear structure as the endpoint surface grows (create, redirect, stats),
instantly recognizable to any .NET reviewer, and no third-party dependency to
vet (as FastEndpoints would add). Minimal APIs were equally valid for a smaller
surface; Controllers win on recognizability and maintainability here.
**Consequences:** Slightly more ceremony than Minimal APIs; controllers kept thin.

---

## ADR-002 — Storage: EF Core + SQLite

**Context:** Need persistence that a reviewer can run with zero setup.
**Decision:** EF Core with SQLite, behind `IUrlRepository`.
**Why:** SQLite is a single file — no server, no Docker. EF Core parameterizes
queries (injection-safe). The repository interface means moving to SQL Server or
PostgreSQL is a configuration + provider change, not a rewrite.
**Consequences:** Lower concurrency ceiling than a server DB; acceptable for a
prototype and documented as the production upgrade path.

---

## ADR-003 — Database bootstrap: EnsureCreated (not migrations)

**Context:** Schema needs to exist on first run; migrations could not be generated
in the authoring environment.
**Decision:** `Database.EnsureCreated()` at startup for the prototype.
**Why:** Zero-setup, deterministic first run.
**Consequences:** No incremental migration history. Production replaces this with
`dotnet ef migrations` + `Migrate()`. Documented in README and `scenarios.md`.

---

## ADR-004 — Validation: FluentValidation

**Context:** URL/alias/expiry rules are non-trivial and security-relevant.
**Decision:** FluentValidation over DataAnnotations.
**Why:** Rules are independently unit-testable and expressive; URL safety is a
security control (S1) that deserves first-class, tested rules.
**Consequences:** One extra dependency; justified by testability and clarity.

---

## ADR-005 — No CQRS

**Context:** CQRS/MediatR is common in layered .NET samples.
**Decision:** Plain services, no CQRS.
**Why:** The domain is small with symmetric read/write needs. CQRS would add
command/handler plumbing without a payoff here.
**Consequences:** If analytics querying grows complex, CQRS is the documented
place to evolve to.

---

## ADR-006 — Static analysis: built-in .NET analyzers

**Context:** The quality gate needs static analysis + linting.
**Decision:** Use the built-in .NET (Roslyn) analyzers at `AnalysisMode=Recommended`,
enforced in-build. No external analyzer package.
**Why:** The built-in analyzers cover correctness, performance, and security rules
with zero extra tooling — a reviewer gets the gate by running
`dotnet build -warnaserror`. An earlier version added the `SonarAnalyzer.CSharp`
package, but it ships a separate net472 build-host process that surfaced an
unfamiliar `BuildHost-net472` node in Solution Explorer and added toolchain weight;
it was removed in favour of the built-in analyzers.
**Consequences:** No Sonar-specific rules or dashboard locally. If richer analysis
is wanted at team scale, `SonarAnalyzer.CSharp` (in-build) or a hosted SonarQube
Server wired into CI is the documented upgrade path — added at the CI layer, not
in the day-to-day local build.

---

## ADR-007 — Caching: in-memory Decorator, not a distributed cache

**Context:** The redirect path benefits from caching.
**Decision:** `IMemoryCache` via a `CachedUrlRepository` decorator, short TTL,
evicted on click.
**Why:** No external dependency; the Decorator adds caching without touching the
real repository. AI initially suggested a third-party cache package that could not
be verified — rejected in favour of the built-in cache (log X-01).
**Consequences:** Cache is per-instance. A distributed cache (Redis) is the
multi-instance path; the interface already allows the swap.

---

## ADR-009 — Single project, Clean Architecture by folders

**Context:** Clean Architecture can be realised as separate projects (one per
layer) or as folders/namespaces within one project.
**Decision:** One application project, with `Domain/`, `Application/`,
`Infrastructure/`, and `Api/` as folders; one separate test project.
**Why:** Keeps the solution simple to open, build, and review (two entries, not
six) while preserving the layered structure and the inward dependency rule. For a
service of this size, project-per-layer adds solution ceremony without added
safety that matters here.
**Consequences:** The compiler no longer *enforces* the dependency direction
(e.g. nothing physically stops Domain code from referencing Infrastructure) — the
rule is upheld by convention and code review. If strict enforcement were needed,
an architecture test (e.g. NetArchTest) or a later split back into projects would
restore it. The namespaces (`UrlShortener.Domain.*`, etc.) are already structured
so that such a split would be mechanical.

---

## ADR-008 — Click tracking off the hot path

**Context:** Recording a click must not slow or break a redirect.
**Decision:** Fire-and-forget `IClickTracker` that owns its own DI scope and
swallows its own errors.
**Why:** The redirect is the highest-impact behaviour; analytics must never
jeopardise it.
**Consequences:** Stats can lag a redirect by a brief moment; acceptable and
documented (R4).
