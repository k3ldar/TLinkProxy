namespace TLinkProxy.Abstractions
{
    public enum ModelKind
    {
        None = 0,

        LlamaCpp = 1 << 1,

        Ollama = 1 << 2,

        OpenAI = 1 << 3,
    }

    public enum ModelComputeProfile
    {
        Cloud,

        Gpu,

        Cpu
    }

    public enum ModelLatencyProfile
    {
        Fast,

        Medium,

        Slow
    }

    public enum ModelMaximumContextSize
    {
        Size2048 = 2048,
        Size4096 = 4096,
        Size8192 = 8192,
        Size16384 = 16384,
        Size32768 = 32768,
        Size65536 = 65536,
        Size131072 = 131072,
        Size262144 = 262144,
        Size524288 = 524288,
        Size1048576 = 1048576
    }

    public enum ToolPolicyAction
    {
        Allow = 0,

        Block = 1,

        Sanitize = 2,
    }

    public enum ToolInvocationOutcome
    {
        Allowed = 0,

        Blocked = 1,

        Sanitized = 2,
    }
}
