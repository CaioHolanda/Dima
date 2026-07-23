using Dima.Core.Handlers;
using Dima.Core.Requests.Account;
using Dima.Core.Responses;
using System.Net;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class AccountHandler(IHttpClientFactory httpClientFactory) : IAccountHandler
    {
        private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);
        public async Task<Response<string>> LoginAsync(
            LoginRequest request)
        {
            var result = await _client.PostAsJsonAsync(
                "v1/identity/login?useCookies=true",
                request);

            if (result.IsSuccessStatusCode)
            {
                return new Response<string>(
                    "Login realizado com sucesso!",
                    200,
                    "Login realizado com sucesso!");
            }

            if (result.StatusCode == HttpStatusCode.Unauthorized)
            {
                var content = await result.Content.ReadAsStringAsync();

                if (content.Contains(
                    "NotAllowed",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return new Response<string>(
                        null,
                        401,
                        "[E093] Não foi possível entrar. Verifique suas credenciais e confirme seu e-mail.");
                }

                return new Response<string>(
                    null,
                    401,
                    "[E016] E-mail ou senha inválidos.");
            }

            return new Response<string>(
                null,
                (int)result.StatusCode,
                "[E092] Não foi possível realizar o login.");
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

            if (result.IsSuccessStatusCode)
            {
                return new Response<string>(
                    "Se o e-mail estiver cadastrado, enviaremos as instruções.",
                    200,
                    "Solicitação processada com sucesso.");
            }

            return new Response<string>(
                null,
                (int)result.StatusCode,
                "[E094] Não foi possível processar a solicitação.");
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
                    "[E091] Código inválido, expirado ou senha não aceita.");
        }
    }
}
