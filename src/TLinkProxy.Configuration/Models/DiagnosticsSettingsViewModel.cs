using System.ComponentModel.DataAnnotations;

namespace TLinkProxy.Configuration.Models
{
    public sealed class DiagnosticsSettingsViewModel
    {
        [Display(Name = "Sampling interval (seconds)")]
        [Range(1, 3600, ErrorMessage = "The sampling interval must be between 1 and 3600 seconds.")]
        public int IntervalSeconds { get; set; }

        [Display(Name = "Memory history (samples)")]
        [Range(2, 720, ErrorMessage = "The memory history must be between 2 and 720 samples.")]
        public int HistorySamples { get; set; }
    }
}
