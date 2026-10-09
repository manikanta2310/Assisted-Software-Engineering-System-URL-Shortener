# Risks, Security & Validation

This document covers requirement #6 (Validation & Risk Control) and the security
posture referenced in requirement #4 (secure AI usage) and #7 (controlled oversight).

Scope note: this is a **prototype**. Defenses that are appropriate for a prototype
are implemented; defenses that belong to a production/enterprise deployment are
listed explicitly as out of scope rather than silently omitted. Honest scoping is
a deliberate engineering decision, not a gap.

---

## 1. Threat model → defenses (security)

A URL shortener is a redirect engine, which makes it an attractive abuse target.
The table maps each realistic threat to the defense and where that defense lives
in the layered architecture.

| # | Threat | Why it matters here | Defense | Layer / location |
|---|--------|---------------------|---------|------------------|
| S1 | Open redirect / malicious target | A phishing or malware URL hides behind our trusted domain | Allow only `http`/`https` schemes; reject loopback/internal hosts; optional domain denylist | FluentValidation + Domain rule |
| S2 | SQL injection | Classic data-layer attack | EF Core parameterizes all queries; no raw SQL string concatenation | Infrastructure (EF Core) |
| S3 | Abuse / flooding (mass creation) | A bot hammers `POST /api/urls` | Rate-limiting middleware on the create endpoint | API middleware |
| S4 | Short-code enumeration | Attacker walks `/aaa`, `/aab`… to harvest links | Non-sequential random Base62 codes, not incrementing IDs | Domain (code generator) |
| S5 | Injection via custom alias | Alias contains script/path/control characters | Character whitelist, length bounds, reserved-word block | FluentValidation |
| S6 | Oversized / malformed payloads | DoS via huge bodies | Request-size limit, model validation, field length caps | API |
| S7 | Information leakage in errors | Stack traces reveal internals | Centralized exception handler returns clean JSON; never raw exceptions | API middleware |
| S8 | Privacy (analytics) | IP addresses are personal data | Hash or truncate IPs; do not store raw IPs | Application/Domain |
| S9 | Secrets exposure | Connection strings / keys committed | Config via `appsettings` + user-secrets/env vars; never hardcoded; covered by `.gitignore` | Config |
| S10 | Vulnerable dependencies | A NuGet package has a known CVE | `dotnet list package --vulnerable` as a quality gate | Build / CI |
| S11 | Weak browser-side posture | Missing security headers | Standard headers (HSTS, X-Content-Type-Options, Referrer-Policy) | API middleware |

### AI-specific security rules (requirement #4 — secure AI usage)

- **No secrets in prompts.** Connection strings, keys, tokens and real user data
  are never pasted into the AI tool.
- **AI output is reviewed for vulnerabilities**, not trusted blindly — especially
  code touching redirect logic, URL validation, and any data access.
- **AI-suggested packages are verified** before adding: the package is real,
  maintained, and free of known CVEs. (AI can hallucinate or suggest abandoned
  packages.)

---

## 2. Operational risks & guardrails

| # | Risk | Impact | Guardrail |
|---|------|--------|-----------|
| R1 | Short-code collision | Two URLs map to one code → wrong redirect | Uniqueness check + bounded retry in the generator; unit-tested |
| R2 | Expired link still served | Stale/unexpected redirect | `ShortUrl.IsExpired()` domain rule → `410 Gone` (or `404`) |
| R3 | Database unavailable | Create/redirect fail | Graceful error via centralized handler; health-check endpoint; redirect served from cache where possible |
| R4 | Cache/DB inconsistency | Stale redirect after update/delete | Cache invalidation on write; short TTL; documented trade-off |
| R5 | Regression during brownfield change | New feature breaks existing behavior | Regression test suite must stay green before a change is accepted |
| R6 | Invalid input reaches core logic | Unexpected state / crash | Validation at the API edge before the service is called |

---

## 3. Trade-offs (explicitly chosen)

| Decision | Trade-off accepted | Rationale |
|----------|-------------------|-----------|
| SQLite over a server DB | Lower concurrency ceiling | Zero-setup, reproducible; swappable behind `IUrlRepository` |
| In-memory cache over distributed cache | Not shared across instances | Single-instance prototype; interface allows swap to Redis later |
| No authentication | Anyone can create links | Out of scope for prototype; noted as production requirement |
| Random code generation | Rare retry on collision | Prevents enumeration; retry cost is negligible at this scale |
| Built-in .NET analyzers over an external analyzer package | No Sonar-specific rules/dashboard locally | Zero extra tooling; Sonar/SonarQube is an optional CI-layer add-on |

---

## 4. Failure scenarios & expected behavior

| Scenario | Expected behavior |
|----------|-------------------|
| Unknown short code requested | `404 Not Found`, clean JSON |
| Expired link requested | `410 Gone` (documented choice) |
| Invalid URL submitted | `400 Bad Request` with validation detail |
| Duplicate custom alias | `409 Conflict` |
| Rate limit exceeded | `429 Too Many Requests` |
| Unexpected server error | `500` with correlation id; no stack trace leaked |

---

## 5. Out of scope for the prototype (production requirements)

Listed so reviewers see these were considered, not missed:

- Authentication & authorization (API keys / OAuth), per-user link ownership
- Secret-manager integration (e.g. Azure Key Vault)
- Web Application Firewall / DDoS protection at the edge
- Full audit logging and tamper-evident trails
- Distributed cache and multi-instance horizontal scaling
- Automated malicious-URL reputation checks (threat-intel feeds)
