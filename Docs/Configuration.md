# TLinkProxy Configuration Guide

This guide explains how configuration works inside TLinkProxy (the "Ollama Proxy"), which files own which
responsibilities, and how the configuration flows through the app. It is written so that a developer who is new to
the codebase can make a real configuration change — for example, *"ensure llama-cpp instances are not started on a
specific port"* — and know exactly which files to touch and how to test the change.

---

## 1. Two configuration layers

TLinkProxy uses two, unrelated configuration mechanisms. Do not mix them up.

| Layer | File | Loaded by | Purpose |
|-------|------|-----------|---------|
| **ASP.NET app settings** | `TLinkProxy/appsettings.json` (and `appsettings.Development.json`) | `WebApplication.CreateBuilder` → `builder.Configuration` | Standard .NET config: logging levels, `ProxySessionLogger` options, `AllowedHosts`. |
| **AI backends file** | `ai-backends.json` (see §3) | The app's own `ConfigFile` / `AiBackendRegistry` classes | Which AI backends exist, their endpoints/tags/costs, and the llama-cpp processes the proxy should launch and manage. |

The rest of this document is about the **AI backends file**, because that is the layer you change for anything
related to backends, routing, or llama-cpp process management.

---

## 2. Where the AI backends file lives

The path is resolved by `TLinkProxy/Internal/ConfigFile.cs` in `ResolveConfigFilePath()`:

1. If the environment variable `AI_BACKENDS_CONFIG` is set **and the file exists**, that path is used.
2. Otherwise the default is used: `%CommonApplicationData%\TLinkProxy\ai-backends.json`
   (on Windows this is `C:\ProgramData\TLinkProxy\ai-backends.json`). The directory is created automatically.

`TLinkProxy/ai-backends.sample.json` is only a template/sample. It is **not** copied to the live location
automatically — it exists so people can see the expected shape.

Related logs live under `C:\ProgramData\TLinkProxy\logs\<alias>.log` (see §6).

---

## 3. The shape of `ai-backends.json`

The file is deserialized into `AiSettings` (`TLinkProxy/Configuration/AiSettings.cs`):

```json
{
  "models": "C:\\tools\\llama-cpp\\models",
  "llamacpp": "C:\\tools\\llama-cpp\\llama-server.exe",
  "backends": [
    {
      "alias": "mfdoom",
      "kind": "llamacpp",
      "endpoint": "http://127.0.0.1:8081",
      "model": "MFDoom-deepseek-coder-v2-tool-calling-16b",
      "details": {
        "host": "127.0.0.1",
        "port": 8081
      }
    }
  ],
  "globalQueueCapacity": 256
}
```

`models` and `llamacpp` are optional shared fallbacks; `details` is a nested `LlamaCppConfig` object on each
backend (see §3.2 and §3.3).

### 3.1 Backend entry (`ModelConfig`)

`TLinkProxy/Configuration/ModelConfig.cs`

| JSON property | Type | Meaning |
|---------------|------|---------|
| `alias` | string | Unique, case-insensitive identifier (also exposed as a model name via `/v1/tags`). |
| `kind` | string | `"ollama"`, `"llamacpp"`, or `"openai"`. Unknown/empty defaults to `ollama`. |
| `endpoint` | string | Base URL of the backend. |
| `model` | string | Model name to use on that backend. For `llamacpp` it is also the `.gguf` basename used by the model-path fallback. |
| `tags` | string[] | Capabilities used by routing (e.g. `code-review`, `tool-calling`, `long-context`, `gpu`). |
| `compute` | string | `gpu`, `cpu`, or `cloud` (used for scoring). |
| `latencyProfile` | string | `fast`, `medium`, or `slow` (used for scoring). |
| `costPer1kInput` / `costPer1kOutput` | double | Per-1k-token cost (used for scoring). |
| `maxContext` | int | Maximum context length. |
| `concurrencyLimit` | int | Max concurrent requests. Defaults to `Constants.DefaultConcurrencyLimit` (1). |
| `weight` | int | Routing preference weight. Defaults to `Constants.DefaultWeight` (50). |
| `apiKeyEnvVar` | string | Name of an environment variable holding the API key (e.g. `DEEPSEEK_API_KEY`). |
| `details` | LlamaCppConfig | Kind-specific settings. Populated for `llamacpp` backends the proxy should launch; omitted/null for others. |
| `allowQueue` | bool | Whether requests may queue when the backend is saturated. Default `true`. |
| `maxQueueDepth` | int | Max requests in this backend's queue. Defaults to `Constants.DefaultQueueDepth` (64). |
| `enabled` | bool | `false` disables the backend (and prevents its llama-cpp process from starting). Default `true`. |
| `healthProbeSeconds` | int | Timeout for a health probe. Defaults to `Constants.DefaultHealthProbeSeconds` (5). |

