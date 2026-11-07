using Microsoft.AspNetCore.Mvc;
using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Base.Events;
using TLinkProxy.Configuration.Models;

namespace TLinkProxy.Configuration.Controllers
{
    [Route(ConfigurationController.ControllerName)]
    public sealed class ConfigurationController : Controller
    {
        private const string ControllerName = "Configuration";
        private readonly IAiBackendRegistry _registry;
        private readonly IAiModelStore _modelService;
        private readonly ISettingsService _settingsService;

        public ConfigurationController(IAiBackendRegistry registry, IAiModelStore modelService, ISettingsService settingsService)
        {
            ArgumentNullException.ThrowIfNull(registry);
            ArgumentNullException.ThrowIfNull(modelService);
            ArgumentNullException.ThrowIfNull(settingsService);
            _registry = registry;
            _modelService = modelService;
            _settingsService = settingsService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("ModelConfiguration")]
        public IActionResult ModelConfiguration()
        {
            IReadOnlyList<IAiModelConfig> models = _registry.GetAllBackends();
            return View(models);
        }

        [HttpGet("Edit/{alias?}")]
        public IActionResult Edit(string alias)
        {
            return View(BuildViewModel(alias));
        }

        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(AiModelEditViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            IAiModelConfig backend = viewModel.Backend ?? new AiModelConfig();

            backend.Tags = ParseTags(viewModel.TagsCsv);
            backend.AdditionalHeaders = ParseAdditionalHeaders(viewModel.AdditionalHeadersCsv);

            // Details are only meaningful for llama.cpp backends; strip them otherwise.
            if (backend.Kind != ModelKind.LlamaCpp)
            {
                backend.Details = null;
            }

            if (String.IsNullOrWhiteSpace(backend.Alias))
            {
                ModelState.AddModelError(nameof(backend.Alias), Constants.ModelAliasRequiredMessage);
            }

            if (backend.Kind == ModelKind.None)
            {
                ModelState.AddModelError(nameof(backend.Kind), Constants.ModelKindRequiredMessage);
            }

            if (!ModelState.IsValid)
            {
                backend.Details ??= new LlamaCppConfig();
                viewModel.InstalledModels = GetInstalledModels();
                return View(viewModel);
            }

            if (backend.Kind == ModelKind.LlamaCpp && !backend.Model.EndsWith(Constants.GgufFileExtension))
                backend.Model = $"{backend.Model}{Constants.GgufFileExtension}";

            if (!String.IsNullOrWhiteSpace(viewModel.OriginalAlias) &&
                !String.Equals(viewModel.OriginalAlias, backend.Alias, StringComparison.OrdinalIgnoreCase))
            {
                IAiModel original = _modelService.GetModel(viewModel.OriginalAlias);

                if (original != null)
                {
                    _modelService.DeleteModel(original);
                }
            }

            _modelService.AddModel(AiModelMapper.ToModel(backend));
            _registry.EventBus?.Publish(new ModelsReloadedEvent(_registry.GetAllBackends()));

            return RedirectToAction(nameof(ModelConfiguration), ControllerName);
        }

        [HttpGet("VisualStudio")]
        public IActionResult VisualStudio()
        {
            return View(BuildVisualStudioViewModel(LoadVisualStudioSettings()));
        }

        [HttpPost("VisualStudio")]
        [ValidateAntiForgeryToken]
        public IActionResult VisualStudio(VisualStudioSettingsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            VisualStudioSettings settings = BuildVisualStudioSettings(viewModel);
            _settingsService.UpdateSetting(Constants.VisualStudioSettingsKey, settings.ToJson());
            TempData[Constants.VisualStudioSavedKey] = true;

            return RedirectToAction(nameof(VisualStudio), ControllerName);
        }

        [HttpGet("Diagnostics")]
        public IActionResult Diagnostics()
        {
            return View(new DiagnosticsSettingsViewModel { IntervalSeconds = LoadDiagnosticsInterval(), HistorySamples = LoadDiagnosticsHistorySamples() });
        }

        [HttpPost("Diagnostics")]
        [ValidateAntiForgeryToken]
        public IActionResult Diagnostics(DiagnosticsSettingsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            _settingsService.UpdateSetting(Constants.DiagnosticsIntervalSecondsKey, viewModel.IntervalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
            _settingsService.UpdateSetting(Constants.DiagnosticsHistorySamplesKey, viewModel.HistorySamples.ToString(System.Globalization.CultureInfo.InvariantCulture));
            TempData[Constants.DiagnosticsSavedKey] = true;

            return RedirectToAction(nameof(Diagnostics), ControllerName);
        }

        private int LoadDiagnosticsInterval()
        {
            string value = _settingsService.GetSetting(Constants.DiagnosticsIntervalSecondsKey);

            return Int32.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int seconds) && seconds > 0
                ? seconds
                : Constants.DefaultDiagnosticsIntervalSeconds;
        }

