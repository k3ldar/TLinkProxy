using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{

    public class Properties
    {
        // Consolidated schema-like properties into a reusable SchemaField type
        [JsonPropertyName("tenant")]
        public SchemaField Tenant { get; set; }

        [JsonPropertyName("authmethod")]
        public SchemaField AuthMethod { get; set; }

        [JsonPropertyName("retrydelay")]
        public SchemaField RetryDelay { get; set; }

        [JsonPropertyName("retrymaxdelay")]
        public SchemaField RetryMaxDelay { get; set; }

        [JsonPropertyName("retrymaxretries")]
        public SchemaField RetryMaxRetries { get; set; }

        [JsonPropertyName("retrymode")]
        public SchemaField RetryMode { get; set; }

        [JsonPropertyName("retrynetworktimeout")]
        public SchemaField RetryNetworkTimeout { get; set; }

        [JsonPropertyName("intent")]
        public SchemaField Intent { get; set; }

        [JsonPropertyName("clitype")]
        public SchemaField CliType { get; set; }

        [JsonPropertyName("reason")]
        public SchemaField Reason { get; set; }

        [JsonPropertyName("solutionDirectory")]
        public SchemaField SolutionDirectory { get; set; }

        [JsonPropertyName("packageName")]
        public SchemaField PackageName { get; set; }

        [JsonPropertyName("packageVersion")]
        public SchemaField PackageVersion { get; set; }

        [JsonPropertyName("projectPaths")]
        public SchemaField ProjectPaths { get; set; }

        [JsonPropertyName("includeVulnerable")]
        public SchemaField IncludeVulnerable { get; set; }

        [JsonPropertyName("includePrerelease")]
        public SchemaField IncludePrerelease { get; set; }

        [JsonPropertyName("prompt")]
        public SchemaField Prompt { get; set; }

        [JsonPropertyName("command")]
        public SchemaField Command { get; set; }

        [JsonPropertyName("parameters")]
        public SchemaField ParametersField { get; set; }

        [JsonPropertyName("learn")]
        public SchemaField Learn { get; set; }

        [JsonPropertyName("searchQueries")]
        public SchemaField SearchQueries { get; set; }

        [JsonPropertyName("filePaths")]
        public SchemaField FilePaths { get; set; }

        [JsonPropertyName("queries")]
        public SchemaField Queries { get; set; }

        [JsonPropertyName("maxResults")]
        public SchemaField MaxResults { get; set; }

        [JsonPropertyName("projectPath")]
        public SchemaField ProjectPath { get; set; }

        [JsonPropertyName("filePath")]
        public SchemaField FilePath { get; set; }

        [JsonPropertyName("content")]
        public SchemaField Content { get; set; }

        [JsonPropertyName("summary")]
        public SchemaField Summary { get; set; }

        [JsonPropertyName("background")]
        public SchemaField Background { get; set; }

        [JsonPropertyName("terminal_id")]
        public SchemaField TerminalId { get; set; }

        [JsonPropertyName("headLines")]
        public SchemaField HeadLines { get; set; }

        [JsonPropertyName("tailLines")]
        public SchemaField TailLines { get; set; }

        [JsonPropertyName("stop")]
        public SchemaField Stop { get; set; }

        [JsonPropertyName("waitMs")]
        public SchemaField WaitMs { get; set; }

        [JsonPropertyName("planMarkdown")]
        public SchemaField PlanMarkdown { get; set; }

        [JsonPropertyName("stepId")]
        public SchemaField StepId { get; set; }

        [JsonPropertyName("status")]
        public SchemaField Status { get; set; }

        [JsonPropertyName("message")]
        public SchemaField MessageField { get; set; }

        [JsonPropertyName("autoAdvance")]
        public SchemaField AutoAdvance { get; set; }

        [JsonPropertyName("observation")]
        public SchemaField Observation { get; set; }

        [JsonPropertyName("planTitle")]
        public SchemaField PlanTitle { get; set; }

        [JsonPropertyName("questions")]
        public SchemaField Questions { get; set; }

        [JsonPropertyName("memory")]
        public SchemaField Memory { get; set; }

        [JsonPropertyName("confidence")]
        public SchemaField Confidence { get; set; }

        [JsonPropertyName("paneId")]
        public SchemaField PaneId { get; set; }

        [JsonPropertyName("filterTypes")]
        public SchemaField FilterTypes { get; set; }

        [JsonPropertyName("filterValues")]
        public SchemaField FilterValues { get; set; }

        [JsonPropertyName("navigationType")]
        public SchemaField NavigationType { get; set; }

        [JsonPropertyName("symbolName")]
        public SchemaField SymbolName { get; set; }

        [JsonPropertyName("lineText")]
        public SchemaField LineText { get; set; }

        [JsonPropertyName("urls")]
        public SchemaField Urls { get; set; }

        [JsonPropertyName("query")]
        public SchemaField Query { get; set; }

        [JsonPropertyName("isRegexp")]
        public SchemaField IsRegexp { get; set; }

        [JsonPropertyName("includePattern")]
        public SchemaField IncludePattern { get; set; }

        [JsonPropertyName("filename")]
        public SchemaField Filename { get; set; }

        [JsonPropertyName("startLine")]
        public SchemaField StartLine { get; set; }

        [JsonPropertyName("endLine")]
        public SchemaField EndLine { get; set; }

        [JsonPropertyName("includeLineNumbers")]
        public SchemaField IncludeLineNumbers { get; set; }

        [JsonPropertyName("oldString")]
        public SchemaField OldString { get; set; }

        [JsonPropertyName("newString")]
        public SchemaField NewString { get; set; }

        [JsonPropertyName("replacements")]
        public SchemaField Replacements { get; set; }

        [JsonPropertyName("explanation")]
        public SchemaField Explanation { get; set; }
    }

}