### 3.2 llama-cpp details (`LlamaCppConfig`)

`TLinkProxy/Configuration/LlamaCppConfig.cs`

| JSON property | Type | Meaning |
|---------------|------|---------|
| `executable` | string | Path to `llama-server.exe` (or equivalent). Optional — falls back to root `llamacpp`. |
| `modelPath` | string | Path to the `.gguf` model. Optional — falls back to `{models}\{backend.model}.gguf`. |
| `host` | string | Bind address passed as `--host`. |
| `port` | int | Bind port passed as `--port`. **This is the port a llama-cpp instance listens on.** |
| `gpuLayers` | int | Passed as `--n-gpu-layers`. |
| `threads` | int | Passed as `--threads`. |
| `batchSize` | int | Passed as `--batch-size`. |
| `contextSize` | int | Passed as `--ctx-size`. |
| `autoRestart` | bool | Restart the process (with backoff) if it exits. |
| `healthCheckSeconds` | int | Overrides the health-check interval for this instance. |
| `timeoutSeconds` | int | Startup timeout for this instance. |

### 3.3 `details` and the root `models` / `llamacpp` shortcuts

`details` is a **nested** `LlamaCppConfig` object on a backend; it is only meaningful when `kind` is `"llamacpp"`:

- A **`ModelConfig`** entry is the *logical* definition of a model: how requests are routed to it (`endpoint`, `tags`,
  `weight`), how it is scored (`compute`, `latencyProfile`, `costPer1k*`), how health is judged
  (`healthProbeSeconds`), and whether it is available at all (`enabled`, `concurrencyLimit`, `allowQueue`,
  `maxQueueDepth`).
- Its **`details`** object is the *physical* definition of the `llama-server` process the proxy launches and
  supervises: which executable to run, which model file to load, and the arguments (`host`, `port`, `gpuLayers`, …).

The root `models` and `llamacpp` properties are shared fallbacks so each backend's `details` can stay short:

- `details.executable` falls back to root `llamacpp`.
- `details.modelPath` falls back to `{models}\{backend.model}.gguf`.

Rules:

1. Only `kind: "llamacpp"` backends use `details`. `ollama` and `openai` backends talk to servers the proxy does
   **not** manage, so they should omit `details`.
2. `details.host` / `details.port` define where the launched `llama-server` listens, and the backend's `endpoint` is
   where the proxy forwards requests — the two should point at the same place (e.g. `endpoint:
   "http://127.0.0.1:8080"` with `host: "127.0.0.1"` and `port: 8080`).
3. `enabled: false` on the **backend** also prevents its process from starting (`LlamaCppProcessManager` skips
   disabled backends and marks them unhealthy).

