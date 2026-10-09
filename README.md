# URL Shortener (.NET 10, Clean Architecture)

A production-style URL shortener built as an AI-assisted engineering exercise.
It exposes core shortening APIs, redirects, and click analytics, and is organised
into four clean-architecture layers with a documented AI-assisted workflow.

> Engineering principle followed throughout: **AI assists within each task; the
> engineer owns correctness, maintainability and production readiness.** See
> `docs/ai-usage-log.md`.

---

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` should print `10.x`)
- Nothing else. The database is SQLite (a local file, created on first run) —
  no server, no Docker, no external services.

---

## Run it

```bash
# from the repository root
dotnet restore
dotnet run --project UrlShortener
```

The API starts on the URL printed in the console (typically `https://localhost:7xxx`
and `http://localhost:5xxx`). A SQLite file `urlshortener.db` is created
automatically in the API project folder on first run.

- **Swagger UI** (interactive API explorer): open `https://localhost:7001/swagger` in a browser
- OpenAPI document: `GET /openapi/v1.json`
- Health check: `GET /health`

A ready-to-use request collection is in `UrlShortener/UrlShortener.http`
(open it in Visual Studio, Rider, or VS Code with the REST Client extension).

---

## Try the API

```bash
# 1) Create a short URL
curl -k -X POST https://localhost:7001/api/urls \
  -H "Content-Type: application/json" \
  -d '{"longUrl":"https://learn.microsoft.com/dotnet/","customAlias":null,"expiresInDays":30}'
# → 201 Created  { "code": "Ab3xQ9z", "shortUrl": "https://localhost:7001/Ab3xQ9z", ... }

# 2) Follow the redirect (use the code from step 1)
curl -k -i https://localhost:7001/Ab3xQ9z
# → 302 Found, Location: https://learn.microsoft.com/dotnet/

# 3) See analytics
curl -k https://localhost:7001/api/urls/Ab3xQ9z/stats
# → 200 OK  { "code": "...", "totalClicks": 1, "lastAccessedAt": "...", ... }
```

---

## Test it

```bash
dotnet test
```

Runs both suites from the single test project `UrlShortener.Tests`:
- `Unit/` — domain rules, code generation, validation, service logic (no database).
- `Integration/` — full API through `WebApplicationFactory` with an in-memory database.

---

## Quality gates

Run before accepting any change (see `docs/ai-usage-log.md`):

```bash
dotnet build -warnaserror        # build + built-in .NET analyzers clean
dotnet format --verify-no-changes # formatting / style
dotnet test                       # all tests green
dotnet list package --vulnerable --include-transitive   # dependency scan
```

---

## Project structure

One application project. Clean Architecture is expressed through folders and
namespaces, not separate projects.

```
UrlShortener/                  # the single application project
  Domain/                      # entities, value objects, rules
  Application/                 # services, DTOs, validators, interfaces (orchestration)
  Infrastructure/              # EF Core + SQLite, repositories, cache, code generator
  Api/                         # controllers, middleware
  Program.cs                   # composition root (DI wiring, pipeline)
UrlShortener.Tests/            # the single test project
  Unit/                        # domain, code generation, validation, service tests
  Integration/                 # full API via WebApplicationFactory
docs/                          # architecture, scenarios, AI usage log, risks, decisions
```

Dependency rule (by convention within the assembly): `Api → Application → Domain
← Infrastructure`. Everything points inward to Domain; Domain references no other
layer. See `docs/decisions.md` ADR-009 for the single-project trade-off.

---

## Documentation (`docs/`)

| File | Contents |
|------|----------|
| `architecture.md` | Layers, patterns, request flows, key decisions |
| `requirements.md` | Ambiguities, assumptions, functional + non-functional spec, task breakdown |
| `scenarios.md` | Greenfield, brownfield, ambiguous — decomposition / execution / validation |
| `ai-usage-log.md` | Prompts, generated/edited/rejected, quality gates, sign-offs |
| `risks.md` | Threat model, guardrails, trade-offs, failure modes, out-of-scope |
| `decisions.md` | Architecture decision records (ADRs) |
| `testing.md` | Test strategy, limitations, trade-offs |
| `summary.md` | Final engineering summary |

---

## Configuration

`appsettings.json` → `UrlShortener` section:

| Key | Default | Meaning |
|-----|---------|---------|
| `BaseUrl` | (request origin) | Base used to build the returned short URL |
| `CodeLength` | 7 | Length of generated Base62 codes |
| `MaxGenerationRetries` | 5 | Collision retries before failing |
| `MaxUrlLength` | 2048 | Longest accepted target URL |
| `DefaultExpiryDays` | null | Default link lifetime (null = never expires) |

Connection string is in `ConnectionStrings:Default`. Secrets are never hardcoded;
use `dotnet user-secrets` or environment variables for anything sensitive.
