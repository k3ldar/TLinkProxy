# TLinkProxy Architecture

TLinkProxy is an OpenAI/Ollama-compatible reverse proxy (.NET 10, ASP.NET Core) between IDE clients (e.g. Visual Studio / GitHub Copilot) and AI backends (llama.cpp, Ollama, OpenAI-compatible). It optimises requests, routes them to the best healthy backend, throttles load, supervises local llama.cpp processes, inspects tool calls in responses, and records token/tool metrics. A web UI provides a dashboard and configuration.

## Solution layout

| Project | Role |
|---|---|
| `TLinkProxy` | Host executable (`Program.cs`). Composes plugins, runs the web app (can run as a Windows Service). Owns routing (`Routing/`), scheduling (`Scheduling/`), cost estimation (`Costing/`), and the dashboard (`HomeController`). |
| `TLinkProxy.Abstractions` | Interfaces and enums only. |
| `TLinkProxy.Base` | Shared models, configuration POCOs (`AiSettings`, `AiModelConfig`, `ToolPolicySettings`, `VisualStudioSettings`), events, `Constants`. |
| `TLinkProxy.Kernel` | OpenAI wire-format DTOs (`OpenAIModel`, `Message`, `Tool`, `ToolCall`, `RoutingEnvelope`, JSON converters). |
| `TLinkProxy.Proxy` | HTTP surface: `ProxyController` (`/v1/*`) and `OllamaController` (`/api/*`). |
| `TLinkProxy.Services` | Backend registry, request parser, session registry/logger, context optimisers, tool-call inspection/policy, event bus, model selector. |
| `TLinkProxy.Clients` | `BackendClientFactory`, `HttpBackendClientBase`, OpenAI/LlamaCpp/Ollama clients, `BackendResponse`. |
| `TLinkProxy.Processes` | Hosted services `LlamaCppProcessManager` and `AiInstanceHealthMonitor`. |
SQLite persistence: settings, AI models, daily token stats, session/tool metrics.
| `TLinkProxy.Configuration` | Configuration UI (`ConfigurationController`, model and Visual Studio settings views). |
| `TLinkProxy.Tests` | Unit tests. |
| `TLinkProxyConsole` | Console regression runner (fixtures, MCP client, response comparer) driven by `RegressionTests`. |

The host references the plugin projects, which depend on `Abstractions`/`Base`/`Kernel` and talk to each other only through `Abstractions` interfaces.

## Plugin composition and DI