> **Key rule of thumb.** Whether a backend needs `details` is determined by `kind`, not by local vs. remote:
>
> - `kind: "ollama"` or `"openai"` — the backend points at a service the proxy does **not** manage. A backend entry
>   alone is fully functional as long as its `endpoint` is reachable. Do **not** add `details`.
> - `kind: "llamacpp"` — the backend points at a `llama-server` the proxy is expected to launch itself. It needs
>   `details` (or enough root `models`/`llamacpp` fallback for the manager to resolve them); without them no process
>   starts, the `endpoint` has no listener, health checks fail, and routing avoids the backend. (The one exception:
>   something outside TLinkProxy is already running a `llama-server` on that `endpoint` — an unusual setup, since the
>   point of a `llamacpp` backend is for the proxy to own the process.)

---

## 4. Key files and responsibilities

| File | Responsibility |
|------|----------------|
| `TLinkProxy/Internal/ConfigFile.cs` | Resolves the file path (§2) and reads/writes `AiSettings` as JSON. Implements `IConfigFile`. |
| `TLinkProxy/Abstractions/IConfigFile.cs` | Contract: `GetConfigFile()`, `GetConfig()`, `SaveConfig(...)`. |
| `TLinkProxy/Configuration/AiSettings.cs` | Root model: `Backends`, `Models`, `Llamacpp`, `GlobalQueueCapacity`. |
| `TLinkProxy/Configuration/ModelConfig.cs` | One backend entry (routing/health/cost) plus its nested `details`. |
| `TLinkProxy/Configuration/LlamaCppConfig.cs` | One managed llama-cpp process (`details`: executable, port, etc.). |
| `TLinkProxy/Configuration/AiBackendRegistry.cs` | Singleton cache of `AiSettings`, **hot reload** via `FileSystemWatcher`, and per-alias health state. |
| `TLinkProxy/Configuration/RoutingEnvelope.cs` | Per-request routing hints. **Not** part of `ai-backends.json` — it is extracted from the incoming request payload. |
| `TLinkProxy/Processes/LlamaCppProcessManager.cs` | Hosted service that starts/monitors/restarts/kills the configured llama-cpp processes. |
| `TLinkProxy/Processes/BackendHealthMonitor.cs` | Hosted service that polls each backend's health endpoint. |
| `TLinkProxy/Routing/BackendRouter.cs` | Chooses a backend using config + health + routing envelope. |
| `TLinkProxy/Routing/RuleEngine.cs` | Orders/filters candidate backends by request type and capabilities. |
| `TLinkProxy/Controllers/HomeController.cs` | Proxy endpoints; exposes `/v1/tags` built from `GetConfig().Backends`. |
| `TLinkProxy/Internal/Constants.cs` | File names, environment-variable names, default values, and string literals. |
| `TLinkProxy/Program.cs` | Registers all of the above in dependency injection and wires up the hosted services. |
| `TLinkProxy/ai-backends.sample.json` | Example config file. |

---

## 5. How configuration flows through the app

1. **Registration** (`Program.cs`) registers these as singletons/hosted services:

   ```csharp
   builder.Services.AddSingleton<IConfigFile, ConfigFile>();
   builder.Services.AddSingleton<AiBackendRegistry>();
   builder.Services.AddHostedService<Processes.BackendHealthMonitor>();
   builder.Services.AddHostedService<Processes.LlamaCppProcessManager>();
   ```

2. **Load** — `AiBackendRegistry`'s constructor asks `IConfigFile.GetConfigFile()` for the path, loads the file
   immediately, then starts a `FileSystemWatcher`. On any change it debounces
   (`Constants.ConfigReloadDebounceMs`, 250 ms) and reloads into the `Current` property. A missing or invalid file
   simply leaves the previous/empty config in place.

3. **Consumption** — components read `_registry.Current` at different times:

   - `BackendRouter` reads it **per request**.
   - `BackendHealthMonitor` reads it **every loop iteration**.
   - `LlamaCppProcessManager` reads it **once, at startup** (`StartAsync`).

   > **Important gotcha:** editing `backends` (including `details`) at runtime hot-reloads routing and health, but
   > **llama-cpp processes are only started/stopped when the app starts/stops.** To change which llama-cpp
   > processes run, edit the file **and restart the service/app**.

