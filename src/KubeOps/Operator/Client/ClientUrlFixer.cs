using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KubeOps.Operator.Client
{
    internal class ClientUrlFixer : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Segments.Count(s => s == "/") > 1)
            {
                // This request uri contains "empty" segments. (i.e. https://.../apis//v1/...)
                // This means, this is a default api group (/api/v1)
                var builder = new UriBuilder(request.RequestUri);
                builder.Path = builder.Path.Replace("//", "/").Replace("apis", "api");
                request.RequestUri = builder.Uri;
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
