using System.ComponentModel.DataAnnotations;

namespace TLinkProxy.Configuration.Models
{
    public sealed class DiagnosticsSettingsViewModel
    {
        [Display(Name = "Sampling interval (seconds)")]
        [Range(1, 3600, ErrorMessage = "The sampling interval must be between 1 and 3600 seconds.")]
        public int IntervalSeconds { get; set; }
    }
}