registers them (`Db`, `Base`,

Registrations (singletons unless noted):

- **Services**: `IEventBus`, `IProxySessionLogger`, `IContextOptimizerFactory`, `IToolCallPolicy`, `IToolCallInspector`, `ISessionRegistry`, `IAiBackendRegistry`, `IBackendModelSelector`, `IOpenAiRequestParser`.
- **Db**: `SqliteDatabase` (connection and schema), `ISettingsService`, `IAiModelStore`, `ITokenStatsStore`, `ISessionMetricStore`. Database file is `%ProgramData%\TLinkProxy\TLinkProxy.db`; the schema is created on startup.
- **Clients**: `BackendClientFactory`.
- **Processes**: `ILlamaCppProcessManager`, `IAiInstanceHealthMonitor` (also hosted services), `IAiModelRuntimeStateStore`.
- **Host**: `RuleEngine`, `BackendScorer`, `ICostEstimator`, `IBackendRouter`, `IModelScheduler`, named `HttpClient` `"backend"`, cookie auth and session.

`TLinkProxy.PluginInitialisation` also registers transient `IRuleEngine`/`IBackendScorer`/`IBackendRouter`; the host's later singleton registrations take precedence for single-service resolution.

## Request flow (`/v1/*`)

`ProxyController` (`[Route("v1")]`) has an explicit `GET /v1/tags` and a catch-all action for all verbs. Per request (`ProxyAsync`):

1. **Create context** (`CreateContextAsync`)
   - Session id via `ISessionRegistry` (`X-Session-Id`, or hash of api key + client IP + user agent).
   - `IOpenAiRequestParser.ParseAsync` normalises the body into an `OpenAIModel`, extracts a `RoutingEnvelope` (top-level property or reserved system message, which is stripped) and the normalised body.
   - **Select backend** (`ConfigureBackendModelAsync`): first candidate from `IBackendRouter.ChooseCandidates`; fallback is lookup by model alias/model name or endpoint in `IAiBackendRegistry`. On-demand llama.cpp backends are started via `ILlamaCppProcessManager.EnsureStartedAsync`. The forward URL is rewritten to the backend endpoint.
   - **Optimise** with all `IContextOptimizer`s from `IContextOptimizerFactory`; bytes saved are logged and `ITokenStatsStore.RecordAsync` stores before/after sizes.
   - **Schedule** (`ValidateScheduler`): global slot (none → `429`), then per-backend slot (none → `504`, global slot released). Slots are held in `ProxyContext` until the request ends.
2. **Intercept** by path:
   SSE is relayed to the client event by event (`SseStreamRelay`, pooled buffers); once an event carries a non-empty `tool_calls` array the remainder is held, inspected by `IToolCallInspector` and then written (rewritten if policy requires); non-streaming bodies are inspected and written.
   - `/v1/models`, `/v1/show`, `/v1/embeddings` → answered from the backend registry or forwarded to the chosen backend.
with a single process-wide `HttpClient` (`SocketsHttpHandler`, 5-minute connection lifetime, certificate validation disabled as before) shared by all requests, copying
4. **Response inspection**: `IToolCallInspector.InspectResponseAsync` applies `IToolCallPolicy` to tool calls (allow/block, argument redaction), appends block messages, and records metrics via `ISessionMetricStore`.
5. **Cleanup**: `ProxyContext.Dispose` releases scheduler slots. Cancellation, timeouts and exceptions map to `502`.

`OllamaController` (`[Route("api")]`) emulates the native Ollama API: `generate`, `chat`, `embeddings`, `tags`, `show`, `ps`, `version` return real data (translated to/from OpenAI chat format via the backend clients); `pull`, `push`, `create`, `delete` are stubs.

## Routing (`TLinkProxy/Routing`)

- `BackendRouter`: `ChooseCandidates` / `ChooseBackend`. Without a routing envelope an explicit model alias selects a backend directly (legacy behaviour); otherwise rule engine, then scoring.
- `RuleEngine`: ordered pipeline - (1) `preferredModel` first, (2) `requestType` preference ordering, (3) `requiredCapabilities` filtering (`fast-response` is a scoring preference, not a filter).
- `BackendScorer`: weighted sum of capability match, latency, estimated cost (`ICostEstimator`), configured weight, and queue pressure (`IModelScheduler`).

## Scheduling (`TLinkProxy/Scheduling`)

`ModelScheduler` provides back-pressure: a global token channel (capacity `globalQueueCapacity`, fails immediately when empty) plus a `ConcurrencyGate` per backend alias sized by the model's concurrency limit. Gates are swapped on hot-reload when limits change; in-flight requests keep the old gate. `GetQueueDepth` / `GetAvailableSlots` feed scoring and the dashboard.

## Backends and clients

- `AiBackendRegistry` merges `AiSettings` and persisted models (`IAiModelStore`) into `IAiModelConfig`s, tracks health (`SetHealth`/`IsHealthy`) and runtime state, and publishes `ModelsReloadedEvent` on `IEventBus` when models change.
- `BackendClientFactory.CreateFor(IAiModelConfig)` switches on `ModelKind` (`OpenAI`, `LlamaCpp`, `Ollama`) and creates a fresh client per call with `ActivatorUtilities.CreateInstance`; unknown kinds fall back to Ollama; null throws `ArgumentNullException`.
- `HttpBackendClientBase` (template method): uses the named `"backend"` `HttpClient`, POSTs a UTF-8 JSON body to `Endpoint + pathAndQuery`, rewrites `"model"` to the backend model, logs via `IProxySessionLogger`; subclasses supply `ClientLabel` and `ConfigureRequest`.
  - `OpenAIBackendClient` adds `Authorization: <AuthScheme>` from the env var named by `ApiKeyEnvVar` (the scheme defaults to `Bearer`). `HttpBackendClientBase` also attaches any per-model `additionalHeaders` to every backend request.
  - `LlamaCppBackendClient` and `OllamaBackendClient` use no auth.
- `BackendResponse` (`IBackendResponse`) wraps the `HttpResponseMessage` (`StatusCode`, `Headers`, `Content`) and disposes it.
- `IBackendModelSelector` resolves/applies the actual model name for a selected backend.

## Process and health supervision (`TLinkProxy.Processes`)

- `LlamaCppProcessManager`: launches `llama-server` per llama.cpp backend. Always-on backends are supervised and restarted; on-demand backends (idle timeout set) start on `EnsureStartedAsync` and stop after idling. Pipes output to log files, detects port conflicts, reconciles on `ModelsReloadedEvent`, exposes `GetLastActivityUtc`.
- `AiInstanceHealthMonitor`: probes each backend periodically (per-model interval/timeout, backs off when unhealthy, supports auth) and updates registry health.
- `AiModelRuntimeStateStore`: in-memory running/start-count state.

## Context optimisation and tool inspection (`TLinkProxy.Services`)

- `ContextOptimizerFactory` returns optimisers applicable to a request. `VisualStudioContextOptimizer` removes trivial single-word user messages, normalises custom tool calls and completion tokens, prunes tools (e.g. Azure-related), removes empty `tool_choice`, and ensures a session marker message. Configurable through `VisualStudioSettings`.
- `ToolCallPolicy`: file-based rules (hot-reloaded via `FileSystemWatcher`) returning `IToolCallDecision`s, with sensitive-argument redaction.
- `ToolCallInspector`: parses non-streaming and SSE responses, applies policy, rebuilds the response, records invocations.
- `ProxySessionLogger`: console plus optional per-session log files (`LogToFile`).
- `InProcessEventBus`: publish/subscribe; subscriber faults are isolated.

## Persistence (`TLinkProxy.Db`)

SQLite tables for settings, AI models, daily token stats and session metrics,

## Web UI

- `HomeController` (dashboard, `home/status` JSON): backend health, queue/slot state, process activity, token period summaries, tool usage (from incrementally maintained counts in `ISessionMetricStore.GetToolUsageCounts`, not a per-poll scan), optimiser summaries, and host diagnostics. The dashboard polls `home/status` via AJAX.
is read from the `DiagnosticsIntervalSeconds` setting on every pass. A bounded in-memory ring of recent samples (`DiagnosticsHistorySamples`, default 30, 2-720, cleared on restart) backs the dashboard memory graph, served by `GET /home/diagnostics/history`; the page fetches it only when a new sample timestamp appears in `/home/status` and draws an inline SVG chart.
- `ConfigurationController` (`/Configuration`): model list/edit (llama.cpp details kept only for llama.cpp kinds), Visual Studio optimiser settings, diagnostics sampling interval, installed model discovery.

## Configuration

`AI_BACKENDS_CONFIG`, plus per-model

## Design patterns and observations

- **Plugin architecture**: features isolated per assembly, wired by `PluginInitialisation`.
- **Factory + Strategy + Template Method** for backend clients; **Adapter** between backend protocols and `IBackendResponse`.
- **Pipeline** for rules, optimisers, and tool-call policy.
- **Pub/Sub** (`IEventBus`) for model hot-reload across the registry, process manager and health monitor.
- **Resource-per-request**: clients, responses and scheduler slots are per-call disposables.
- Observations: `ProxyController` is large (~990 lines) and duplicates SSE buffering/inspection between the chat intercept and passthrough paths; it creates its own `HttpClient` (certificate validation disabled) instead of `IHttpClientFactory`. SSE responses are fully buffered before re-emission, so clients get no incremental tokens. `TLinkProxy.Proxy.Program.Main` is an empty stub; the host is `TLinkProxy`.
## MCP control plane and tool discovery

Forge can act as the control plane for MCP tools. It is disabled by default; with it off, the request path is unchanged.

Flow when optimization is active: extract the client's tool definitions, register them (plus tools from configured MCP servers) with the discovery provider, strip them from the model-facing request, and expose only `forge.discover` and `forge.describe`. When the model calls `forge.discover`, Forge returns full definitions of the matching tools and adds them to the next request. Calls to discovered MCP tools are executed by Forge against the owning MCP server and the result is fed back to the model. Tools declared by the client are returned to the client for execution.

Responsibilities: Forge owns the model interaction (`TLinkProxy.Services\Mcp`), the discovery system owns tool knowledge (behind `IToolDiscoveryProvider`), MCP servers own execution (`IMcpServer`).

Tool ids are `<server-id>/<tool-name>`; the model-facing name is `<server-id>.<tool-name>`. Discovery is not authorization: a tool can only be executed after it was returned to that session by discovery, and session-owned tools are invisible to other sessions.

Settings are stored as JSON under the `McpSettings` key:

```json
{
  "enabled": true,
  "optimization": { "enabled": true, "fallback": "passthrough" },
  "discovery": { "enabled": true, "provider": "external", "endpoint": "http://localhost:5100", "timeoutMs": 5000, "maxResults": 5 },
  "interception": { "enabled": true, "maxToolIterations": 8 },
  "servers": [ { "id": "gmail", "url": "http://localhost:5200/mcp", "headers": {}, "timeoutMs": 30000 } ]
}
```

`fallback` is `passthrough` (send the request unchanged), `fail` (return 503) or `cached` (search Forge's cached tools). `provider` is `external` (HTTP: `POST tools/register`, `POST tools/search`, `GET tools/{id}`) or `inMemory`. Tool arguments are never logged. Events: `MCP_REQUEST_RECEIVED`, `TOOLS_REGISTERED`, `TOOLS_STRIPPED`, `DISCOVERY_INJECTED`, `DISCOVERY_REQUEST`, `DISCOVERY_RESULT`, `TOOL_CALL_INTERCEPTED`, `TOOL_RESOLVED`, `MCP_TOOL_EXECUTED`, `MCP_OPTIMIZATION_FALLBACK`.

Limitation: when interception is active, chat responses are buffered rather than streamed.
