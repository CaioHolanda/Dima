using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net;

namespace Dima.Web.Security
{
    public class CookieHandler:DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            request.Headers.Add("X-Requested-With", ["XMLHttpRequest"]);

            var response = await base.SendAsync(request, cancellationToken);

            // Login has its own messages for invalid credentials and unconfirmed email.
            var isLogin = request.RequestUri?.AbsolutePath.EndsWith(
                "/v1/identity/login-user", StringComparison.OrdinalIgnoreCase) == true;

            if (response.StatusCode == HttpStatusCode.Unauthorized && !isLogin)
            {
                response.Dispose();
                throw new HttpRequestException(
                    "[401] Sua sessão não está mais válida. Entre novamente. Se não conseguir acessar, entre em contato com o administrador para verificar se sua conta está ativa.",
                    null,
                    HttpStatusCode.Unauthorized);
            }

            return response;
        }
    }
}