4. **Write** — `ConfigFile.SaveConfig(...)` serializes `AiSettings` back to JSON (indented, nulls omitted).
   `AiBackendRegistry` itself never writes.

---

## 6. llama-cpp process management in depth

`TLinkProxy/Processes/LlamaCppProcessManager.cs` (an `IHostedService`) is the class you will most likely change for
the worked example. Its flow:

- `StartAsync` iterates `_registry.Current.Backends` where `kind` is `"llamacpp"`. For each it resolves `details`
  (applying the root `models`/`llamacpp` fallbacks); if the backend has `enabled: false`, it is skipped and marked
  unhealthy. Otherwise it calls `StartInstance`.
- `StartInstance` records a `CancellationTokenSource` per alias (ignoring duplicates) and runs `ManageInstanceAsync`
  on a background task.
- `ManageInstanceAsync` loops forever until cancelled:
  1. Verifies the executable exists, else logs + marks unhealthy and returns.
  2. Builds the process via `BuildStartInfo` and starts it.
  3. Pipes stdout/stderr to `C:\ProgramData\TLinkProxy\logs\<alias>.log` and the session logger.
  4. Marks the alias healthy, waits for exit, marks it unhealthy.
  5. If `autoRestart: true` and not cancelled, restarts after exponential backoff.
- `BuildStartInfo` translates `details` fields into `llama-server` arguments. **The port comes from here:**

  ```csharp
  if (details.Port > 0)
  {
      args.Add($"--port {details.Port}");
  }
  ```

  Full argument list built: `--model`, `--host`, `--port`, `--n-gpu-layers`, `--threads`, `--batch-size`, `--ctx-size`.
- `StopAsync` cancels all tokens and kills each process tree.

---

## 7. How to make a configuration change (end-to-end)

Any change that adds or changes configuration behaviour follows the same path:

1. **Model** — add/rename a property in the matching model class (`ModelConfig` / `LlamaCppConfig` /
   `AiSettings`) and mark it with `[JsonPropertyName("camelCaseName")]`.
2. **Sample** — update `ai-backends.sample.json` so people can see the new field.
3. **Logic** — update the consumer (`LlamaCppProcessManager`, `BackendHealthMonitor`, `BackendRouter`, etc.).
4. **Constants** — put any new string literals or magic numbers in `Internal/Constants.cs`.
5. **Tests** — add/update unit tests (see §9).

Adding a field is backward-compatible: unknown JSON fields are ignored on read, and nulls are omitted on write
(`PropertyNameCaseInsensitive = true`, `WhenWritingNull`).

---

## 8. Worked example: prevent a llama-cpp instance from starting on an occupied port

Goal: *"ensure llama-cpp instances are not running for a specific port."* Concretely, we want the proxy to refuse to
launch a llama-cpp instance when something else is already listening on its configured `port`.

### Step 1 — Decide where to put the guard

