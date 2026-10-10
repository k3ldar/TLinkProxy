namespace TLinkProxy.Internal
{
    internal sealed class ProxyContext
    {
        internal ProxyContext(HttpMethod httpMethod, string pathAndQuery, string baseUrl,
            string targetUrl)
        {
            HttpMethod = httpMethod;
            PathAndQuery = pathAndQuery;
            BaseUrl = baseUrl;
            TargetUrl = targetUrl;
        }

        public HttpMethod HttpMethod { get; }

        public string PathAndQuery { get; }

        public string BaseUrl { get; }

        public string TargetUrl { get; }
    }
}
