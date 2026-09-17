using Dima.Core.Common.Extensions;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Transactions;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class TransactionHandler(IHttpClientFactory httpClientFactory) : ITransactionHandler
    {
        private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);
        public async Task<Response<Transaction?>> CreateAsync(CreateTransactionRequest request)
        {
            using var result = await _client.PostAsJsonAsync("v1/transactions", request);
            return await HttpResponseReader.ReadAsync<Transaction?>(result, "[E018] Fail to create transaction.");
        }



        public async Task<Response<Transaction?>> DeleteAsync(DeleteTransactionRequest request)
        {
            using var result = await _client.DeleteAsync($"v1/transactions/{request.Id}");
            return await HttpResponseReader.ReadAsync<Transaction?>(result, "[E019] Fail to remove transaction.");
        }



        public async Task<Response<Transaction?>> GetByIdAsync(GetTransactionByIdRequest request)
        =>
            await _client.GetResponseAsync<Transaction?>($"v1/transactions/{request.Id}", "[E020] Fail to read transaction.");



         public async Task<PagedResponse<List<Transaction>?>> GetByPeriodAsync(GetTransactionsByPeriodRequest request)
        {
            const string format = "yyyy-MM-dd";
            var startDate = request.StartDate is not null 
                ? request.StartDate.Value.ToString(format)
                : DateTime.UtcNow.GetFirstDay().ToString(format);
            var endDate = request.EndDate is not null 
                ? request.EndDate.Value.ToString(format)
                : DateTime.UtcNow.GetLastDay().ToString(format);
            var url = $"v1/transactions?startDate={startDate}&endDate={endDate}&pageNumber={request.PageNumber}&pageSize={request.PageSize}";
            return await _client.GetPagedResponseAsync<List<Transaction>?>(url, "[E021] Fail to consult period.");
        }




        public async Task<Response<Transaction?>> UpdateAsync(UpdateTransactionRequest request)
        {
            using var result = await _client.PutAsJsonAsync($"v1/transactions/{request.Id}", request);
            return await HttpResponseReader.ReadAsync<Transaction?>(result, "[E022] Fail to update transaction.");
        }
    }
}
