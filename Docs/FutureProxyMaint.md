# Future Proxy Maintenance — Architectural Improvements

> Scope: `TLinkProxy/Controllers/HomeController.cs`, specifically the `Proxy()` action
> (the `v1/{**catchAll}` route). This document describes maintainability/readability
> improvements only. No code changes are proposed here.

The `Proxy()` method is currently a single action that spans several hundred lines and
performs at least six distinct responsibilities. Each responsibility is described below
with a concrete extraction suggestion so future maintainers can identify the seam.

---

## 1. Problem summary

`Proxy()` mixes these concerns in one method:

1. Request buffering, body logging, and body optimization.
2. Route-specific interception (`/v1/show`, `/v1/chat/completions`, `/v1/models`).
3. Backend selection / routing.
4. Concurrency scheduling and back-pressure.
5. Header/body forwarding to the remote.
6. Response re-streaming (SSE vs buffered) and response mutation.

The result is deep nesting (`if` inside `if` inside `try`), duplicated blocks, and a
method that is very hard to reason about or unit test.

---

## 2. Stop using the Service Locator pattern

The controller repeatedly resolves dependencies via
`HttpContext.RequestServices.GetService(typeof(...))`:

- `IBackendRouter`
- `BackendClientFactory`
- `IModelScheduler`
- `AiBackendRegistry`

This is a service locator / hidden runtime dependency. Per the project's own guidance,
dependencies should be constructor-injected. Most of these are registered as singletons
(see `Docs/Architecture.md`), so they can be injected directly into `HomeController`.

**Recommendation**

- Inject `IBackendRouter`, `BackendClientFactory`, `IModelScheduler`, and `AiBackendRegistry`
  through the constructor (they are already singletons).
- This removes the `GetService(...)` noise, the redundant null checks, and makes the
  dependencies visible and testable.
- Note: `IBackendRouter`/`BackendClientFactory` are only needed for `/v1/chat/completions`,
  so they should move into the dedicated chat handler (see §4) rather than the controller.

---

## 3. Split `Proxy()` into a small orchestration method + focused handlers

The single action should become a thin orchestrator that delegates to a sequence of
extracted, well-named methods/services. Suggested boundaries:

### 3.1 `ReadAndOptimizeBodyAsync(...)`

Extract the request-body read/optimize logic (currently the block that calls
`request.EnableBuffering()`, reads the stream, and calls
`_githubOptimizer.OptimizeAsync(...)`).

- Returns a small result object (body text + `OptimizationResult`), or null when there is
  no body.
- Currently this logic is duplicated conceptually with the later
  `StringContent` / `StreamContent` content-building branches.

### 3.2 Route interception

`TryInterceptShowAsync` already exists as a static helper — good. But two more route
intercepts are currently inlined in `Proxy()`:

- **`/v1/chat/completions`** — the whole backend-routing block should become a dedicated
  handler, e.g. `TryRouteChatCompletionAsync(...)` or a separate `ChatCompletionProxyHandler`
  service. This is the largest, most complex branch and is a prime candidate for its own
  class/file.
- **`/v1/models`, `/api/models`, `*/models`** — the capability-augmentation block
  (deserializing into `Dictionary<string, ToolDataResponseModel>` and injecting
  `["tools", "vision", "completion"]`) should become
  `AugmentModelsCapabilities(responseText)`.

Extracting these makes it obvious where future per-route behavior belongs instead of
burying it in one long method.

### 3.3 Scheduler slot acquisition

The "acquire global slot + per-backend slot, write 429/504 when saturated" logic is
**duplicated twice**:

- once inside the `/v1/chat/completions` branch (`schedulerInner`),
- once in the generic forwarding path (`schedulerTop`).

**Recommendation:** extract one helper that both paths share, e.g.:

```
AcquireProxySlotsAsync(IModelScheduler scheduler, string backendAlias, int timeoutMs, ...)
```

It should return the acquired `IDisposable`s (or a small holder implementing
`IDisposable`) and let the caller decide the error response, so the 429/504 responses are
written in one place.

### 3.4 Response header copying

Header copying appears in several places (forwarded response headers, content headers,
and the `BackendResponse` path). Each block is near-identical except for the source.

