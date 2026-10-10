# How Model Selection Works

This page explains, in plain English, how TLinkProxy decides **which AI model** answers a chat request.
It covers the step-by-step decision process, the levers an end user can pull, and how **tags** fit into
the picture.

If you want the developer-level view of the config file or the backend plumbing, see
[`Configuration.md`](Configuration.md) and [`Architecture.md`](Architecture.md). This page is about the
*selection* logic specifically.

---

## 1. The big picture

TLinkProxy is a proxy that sits between you and one or more AI "backends" (local llama.cpp servers, a
remote Ollama server, or a cloud API such as DeepSeek). Each backend is defined in `ai-backends.json` and is
given:

- an **alias** — a short, human-friendly name such as `mfdoom`, `qwen-remote`, `llama-local`, or `deepseek`;
- a **model** — the actual model name that backend understands (e.g. `qwen3.5:9b`); and
- **tags** — descriptive labels about what that model is good at (e.g. `code-review`, `fast-response`).

When you send a chat request, you are not directly calling a backend. The proxy looks at your request,
figures out what you need, and *picks* a backend for you. Model selection = **backend selection**.

---

## 2. What the proxy looks at, in order

The whole decision happens in `Routing/BackendRouter.cs`, with help from `Routing/RuleEngine.cs`
(order/filter) and `Routing/BackendScorer.cs` (final ranking). The flow is:

1. **Narrow down to healthy backends.**
   Any backend that is disabled (`enabled: false`) or currently failing its health check is ignored.
   If none are healthy, the request has nowhere to go and the proxy falls back to the legacy remote
   endpoint (`REMOTE_OLLAMA_URL`) — or returns an error if that isn't set either.

2. **Check for a direct request.**
   If you *don't* provide routing hints but *do* provide a `model` name, the proxy tries to match that name
   directly:

   - first against a backend **alias** (`GetBackendByAlias`, case-insensitive);
   - then against a backend's actual **model** name.

   This is what makes aliases useful as ordinary model names: send `"model": "llama-local"` and you get that
   backend, no other logic needed.

3. **Apply routing hints (if you supplied any).**
   A routing envelope can reorder or filter the candidates (see §3).

4. **Score and rank the survivors.**
   Every remaining backend gets a numeric score and the highest score wins (§5).

The important mental model: **aliases are for direct selection, tags are for "pick the best fit".**

---

## 3. The routing envelope

A chat request can optionally carry a small JSON object called the routing envelope. It is *not* part of
`ai-backends.json` — it is sent **with each request**. The proxy strips it out before forwarding, so the
backend never sees it. It can be sent two ways:

1. **Top-level field** (preferred):

   ```json
   {
     "model": "anything",
     "routing": {
       "requestType": "code-review",
       "preferredModel": "mfdoom",
       "requiredCapabilities": ["long-context"],
       "timeoutMs": 120000
     },
     "messages": [ ... ]
   }
   ```

