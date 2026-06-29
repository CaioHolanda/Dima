using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Dima.Web.Security
{
    public class CookieHandler:DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            request.Headers.Add("X-Requested-Witdh", ["XMLHttpRequest"]);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
