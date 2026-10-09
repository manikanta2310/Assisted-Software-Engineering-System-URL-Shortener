# AI Usage Log

This log is the primary evidence for requirement #4 (AI-Assisted Execution) and
requirement #7 (Controlled Oversight). It records how AI was used across the
lifecycle, what was kept, edited or rejected, the quality gates each change passed,
and explicit human sign-off for high-impact changes.

**Operating principle:** AI assists within a task; the engineer owns correctness,
maintainability and production readiness. AI output is treated as a first draft
from a capable junior engineer — useful, but reviewed before acceptance.

---

## How to read this log

Each meaningful task has one entry using the template below. The **Outcome** field
is always one of: `generated-as-is`, `generated-then-edited`, or `rejected`.
High-impact changes (see review tiers) additionally carry a **Sign-off** line.

### Review tiers

| Tier | Change type | Review level |
|------|-------------|--------------|
| 🔴 High | Redirect logic, URL validation / security rules, DB schema & migrations | Line-by-line review + mandatory sign-off |
| 🟠 Medium | Code-generation algorithm, dependency additions | Review + tests |
| 🟡 Low | DTO/service wiring, mapping | Light review, tests confirm |
| 🟢 Trivial | Tests, docs, comments, formatting | Spot-check |

### Quality gates (a change is "accepted" only when all pass)

1. `dotnet build -warnaserror` clean (strict gate; nullable is always an error)
2. Analyzers clean (built-in .NET analyzers)
3. `dotnet format` applied
4. All tests green (unit + integration; regression suite still green)
5. `dotnet list package --vulnerable` clean
6. Sign-off recorded (high-impact only)

---

## Entry template (copy for each task)

```
### [ID] <short task title>

- Tier:            🔴 / 🟠 / 🟡 / 🟢
- Intent:          <what I wanted>
- Constraints:     <.NET 10, Controllers, SQLite, no new deps, etc.>
- Acceptance:      <how I know it's correct>
- Context given:   <files / interfaces / prior code shared with the AI>
- Prompt summary:  <the structured prompt, paraphrased>
- Outcome:         generated-as-is | generated-then-edited | rejected
- Edited because:  <what I changed and why>  (if edited)
- Rejected because:<why discarded>           (if rejected)
- Gates passed:    build ✓ analyzers ✓ format ✓ tests ✓ vuln ✓
- Sign-off:        <name>, <date>            (high-impact only)
```

---

## Worked examples

### [G-04] Short-code generation strategy

- Tier:            🟠 Medium
- Intent:          Generate a unique, non-sequential short code.
- Constraints:     .NET 10; no external deps; must be collision-safe; testable.
- Acceptance:      Codes are Base62, configurable length; collision triggers a
                   bounded retry; covered by unit tests.
- Context given:   `ICodeGenerator` interface, `UrlShortenerOptions`.
- Prompt summary:  Asked for a Base62 generator behind `ICodeGenerator`, using a
                   cryptographically-reasonable random source, with a retry hook
                   for uniqueness handled by the caller.
- Outcome:         generated-then-edited
- Edited because:  First draft used `System.Random` (predictable → enumeration
                   risk, S4). Swapped to a non-predictable source and moved the
                   uniqueness retry into the service so the generator stays pure.
- Gates passed:    build ✓ analyzers ✓ format ✓ tests ✓ vuln ✓

### [G-07] Redirect endpoint (GET /{code})

- Tier:            🔴 High
- Intent:          Resolve a code and redirect, honoring expiry.
- Constraints:     Must check `IsExpired()`; return 404 unknown / 410 expired;
                   read through cache first.
- Acceptance:      Correct status codes; cache hit avoids DB; click tracked
                   without blocking the redirect.
- Context given:   `UrlService`, `CachedUrlRepository`, `ShortUrl` entity.
- Prompt summary:  Asked for the controller action + service method for resolve,
                   with expiry handling and fire-and-forget click tracking.
- Outcome:         generated-then-edited
- Edited because:  Draft awaited click tracking on the hot path (added latency).
                   Changed to fire-and-forget; verified it can't throw into the
                   redirect. Reviewed line-by-line as core behavior.
- Gates passed:    build ✓ analyzers ✓ format ✓ tests ✓ vuln ✓
- Sign-off:        <your name>, 2026-10-08

### [G-09] URL validation rules

- Tier:            🔴 High
- Intent:          Reject malicious / malformed target URLs (S1).
- Constraints:     Allow only http/https; reject loopback/internal hosts.
- Acceptance:      Valid URLs pass; disallowed schemes/hosts rejected with 400;
                   rules unit-tested in isolation.
- Context given:   FluentValidation validator stub; threat table S1.
- Prompt summary:  Asked for a validator enforcing scheme allow-list and host
                   checks, written so each rule is independently testable.
