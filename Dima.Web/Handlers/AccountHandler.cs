using Dima.Core.Handlers;
using Dima.Core.Requests.Account;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class AccountHandler(IHttpClientFactory httpClientFactory) : IAccountHandler
    {
        private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);
        public async Task<Response<string>> LoginAsync(LoginRequest request)
        {
           var result = await _client.PostAsJsonAsync("v1/identity/login?useCookies=true", request);
            return result.IsSuccessStatusCode
                ? new Response<string>("Login realizado com sucesso!", 200, "Login realizado com sucesso!")
                : new Response<string>(null, 400, "[E016] Login not possible.");
        }

        public async Task LogoutAsync()
        {
            using var response =
                await _client.PostAsync("v1/identity/logout", null);

            response.EnsureSuccessStatusCode();
        }

        public async Task<Response<string>> RegisterAsync(RegisterRequest request)
        {
            var result = await _client.PostAsJsonAsync("v1/identity/register", request);
            return result.IsSuccessStatusCode
                ? new Response<string>("Cadastro realizado com sucesso!", 201, "Cadastro realizado com sucesso!")
                : new Response<string>(null, 400, "[E017] Register not possible.");
        }
        public async Task<Response<string>> ForgotPasswordAsync(
                                            ForgotPasswordRequest request)
        {
            var result = await _client.PostAsJsonAsync(
                "v1/identity/forgotPassword",
                request);

            return result.IsSuccessStatusCode
                ? new Response<string>(
                    "Se o e-mail estiver cadastrado, enviaremos as instruções.")
                : new Response<string>(
                    null,
                    400,
                    "Não foi possível processar a solicitação.");
        }
        public async Task<Response<string>> ResetPasswordAsync(
                                            ResetPasswordRequest request)
        {
            var payload = new
            {
                request.Email,
                request.ResetCode,
                request.NewPassword
            };

            var result = await _client.PostAsJsonAsync(
                "v1/identity/resetPassword",
                payload);

            return result.IsSuccessStatusCode
                ? new Response<string>("Senha redefinida com sucesso.")
                : new Response<string>(
                    null,
                    400,
                    "Código inválido, expirado ou senha não aceita.");
        }
    }
}
