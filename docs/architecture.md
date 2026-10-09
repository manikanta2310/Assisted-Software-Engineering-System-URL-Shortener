# Architecture Overview

## 1. Layers

Four clean-architecture layers, expressed as folders/namespaces within a single
application project (see ADR-009). The dependency rule is the point:
`Api → Application → Domain ← Infrastructure`. Everything points inward to Domain;
Domain references no other layer. In a single project this rule is upheld by
convention and review rather than compiler-enforced.

```
┌──────────── API ────────────┐  HTTP only
│ Controllers, middleware,     │  UrlsController, RedirectController,
│ DI wiring, OpenAPI + Swagger │  GlobalExceptionHandler, SecurityHeaders
├──────── APPLICATION ─────────┤  Orchestration ("what to do")
│ UrlService, AnalyticsService │  DTOs, FluentValidation,
│ interfaces, options          │  IUrlRepository/ICodeGenerator/IClickTracker/IClock
├────────── DOMAIN ────────────┤  Pure business rules (depends on nothing)
│ ShortUrl, ClickEvent,        │  IsExpired(), RegisterClick(),
│ ShortCodeRules, exceptions   │  factory construction, domain exceptions
├──────── INFRASTRUCTURE ──────┤  The outside world
│ EF Core + SQLite, repository │  UrlRepository, CachedUrlRepository,
│ cache, code generator, clock │  Base62CodeGenerator, ClickTracker, SystemClock
└──────────────────────────────┘
```

Interfaces are defined *inward* (Application/Domain) and implemented *outward*
(Infrastructure). This is the Dependency Inversion Principle, and it is what makes
the core unit-testable without a database and the storage swappable.

## 2. Design patterns (each earns its place)

| Pattern | Where | Why |
|---------|-------|-----|
| Repository | `IUrlRepository` → `UrlRepository` | Keeps core DB-agnostic; injection-safe queries |
| Dependency Injection | Everywhere (`Program.cs`, `AddApplication`, `AddInfrastructure`) | The backbone; enables testing + swapping |
| Strategy | `ICodeGenerator` → `Base62CodeGenerator` | Swap code-gen scheme without touching callers |
| Decorator | `CachedUrlRepository` wraps `UrlRepository` | Adds caching without modifying the repo (Open/Closed) |
| Options | `UrlShortenerOptions` | Strongly-typed config, no magic numbers |
| Result object | `ResolveResult` | Expected outcomes (not-found/expired) as data, not exceptions |
| Middleware / Chain of Responsibility | Exception handler, rate limiter, security headers | Cross-cutting concerns, each isolated |

CQRS is deliberately **not** used — see `decisions.md` ADR-005.

## 3. Request flows

Every arrow that crosses a layer lands on an **interface**, never a concrete
class — that is what makes the core testable and the storage swappable.

### End-to-end request lifecycle (the big picture)

This is the full round trip every request makes — from the client (a browser,
`curl`, or the **Swagger UI** at `/swagger`) through the middleware pipeline and
the four layers, down to the database, and back out as a response.

```
   ┌─────────────────────────────┐
   │  Client                     │   browser · curl · Swagger UI (/swagger)
   │  sends an HTTP request (JSON)│
   └──────────────┬──────────────┘
                  │  request
                  ▼
 ╔════════════════════════════════════════════════════════╗
 ║  MIDDLEWARE PIPELINE  (Program.cs, in order)            ║
 ║  Exception handler → Security headers → HTTPS redirect  ║
 ║  → Rate limiter → routing to a controller               ║
 ╚════════════════════════════┬═══════════════════════════╝
                              ▼
     [API]   Controller            receive, validate, delegate
                              │
                              ▼
     [APP]   Service (UrlService / AnalyticsService)   orchestrate
                              │        │
                              │        └──► [DOMAIN] rules: IsExpired(),
                              │                   code validity, RegisterClick()
                              ▼
     [INFRA] Repository (+ cache)  →  EF Core  →  SQL  →  SQLite (urlshortener.db)
                              │
                              ▼
     response travels back up the same path
                              │
                              ▼
 ╔════════════════════════════════════════════════════════╗
 ║  Response leaves the pipeline                           ║
 ║  201 / 302 / 200  — or a clean error (400/404/409/410)  ║
 ║  shaped as JSON (ProblemDetails on errors)              ║
 ╚════════════════════════════┬═══════════════════════════╝
                              ▼
   ┌─────────────────────────────┐
   │  Client receives the reply  │
   └─────────────────────────────┘
```

