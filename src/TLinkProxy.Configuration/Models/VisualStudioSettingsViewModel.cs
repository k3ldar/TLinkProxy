using System.ComponentModel.DataAnnotations;

namespace TLinkProxy.Configuration.Models
{
    public sealed class VisualStudioSettingsViewModel
    {
        [Display(Name = "Enable prompt processing")]
        public bool Enabled { get; set; }

        [Display(Name = "Trim trailing whitespace on each line")]
        public bool TrimLineEnds { get; set; }

        [Display(Name = "Collapse blank lines")]
        public bool CollapseBlankLines { get; set; }

        [Display(Name = "Blocks to remove")]
        public string RemoveBlocks { get; set; }

        [Display(Name = "Text patterns to remove")]
        public string RemoveTextPatterns { get; set; }

        [Display(Name = "Regex patterns to remove")]
        public string RemoveRegexPatterns { get; set; }

        [Display(Name = "Copilot instructions")]
        public string CopilotInstructions { get; set; }
    }
}
