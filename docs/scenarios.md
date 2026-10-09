# Three Scenarios

Each scenario shows **decomposition → execution → validation**, as required by the
deliverables, and demonstrates a different requirement style.

---

## Scenario 1 — Greenfield (build the core) 🟢

**Requirement:** Build the core shortener: create, redirect, and basic analytics,
from nothing.

### Decomposition
Domain model → repository/generator interfaces → Base62 generator → `UrlService`
→ controllers → tests. (Tasks 1–7 in `requirements.md`.)

### Execution (AI-assisted)
- Domain entity and rules drafted, then hardened (factory enforces valid state;
  `IsExpired`/`RegisterClick` live on the entity).
- Base62 generator: first AI draft used `System.Random`; replaced with a
  cryptographic RNG to prevent enumeration (log entry G-04).
- `UrlService` create flow: generate → check uniqueness → persist; alias path
  checks existence first.
- Redirect endpoint resolves and 302s; click tracking fired off-path (log G-07).

### Validation
- Unit: code length/alphabet/variety; create/alias/collision/expiry; resolve
  found/not-found/expired.
- Integration: `POST` → `GET /{code}` (302) → `GET .../stats` round-trip.
- Acceptance: a reviewer can create, be redirected, and read stats in minutes.

---

## Scenario 2 — Brownfield (enhance existing code) 🟡

**Requirement:** Add **custom aliases** and **link expiry** to the existing
create flow — an enhancement to code that already works.

### Codebase reasoning (impact analysis — requirement #3)

| Area | Impact |
|------|--------|
| `CreateShortUrlRequest` DTO | Add `CustomAlias`, `ExpiresInDays` |
| `CreateShortUrlRequestValidator` | Add alias rules + expiry bounds |
| `UrlService.CreateAsync` | Branch: claim alias vs generate; compute expiry |
| `ShortUrl` entity | `ExpiresAt` field + `IsExpired` rule |
| Resolve path | Return `410` when expired |
| Schema | Nullable `ExpiresAt` column (backward compatible) |
| Tests | New alias/expiry cases; existing tests must stay green |

Data flow touched: create request → validator → service → repository → schema;
and resolve → service expiry check → controller status code.

### Execution (AI-assisted)
- Alias path added behind the existing interface — no change to callers.
- Expiry added as an optional, nullable field so existing links are unaffected
  (log B-02). Schema change reviewed before applying (🔴 high-impact sign-off).

### Validation
- **Regression:** all Scenario 1 tests still pass (no behaviour broken).
- New: duplicate alias → `409`; expired link → `410`; requested expiry honored.
- Safe-change evidence: change isolated behind interfaces; tests gate acceptance.

> Note: in this submission aliases and expiry are built in from the start; this
> section documents the brownfield *process and impact analysis* as if enhancing
> an existing create flow, which is exactly how the change was sequenced.

---

## Scenario 3 — Ambiguous requirement 🟠

**Requirement (as given):** "Add analytics to the shortener." Deliberately vague.

### Clarification (the real work here)
Questions raised, and the scoped answers:

| Question | Decision |
|----------|----------|
| Clicks only, or referrer / UA / geo? | Total clicks, last access, 7-day daily series; referrer + hashed IP stored for future use; no geo |
| Real-time or batch? | Near-real-time; cache evicted on click so stats are current |
| Privacy of IPs? | **Never store raw IP** — store a salted hash only (S8) |
| Impact on redirect latency? | Click tracking is fire-and-forget, off the hot path |
| Unbounded history? | Time-series query bounded to a recent window |

### Execution (AI-assisted)
- `ClickEvent` entity + `IClickTracker` added; tracker hashes IP and writes the
  event, then evicts the cache entry.
- `AnalyticsService` combines aggregate counters with the daily series.

### Validation
- Stats endpoint returns the shape above; privacy decision enforced in code
  (raw IP never persisted).
- Deferred explicitly (documented, not silently dropped): geo lookup, referrer
  breakdown UI, retention policy. These are future scope.

**What this scenario demonstrates:** judgment under ambiguity — surfacing the
unknowns, choosing a defensible scope, protecting privacy, and stating what was
deferred and why.