**Recommendation:** extract `CopyResponseHeaders(Response, sourceHeaders)` and
`CopyContentHeaders(...)` helpers that also apply the hop-by-hop filter once.

### 3.5 SSE streaming

The SSE streaming loop (read chunk → log snippet → write to `Response.Body` → flush) is
**duplicated** in two places:

- the `BackendResponse` path (chat/completions),
- the forwarded `HttpResponseMessage` path.

**Recommendation:** extract a single `StreamServerSentEventsAsync(Stream source, ...)`
(or take the content and stream it) used by both paths. Also extract the "log a truncated
text snippet or byte count" logic into `LogChunkAsync(...)`.

### 3.6 Response body writing (non-streaming)

The buffered-body copy (`ReadAsByteArrayAsync` + write) is also duplicated between the
two paths. A shared `WriteBufferedResponseAsync(...)` helper would centralize content-type
handling and logging.

---

## 4. Dedicated handler for `/v1/chat/completions` (highest-value change)

The chat completions branch is a self-contained pipeline:

1. Deserialize `OpenAIModel`.
2. Resolve `IBackendRouter` → `ModelConfig`.
3. Create an `IBackendClient` via `BackendClientFactory`.
4. Acquire scheduler slots.
5. Send and re-stream the response.

This is a textbook candidate for a separate service/handler class, e.g.
`ChatCompletionProxyHandler`, injected into the controller. Benefits:

- `HomeController` no longer needs `IBackendRouter` or `BackendClientFactory`.
- The routing/scheduling/streaming logic becomes unit-testable in isolation.
- The controller keeps only the "is this `/v1/chat/completions`? delegate" decision.

This is the clearest "future duplicate" warning: if any other route needs
backend-routing + scheduling + SSE streaming, it should reuse this handler, not copy the
branch.

---

## 5. Reduce duplication & magic values

- **`8192`** (SSE buffer) and **`1000`** (log snippet truncation) are magic numbers.
  Move them to `Constants.cs` with descriptive names (per project conventions).
- **`"text/event-stream"`** and the hop-by-hop header names are string literals repeated in
  the method; promote to named constants.
- **`Console.WriteLine(...)`** is used in a few spots alongside `_sessionLogger`.
  Standardize on `_sessionLogger` so all proxy activity lands in one place.
- **Empty `catch { }` blocks** silently swallow exceptions. At minimum, log them via
  `_sessionLogger`; prefer catching specific exceptions.

---

## 6. Flatten nesting / reduce boolean noise

- The scheduler lookups are wrapped in excessive parentheses:
  `((((HttpContext.RequestServices.GetService(...)) as IModelScheduler)))`.
  Constructor injection (§2) removes this entirely.
- The chat branch is `if (pathAndQuery == "/v1/chat/completions")` containing a large
  `try` with several nested `if (x != null)` guards. Extracting the handler (§4) and
  early-returning for each null condition will collapse the nesting depth.
- Prefer early returns / guard clauses so the "happy path" reads top-to-bottom without
  many levels of indentation.

---

## 7. Response error writing is repetitive

`429 Too Many Requests`, `504 Gateway Timeout`, and `502 Bad Gateway` responses are each
written inline with `Response.WriteAsync(...)` and a log line. Extract a small helper such
as `WriteProxyErrorAsync(HttpStatusCode, string, string sessionId)` so error responses are
consistent and single-sourced.

---

## 8. Suggested target shape (for orientation)

```
HomeController.Proxy()
  ├── ReadAndOptimizeBodyAsync(...)
  ├── TryInterceptShowAsync(...)            // exists
  ├── if chat/completions → ChatCompletionProxyHandler.HandleAsync(...)
  ├── if models → AugmentModelsCapabilities(...)
  └── ForwardAsync(...)
		├── AcquireProxySlotsAsync(...)
		├── CopyRequestHeaders(...)
		├── SendAsync(...)
		├── CopyResponseHeaders(...)
		└── StreamServerSentEventsAsync(...) | WriteBufferedResponseAsync(...)
```

Each arrow above is a unit-testable seam. This is the recommended end-state; implement it
incrementally rather than as one big refactor.