        private int LoadDiagnosticsHistorySamples()
        {
            string value = _settingsService.GetSetting(Constants.DiagnosticsHistorySamplesKey);

            return Int32.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int samples)
                ? Math.Clamp(samples, Constants.MinDiagnosticsHistorySamples, Constants.MaxDiagnosticsHistorySamples)
                : Constants.DefaultDiagnosticsHistorySamples;
        }

        private VisualStudioSettings LoadVisualStudioSettings()
        {
            string json = _settingsService.GetSetting(Constants.VisualStudioSettingsKey);
            return VisualStudioSettings.FromJson(json);
        }

        private static VisualStudioSettingsViewModel BuildVisualStudioViewModel(VisualStudioSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            return new VisualStudioSettingsViewModel
            {
                Enabled = settings.Enabled,
                TrimLineEnds = settings.TrimLineEnds,
                CollapseBlankLines = settings.CollapseBlankLines,
                RemoveBlocks = VisualStudioSettings.JoinLines(settings.RemoveBlocks),
                RemoveTextPatterns = VisualStudioSettings.JoinLines(settings.RemoveTextPatterns),
                RemoveRegexPatterns = VisualStudioSettings.JoinLines(settings.RemoveRegexPatterns),
                CopilotInstructions = VisualStudioSettings.JoinLines(settings.CopilotInstructions)
            };
        }

        private static VisualStudioSettings BuildVisualStudioSettings(VisualStudioSettingsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            return new VisualStudioSettings
            {
                Enabled = viewModel.Enabled,
                TrimLineEnds = viewModel.TrimLineEnds,
                CollapseBlankLines = viewModel.CollapseBlankLines,
                RemoveBlocks = VisualStudioSettings.SplitLines(viewModel.RemoveBlocks),
                RemoveTextPatterns = VisualStudioSettings.SplitLines(viewModel.RemoveTextPatterns),
                RemoveRegexPatterns = VisualStudioSettings.SplitLines(viewModel.RemoveRegexPatterns),
                CopilotInstructions = VisualStudioSettings.SplitLines(viewModel.CopilotInstructions)
            };
        }

        private AiModelEditViewModel BuildViewModel(string alias)
        {
            IAiModelConfig backend = String.IsNullOrWhiteSpace(alias)
                ? new AiModelConfig()
                : _registry.GetBackendByAlias(alias);

            backend ??= new AiModelConfig();

            backend.Details ??= new LlamaCppConfig();

            AiModelEditViewModel viewModel = new()
            {
                Backend = backend,
                OriginalAlias = String.IsNullOrWhiteSpace(alias) ? String.Empty : alias,
                TagsCsv = backend.Tags == null ? String.Empty : String.Join(Constants.ListSeparator, backend.Tags),
                AdditionalHeadersCsv = JoinAdditionalHeaders(backend.AdditionalHeaders),
                InstalledModels = GetInstalledModels()
            };

            return viewModel;
        }

        private static string[] ParseTags(string tagsCsv)
        {
            if (String.IsNullOrWhiteSpace(tagsCsv))
            {
                return [];
            }

            return [.. tagsCsv.Split(Constants.TagSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase)];
        }

        private static Dictionary<string, string> ParseAdditionalHeaders(string text)
        {
            Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);

            if (String.IsNullOrWhiteSpace(text))
            {
                return headers;
            }

            foreach (string rawLine in text.Split([Constants.CrlfNewLine, Constants.NewLine], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int separator = rawLine.IndexOf(Constants.CharColon);

                if (separator <= 0)
                {
                    continue;
                }

                string name = rawLine[..separator].Trim();
                string value = rawLine[(separator + 1)..].Trim();

                if (!String.IsNullOrWhiteSpace(name))
                {
                    headers[name] = value;
                }
            }

            return headers;
        }

        private static string JoinAdditionalHeaders(IDictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
            {
                return String.Empty;
            }

            return String.Join(Environment.NewLine, headers.Select(kv => $"{kv.Key}: {kv.Value}"));
        }

        private string[] GetInstalledModels()
        {
            string modelPath = _registry.Current?.ModelPath;

            if (String.IsNullOrWhiteSpace(modelPath) || !Directory.Exists(modelPath))
            {
                return [];
            }

            return [.. Directory.GetFiles(modelPath, Constants.WildcardFilePattern + Constants.GgufFileExtension, SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];
        }
    }
}
