using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class AdminVoucherHandler(
        IHttpClientFactory httpClientFactory)
        : IAdminVoucherHandler
    {
        private readonly HttpClient _client =
            httpClientFactory.CreateClient(
            Configuration.HttpClientName);
        public Task<Response<Voucher?>> ActivateAsync(ActivateVoucherRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Voucher?>>
            CreateAsync(CreateVoucherRequest request)
        {
            var response = await _client.PostAsJsonAsync(
                "v1/admin/vouchers",
                request);

            return await ReadResponseAsync(response);
        }
        public Task<Response<Voucher?>> DeactivateAsync(DeactivateVoucherRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResponse<List<Voucher>?>>
            GetAllForAdminAsync(
                GetAllAdminVouchersRequest request)
        {
            var response = await _client.GetAsync(
                $"v1/admin/vouchers" +
                $"?pageNumber={request.PageNumber}" +
                $"&pageSize={request.PageSize}");

            var result = await response.Content
                .ReadFromJsonAsync<PagedResponse<List<Voucher>?>>();

            return result ??
                new PagedResponse<List<Voucher>?>(
                    null,
                    (int)response.StatusCode,
                    "[E155] Não foi possível obter os vouchers");
        }
        public Task<Response<Voucher?>> GetByIdForAdminAsync(GetVoucherByIdRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<Response<Voucher?>> UpdateAsync(UpdateVoucherRequest request)
        {
            throw new NotImplementedException();
        }

        private static async Task<Response<Voucher?>>
        ReadResponseAsync(HttpResponseMessage response)
        {
            var result = await response.Content
                .ReadFromJsonAsync<Response<Voucher?>>();

            return result ??
                new Response<Voucher?>(
                    null,
                    (int)response.StatusCode,
                    "[E156] Não foi possível processar o voucher");
        }
    }
}