The port is `LlamaCppConfig.Port` (inside a backend's `details`), consumed in `LlamaCppProcessManager`. The guard
belongs in `ManageInstanceAsync`, **after** the executable check and **before** `proc.Start()`:

```csharp
// inside ManageInstanceAsync, after the executable-exists check
if (IsPortInUse(details.Port))
{
    await _logger.LogAsync(alias, $"[LlamaMgr] Port {details.Port} is already in use; not starting {alias}").ConfigureAwait(false);
    _registry.SetHealth(alias, false);
    return;
}
```

Returning here exits the manage loop, so the manager does **not** restart-loop against a busy port.

### Step 2 — Add the port check helper

```csharp
using System.Net;
using System.Net.NetworkInformation;

private static bool IsPortInUse(int port)
{
    IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
    IPEndPoint[] listeners = properties.GetActiveTcpListeners();
    return listeners.Any(endpoint => endpoint.Port == port);
}
```

### Step 3 (optional but recommended) — make it opt-in via config

If you only want this behaviour for certain instances, add a field to `LlamaCppConfig`:

```csharp
[JsonPropertyName("skipIfPortInUse")]
public bool SkipIfPortInUse { get; set; }
```

Then gate the check on it in `ManageInstanceAsync`:

```csharp
if (details.SkipIfPortInUse && IsPortInUse(details.Port))
{
    // log + mark unhealthy + return, as above
}
```

Add `"skipIfPortInUse": true` to the relevant entry in `ai-backends.sample.json`.

### Step 4 — Update constants (if you introduced literals)

Per the codebase conventions, put any new literal (e.g. a default value or the JSON property name if reused) into
`Internal/Constants.cs` rather than inlining it.

### Step 5 — Add tests

See `TLinkProxy.Tests/Processes/LlamaCppProcessManagerTests.cs` for existing patterns. Follow the naming convention
`MethodName_StateUnderTest_ExpectedBehavior` and the AAA structure with the system under test named `sut`. For example:

```csharp
[TestMethod]
public void IsPortInUse_OccupiedPort_ReturnsTrue()
{
    // Bind a TcpListener to an ephemeral port, then assert IsPortInUse(port) is true.
}

[TestMethod]
public void IsPortInUse_FreePort_ReturnsFalse()
{
    // Pick a port with no listener and assert IsPortInUse(port) is false.
}
```

Because `IsPortInUse` and `BuildStartInfo` are private, existing tests call private members via reflection
(`GetMethod(...).Invoke(...)`), or you can exercise the behaviour through `StartAsync` with a real short-lived
executable as the existing tests do.

---

## 9. Testing configuration changes

- Test files mirror production namespaces under `TLinkProxy.Tests` (e.g. `TLinkProxy.Tests.Configuration`).
- Test classes are `sealed` and marked `[ExcludeFromCodeCoverage]`.
- Method naming: `MethodName_StateUnderTest_ExpectedBehavior` (e.g. `Deserialize_FullJson_MapsAllProperties`).
- Use Arrange-Act-Assert, but do **not** add `// Arrange` / `// Act` / `// Assert` comments — the structure should be
  obvious. Name the system under test `sut`.
- Cover happy path, boundaries, nulls, empty collections, invalid inputs, and exception paths.
- Use UTC `DateTime` values.
- Reference tests that are already there:
  - `TLinkProxy.Tests/Configuration/LlamaCppConfigTests.cs` — JSON (de)serialization of the nested `details` entry.
  - `TLinkProxy.Tests/Configuration/BackendConfigTests.cs` — JSON (de)serialization of a backend entry.
  - `TLinkProxy.Tests/Processes/LlamaCppProcessManagerTests.cs` — process-manager behaviour (start/restart/disable).

Run them from the repo root:

```powershell
dotnet test TLinkProxy.Tests
```

---

## 10. Conventions and gotchas

- **One class per file**; classes are `sealed` unless designed for inheritance.
- **Explicit types**, not `var` (e.g. `List<string> values = [];`).
- **String literals and magic numbers live in `Constants.cs`** with descriptive names.
- Use C# aliases (`string`, `int`, `bool`) for declarations; use CLR names (`String`, `Int32`) only for static members.
- **Nullable reference types are not enabled**, so reference properties default to null — guard with
  `ArgumentNullException.ThrowIfNull(...)` where appropriate. Value types are **non-nullable** and use
  property-initializer defaults (e.g. `Enabled = true`, `ConcurrencyLimit = 1`).
- JSON property names are camelCase and matched **case-insensitively** on read.
- The config is **hot-reloaded** for routing/health, but **llama-cpp processes are managed only at startup/stop**.
- Do not introduce `CC0009` or `MSTEST0037` warnings/errors when building.