- Outcome:         generated-then-edited
- Edited because:  Draft missed loopback/internal-host rejection; added those
                   and a test per rule. This is a security control → reviewed
                   against the threat model.
- Gates passed:    build ✓ analyzers ✓ format ✓ tests ✓ vuln ✓
- Sign-off:        <your name>, 2026-10-08

### [B-02] Add link expiry (brownfield schema change)

- Tier:            🔴 High
- Intent:          Add `ExpiresAt` to the model and migration.
- Constraints:     Must not break existing rows; regression suite stays green.
- Acceptance:      Migration applies cleanly; existing links default to no expiry;
                   all prior tests still pass.
- Context given:   Impact analysis from `scenarios.md`, `AppDbContext`.
- Prompt summary:  Asked for the entity change + EF migration, nullable column,
                   backward compatible.
- Outcome:         generated-then-edited
- Edited because:  Reviewed the generated migration before applying (schema =
                   hard to reverse). Confirmed nullable + default behavior.
- Gates passed:    build ✓ analyzers ✓ format ✓ tests ✓ vuln ✓
- Sign-off:        <your name>, 2026-10-08

### [X-01] Rejected: AI-suggested caching package

- Tier:            🟠 Medium
- Intent:          Add caching to the redirect path.
- Outcome:         rejected
- Rejected because:AI suggested a third-party caching package I couldn't verify
                   as actively maintained. Used the built-in `IMemoryCache` via a
                   Decorator instead — no new dependency, no supply-chain risk (S10).

---

## Real AI-assisted moments (actual, from building and running this project)

These are not illustrative — they are the genuine cases where AI output was
wrong or incomplete and engineer review caught and corrected it. They are the
clearest evidence of the operating principle: the engineer owns correctness.

### [R-01] Bug caught by running the app: stats query untranslatable on SQLite

- Tier:            🟠 Medium
- Context given:   `UrlRepository.GetDailyClickCountsAsync`, the SQLite provider.
- What AI produced:A LINQ query filtering `c.OccurredAt >= since` in the database.
- What happened:   At runtime the stats endpoint returned HTTP 500 — the SQLite
                   provider cannot translate a `DateTimeOffset` comparison to SQL
                   (`InvalidOperationException`).
- Why tests missed it: the integration tests use the EF InMemory provider, which
                   evaluates the comparison client-side and hides the SQLite-only
                   translation limit (documented limitation in testing.md).
- Fix (engineer): filter by `Code` in SQL (index-backed), then apply the date
                   filter and grouping in memory.
- Outcome:         generated-then-edited
- Gates passed:    build ✓ analyzers ✓ tests ✓ manual run ✓
- Sign-off:        <your name>, 2026-10-09

### [R-02] Bug caught by running tests: two EF Core providers registered

- Tier:            🔴 High (test infrastructure)
- Context given:   `CustomWebAppFactory`, Program.cs service registration.
- What AI produced:A test factory that removed the SQLite `DbContext` registration
                   and added InMemory.
- What happened:   5 integration tests failed: "Only a single database provider
                   can be registered." The removal missed an EF Core 9+ internal
                   registration, so both providers stayed registered.
- Fix (engineer): changed the approach entirely — Program.cs now skips SQLite in
                   the Testing environment, so only one provider is ever present.
                   More robust than add-then-remove.
- Outcome:         generated-then-edited
- Gates passed:    build ✓ tests ✓ (43/43)
- Sign-off:        <your name>, 2026-10-09

### [R-03] Design improvement: alias-taken returned as a result, not thrown

- Tier:            🟠 Medium
- Context given:   `UrlService`, `UrlsController`, the duplicate-alias path.
- What AI produced:`ClaimAliasAsync` threw `AliasAlreadyExistsException` for a
                   taken alias.
- Why changed:     a taken alias is an expected outcome, not an exceptional one.
                   Throwing made the debugger break and was inconsistent with how
                   `ResolveResult` already modelled not-found/expired.
- Fix (engineer): introduced `CreateUrlResult` (Created / AliasAlreadyTaken); the
                   controller maps the latter to a clean 409. Deleted the now-unused
                   exception; updated the unit tests accordingly.
- Outcome:         generated-then-edited
- Gates passed:    build ✓ analyzers ✓ tests ✓
- Sign-off:        <your name>, 2026-10-09

### [R-04] Edited AI-suggested package set: removed redundant Caching.Memory

- Tier:            🟡 Low
- What AI produced:an explicit `Microsoft.Extensions.Caching.Memory` package
                   reference (correct when Infrastructure was a separate library).
- What happened:   after merging to one web project, the build warned NU1510 —
                   the package is already provided by the ASP.NET Core shared
                   framework.
- Fix (engineer): removed the redundant reference; `IMemoryCache` still works.
- Outcome:         generated-then-edited
- Gates passed:    build ✓ (warning cleared)
- Sign-off:        <your name>, 2026-10-09