**Where Swagger fits:** Swagger UI (`/swagger`) is simply a built-in web client
for this API. It reads the OpenAPI document the app generates at
`/openapi/v1.json` and renders a "Try it out" page, so a reviewer can exercise
every endpoint in the browser without `curl` or a REST client. It is wired in
`Program.cs` via `app.UseSwaggerUI(...)` and sits in front of the same pipeline
shown above — it does not bypass validation, security headers, or rate limiting.

### Create — `POST /api/urls`  (optimised for correctness)

```
          POST /api/urls
                │
   [Api]   UrlsController ──────────────► Exception handler
                │  validate → delegate      (any failure → clean JSON,
                ▼                             no stack trace leaked)
 [App]   UrlService.CreateAsync ──────► ICodeGenerator
                │  generate · check          (Strategy · Base62,
                │  uniqueness · persist        crypto-random)
                ▼
 [Infra] UrlRepository (EF Core)
                │  parameterised query (injection-safe)
                ▼
             SQLite
                │
                ▼
        201 Created · { shortCode }
```

Flow: the controller validates (bad/internal URLs rejected here with `400`), then
`UrlService` asks `ICodeGenerator` for a code, confirms it is unique (retrying on
the rare collision), and saves it. A taken custom alias returns `409`.

### Redirect — `GET /{code}`  (optimised for speed, the hot path)

```
            GET /{code}
                │
  [Api]   RedirectController
                │  resolve → redirect
                ▼
 [App]   UrlService.ResolveAsync
                │  look up · IsExpired() (Domain rule)
                ▼
 [Infra] CachedUrlRepository ──► cache HIT  → return immediately
                │  (Decorator)
                │  cache MISS
                ▼
             SQLite ──► populate cache
                │
                ▼
          302 Found → original URL ──► ClickTracker
          404 unknown / 410 expired     (fire-and-forget,
                                          off the hot path)
```

Flow: a cache hit skips the database entirely. A live link returns `302`; an
unknown code `404`; an expired one `410`. On success, click tracking is fired in
the background (its own DI scope, its own error handling) so analytics can never
delay or break a redirect.

### Analytics — `GET /api/urls/{code}/stats`

```
  AnalyticsController → AnalyticsService → UrlRepository
     (counters + recent daily click series) → 200 OK   (404 if unknown)
```

### Mental model (one paragraph)

> A request enters a **Controller** (Api) → a **Service** orchestrates it
> (Application) → the **Domain** enforces the rules → **Infrastructure** does the
> I/O. **Create** favours correctness (validate hard, unique code, safe persist);
> **Redirect** favours speed (cache first, minimum work, analytics off to the
> side). Everything is wired once, in `ServiceCollectionExtensions.cs`.

## 4. Key decisions (summary — full ADRs in decisions.md)

- Controllers over Minimal APIs / FastEndpoints — recognizable, structured, no extra dependency.
- SQLite behind a repository interface — zero-setup, swappable.
- Single project, layers as folders (Domain / Application / Infrastructure / Api) — layering without project-per-layer ceremony (ADR-009).
- Built-in .NET analyzers (no external analyzer package) — zero operational cost; Sonar is an optional CI-layer add-on.
- No Docker, no CQRS, no auth in the prototype — scoped out and documented.

## 5. Control flow of a running system

1. `Program.cs` builds the host, binds options, registers all three layers,
   creates the DB (EnsureCreated), and wires the middleware pipeline.
2. Requests pass through: exception handler → security headers → (HSTS/HTTPS) →
   rate limiter → controllers.
3. Controllers stay thin; services hold orchestration; the domain holds rules;
   infrastructure does I/O.

## 6. API documentation & exploration (OpenAPI + Swagger)

- The app generates an **OpenAPI** document at `/openapi/v1.json` describing every
  endpoint, request, and response shape.
- **Swagger UI** is served at `/swagger` and renders that document as an
  interactive page. On launch (F5) the browser opens it automatically
  (`launchUrl: "swagger"` in `launchSettings.json`).
- This gives reviewers a zero-setup way to try the API: expand an endpoint,
  click *Try it out*, send a request, and read the live JSON response — no
  external tool required.
- Package: `Swashbuckle.AspNetCore.SwaggerUI` (UI only; the OpenAPI document
  itself comes from the built-in `Microsoft.AspNetCore.OpenApi`).