2. **Reserved system message** (fallback for clients that can't add a top-level field): a system message whose
   content starts with the marker `[[OLLAMAPROXY_ROUTING]]` followed by the routing JSON.

The fields (`Configuration/RoutingEnvelope.cs`):

| Field | Meaning |
|-------|---------|
| `requestType` | The *kind* of work: `code-review`, `ticket-review`, `chat`, or `analysis`. Used to reorder candidates by preference. |
| `preferredModel` | An explicit "use this one if available" hint (matched against backend alias). Moves that backend to the front. |
| `requiredCapabilities` | Hard filters: `gpu`, `tool-calling`, or `long-context`. Any backend failing one is dropped. |
| `timeoutMs` | Optional per-request timeout override. |
| `payload` | Placeholder for future use; not part of selection today. |

### Request type preferences

`Routing/RuleEngine.cs` maps each `requestType` to an ordered preference list. Entries are matched against a
backend's **alias first, then its tags**:

| `requestType` | Preference order |
|---------------|------------------|
| `code-review` | `mfdoom` (alias), then anything tagged `code-review` |
| `ticket-review` | `qwen` (alias), then anything tagged `ticket-review` |
| `chat` | `llama-local` (alias), then anything tagged `general-chat`, then `fast-response` |
| `analysis` | `deepseek` (alias), then anything tagged `analysis`, then `long-context` |

Backends that match none of the preferences are simply appended afterwards in their original order. This is
**reordering, not filtering** — a `requestType` alone never removes a backend.

### Required capabilities

Unlike `requestType`, these are **hard filters**:

- `gpu` — drop any backend whose `compute` is `cpu`.
- `tool-calling` — keep only backends tagged `tool-calling`.
- `long-context` — keep only backends whose `maxContext` is at least (estimated input tokens + a 1024-token
  safety buffer).

Note that `fast-response` is *not* a filter here — it is a scoring preference (see §5).

---

## 4. How tags are used

Tags live on each backend (`"tags": [...]` in `ai-backends.json`) and are the main way to describe *what a
model is good at* without hard-coding a specific alias. The same tag can appear on multiple backends, which
lets the proxy pick whichever matching backend is healthiest and cheapest at that moment.

Where tags matter:

| Mechanism | How tags are used |
|-----------|-------------------|
| `requestType` ordering (`RuleEngine.OrderByPreference`) | A preference entry matches a backend if it equals the backend's **alias** OR appears in its **tags** (case-insensitive). |
| `requiredCapabilities` (`RuleEngine.FilterByCapability`) | `tool-calling` and `long-context` are matched against tags; `gpu` is matched against the `compute` field instead. |
| Scoring (`BackendScorer.ScoreSingle`) | A backend gets a `RequestTypeBonus` when its tags contain the request's `requestType`, and a `ToolCallingBonus` when tagged `tool-calling`. |

In the shipped sample config (`ai-backends.sample.json`), these are the tags in play:

- `code-review`, `tool-calling`, `analysis`, `long-context`, `general-chat`, `fast-response`, `ticket-review`,
  `heavy`.

> **Terminology note:** the same word is used in two ways. A backend can carry a tag like `long-context`
> (a description of its ability), and a request can ask for a *capability* like `long-context` (a hard
> requirement). The proxy matches the request's capability against the backend's tags/fields.

**One important caveat:** tags only affect routing when a request carries routing hints (a `requestType` or
`requiredCapabilities`). A plain request with no routing envelope ignores tags entirely and falls back to
direct alias/model matching and pure scoring.

---

## 5. How the final score is calculated

Once the rules have ordered and filtered the candidates, `Routing/BackendScorer.cs` gives each one a score.
The backend with the highest score wins. The pieces (all constants in `Internal/Constants.cs`):

| Factor | Effect |
|--------|--------|
| `weight` (config) | Baseline preference. Scaled by 0.5. Default weight is 50 if omitted. |
| `latencyProfile` | Bonus: `fast` +30, `medium` +15, `slow` 0, unknown +10. |
| Request-type tag match | +25 if the backend's tags contain the request's `requestType`. |
| Estimated cost | **Penalty.** Higher estimated cost lowers the score (cost × 10). |
| Queue pressure | **Penalty** for deeper queues (−5 per queued request); **bonus** for free slots (+3 per slot). |
| `tool-calling` tag | +10 bonus. |
| GPU (`compute: "gpu"`) | +5 bonus. |

Cost is estimated from your request (`Costing/CostEstimator.cs`): input characters ÷ 4 (approximate tokens)
times `costPer1kInput`, plus estimated output tokens (your `max_completion_tokens`, or a conservative default
of 1024) times `costPer1kOutput`. Free local backends cost 0, so cost only matters when you mix in paid
cloud backends.

---

## 6. What an end user can do to pick the right model

You don't edit `ai-backends.json` to choose a model per request — you shape the request itself. In increasing
order of control:

1. **Do nothing (let it decide).**
   Send a normal chat request. The proxy scores every healthy backend on weight, latency, cost, and load, and
   picks the best fit. This is the "always works" default.

2. **Ask for a specific model by alias.**
   Set `"model"` to a backend alias (e.g. `"llama-local"`, `"mfdoom"`, `"deepseek"`). Because of the direct
   match in step 2 of the router, this bypasses scoring and uses that backend directly. The aliases are the
   `name` values you see in `GET /v1/tags`, so you can discover them from the proxy itself.

3. **Give a soft preference.**
   Send a routing envelope with `preferredModel` set to an alias. That backend jumps to the front of the
   candidate list but is *not* guaranteed if it is unhealthy or filtered out by a required capability.

4. **Route by kind of work.**
   Send `requestType` (`code-review`, `ticket-review`, `chat`, or `analysis`) and let the preference table
   pick the right family of models. This is the most robust way to get "the right tool for the job" without
   pinning a specific model.

5. **Add hard requirements.**
   Send `requiredCapabilities` when the task genuinely needs something:
   - `tool-calling` — the model must be able to call tools/functions.
   - `long-context` — the model's context window must comfortably hold your input.
   - `gpu` — the model must run on GPU-backed compute (excludes `cpu` backends).

6. **Combine them.**
   For example, `requestType: "code-review"` plus `requiredCapabilities: ["tool-calling"]` says "a code-review
   model, and it must support tool calling." The preference list puts `mfdoom` first, then anything tagged
   `code-review`, then the capability filter removes any that can't call tools, and the scorer breaks ties.

---

## 7. Caveats and gotchas

- **Aliases are the user-facing model names.** `GET /v1/tags` returns each backend with `name = alias` and
  `model =` the real model name. Selecting by alias is the reliable way to pin a backend; the underlying
  `model` string is only used as a secondary fallback match.
- **No routing hint = tags are ignored.** Tags only participate when a `requestType` or
  `requiredCapabilities` is present. Otherwise selection is direct alias/model match or pure scoring.
- **`preferredModel` is a hint, not a guarantee.** An unhealthy backend, or one that fails a
  `requiredCapabilities` filter, will not be chosen even if you name it.
- **`requestType` reorders but never filters.** If the preferred backends are all down, a lower-preference
  backend will still be used rather than failing the request.
- **Health checks lag reality.** A backend is only dropped after its health probe fails, so a backend that
  just died may still be selected once before it is marked unhealthy.
- **Scheduler back-pressure can override the winner.** Even after the router picks a backend, the scheduler
  may return `429 Too Many Requests` (global queue full) or `504 Gateway Timeout` (no slots) if everything is
  saturated — see `ModelScheduler`/`ConcurrencyGate` in `Scheduling/`.
- **Paid vs. free is only reflected in scoring.** Cost lowers a backend's score but never filters it out
  unless you also use `requiredCapabilities`. If you never want to spend money, you should not rely on
  scoring alone to avoid a `cloud` backend — tag your requirements explicitly.
- **`fast-response` is a preference, not a capability.** It affects scoring/latency ordering; it cannot be
  used as a hard `requiredCapabilities` filter (only `gpu`, `tool-calling`, and `long-context` are valid hard
  filters).
- **llama.cpp backends only exist if the proxy launched them.** A `llamacpp` backend with no matching
  `details` entry (or whose process failed to start) will fail health checks and effectively never
  be selected. See `Configuration.md` §3.3.
