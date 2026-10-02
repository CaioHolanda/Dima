using Dima.Core.Handlers;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;
using System.Net.Http.Json;


namespace Dima.Web.Handlers
{
    public class AdminVoucherHandler(
        IHttpClientFactory httpClientFactory)
        : IAdminVoucherHandler
    {
        private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);
        public async Task<Response<Voucher?>> ActivateAsync(ActivateVoucherRequest request)
        {
            using var response = await _client.PatchAsync(
                $"v1/admin/vouchers/{request.Id}/activate",
                null);

            return await ReadResponseAsync(response);
        }
        public async Task<Response<Voucher?>> CreateAsync(CreateVoucherRequest request)
        {
            using var response = await _client.PostAsJsonAsync(
                "v1/admin/vouchers",
                request);

            return await ReadResponseAsync(response);
        }
        public async Task<Response<Voucher?>> DeactivateAsync(DeactivateVoucherRequest request)
        {
            using var response = await _client.PatchAsync(
                $"v1/admin/vouchers/{request.Id}/deactivate",
                null);

            return await ReadResponseAsync(response);
        }
        public async Task<PagedResponse<List<AdminVoucherListItem>?>> GetAllForAdminAsync(GetAllAdminVouchersRequest request)
        {
            using var response = await _client.GetAsync(
                $"v1/admin/vouchers" +
                AdminQuery.Build(request));

            return await HttpResponseReader.ReadPagedAsync<List<AdminVoucherListItem>?>(response, "[E155] Não foi possível obter os vouchers");
        }
        private static async Task<Response<Voucher?>> ReadResponseAsync(HttpResponseMessage response)
        {
            return await HttpResponseReader.ReadAsync<Voucher?>(response, "[E156] Não foi possível processar o voucher");
        }
        public async Task<Response<AdminVoucherDetails?>>GetByIdForAdminAsync(GetVoucherByIdRequest request)
        {
            using var response = await _client.GetAsync(
                $"v1/admin/vouchers/{request.Id}");

            return await HttpResponseReader.ReadAsync<AdminVoucherDetails?>(response, "[E162] Não foi possível obter o voucher");
        }
        public async Task<Response<Voucher?>> UpdateAsync(UpdateVoucherRequest request)
        {
            using var response = await _client.PutAsJsonAsync(
                $"v1/admin/vouchers/{request.Id}",
                request);

            return await ReadResponseAsync(response);
        }
    }
}
