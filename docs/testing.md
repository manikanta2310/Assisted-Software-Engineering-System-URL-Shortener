# Testing Approach, Limitations & Trade-offs

## 1. Strategy

A two-tier test strategy, matching the architecture:

Both tiers live in the single `UrlShortener.Tests` project.

| Tier | Folder | What it proves | Speed |
|------|--------|----------------|-------|
| Unit | `Unit/` | Domain rules, code generation, validation, service orchestration — **no infrastructure** | Fast |
| Integration | `Integration/` | Controllers + middleware + validation + DI end-to-end, over an in-memory EF database | Medium |

The architecture *causes* good tests: because the core depends only on interfaces
and an injectable clock, every rule is testable in isolation with hand-written
fakes (no mocking framework needed).

## 2. What is covered

**Unit**
- `ShortUrl`: construction rules, `IsExpired` boundaries, `RegisterClick`.
- `Base62CodeGenerator`: length, alphabet, output variety (collision rarity).
- `CreateShortUrlRequestValidator`: valid URLs; rejected schemes; rejected
  internal hosts (SSRF); over-length URLs; alias rules; expiry bounds.
- `UrlService`: generate-vs-alias, taken alias → conflict, collision retry,
  generation exhaustion, expiry application, resolve found/not-found/expired.

**Integration**
- Create → redirect (302 + Location) → stats round-trip.
- Invalid URL → 400.
- Unknown code → 404.
- Duplicate alias → 409.
- Health endpoint → 200.

## 3. How to run

```bash
dotnet test
```

Fakes live in `UrlShortener.Tests/Unit/TestDoubles/` (`FakeClock`,
`SequenceCodeGenerator`, `InMemoryUrlRepository`). Integration tests use
`CustomWebAppFactory`, which boots the real pipeline and swaps SQLite for a unique
in-memory database per factory instance.

For manual testing, run `dotnet run --project UrlShortener`; the browser opens
**Swagger UI** at `/swagger` automatically, where every endpoint can be exercised
with *Try it out*. The `UrlShortener/UrlShortener.http` file offers the same
requests for the VS/Rider REST client.

## 4. Quality gates (definition of done)

```bash
dotnet build -warnaserror                       # compiler + built-in .NET analyzers
dotnet format --verify-no-changes               # style
dotnet test                                      # unit + integration
dotnet list package --vulnerable --include-transitive   # dependency CVE scan
```

Default `dotnet build` is intentionally non-blocking on analyzer findings (so a
first build is reproducible); `-warnaserror` is the strict gate CI would run.

## 5. Limitations (honest)

- **No load/performance tests.** Throughput and the cache's effect are reasoned
  about, not measured. BenchmarkDotNet or k6 would be the next step.
- **Click-tracking timing.** Because tracking is fire-and-forget, the integration
  test asserts the stats *shape*, not an exact post-redirect count (would be
  flaky). A deterministic test would expose a synchronous tracking hook.
- **SSRF protection is basic.** Literal internal hosts and private IP ranges are
  blocked; a host that *resolves* to an internal IP is not re-checked. Full SSRF
  defence is a documented production item.
- **No migration tests.** EnsureCreated is used; there are no migrations to test.
- **Concurrency.** The generate-check-insert race is closed by the unique index,
  but a high-concurrency stress test is not included.

## 6. Trade-offs

- In-memory EF for integration tests trades a little fidelity (it is not SQLite)
  for speed and isolation. A SQLite-file or Testcontainers-based suite would be
  higher-fidelity and is the production direction.
- Hand-written fakes over a mocking library keep the test dependencies minimal and
  the intent explicit, at the cost of a little boilerplate.
