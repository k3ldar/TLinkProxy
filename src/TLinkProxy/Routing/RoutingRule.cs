namespace TLinkProxy.Routing
{
    /// <summary>
    /// A single routing rule: maps a <see cref="RequestType"/> to an ordered set of preferred
    /// backend aliases or tags. Preference entries are matched against a backend's alias first,
    /// then against its tags.
    /// </summary>
    public sealed class RoutingRule
    {
        public RoutingRule(string requestType, string[] preferences)
        {
            RequestType = requestType;
            Preferences = preferences ?? [];
        }

        public string RequestType { get; }

        public string[] Preferences { get; }
    }
}
