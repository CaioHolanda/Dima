using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Categories;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class CategoryHandler(IHttpClientFactory httpClientFactory) : ICategoryHandler
    {
        private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);


        public async Task<Response<Category?>> CreateAsync(CreateCategoryRequest request)
        {
            using var result = await _client.PostAsJsonAsync("v1/categories", request);
            return await HttpResponseReader.ReadAsync<Category?>(result, "[E024] Falha ao criar categoria.");
        }


        public async Task<Response<Category?>> DeleteAsync(DeleteCategoryRequest request)
        {
            using var result = await _client.DeleteAsync($"v1/categories/{request.Id}");
            return await HttpResponseReader.ReadAsync<Category?>(result, "[E025] Falha ao excluir a categoria.");
        }


        public async Task<PagedResponse<List<Category>>> GetAllAsync(GetAllCategoriesRequest request)

        =>
            await _client.GetPagedResponseAsync<List<Category>>($"v1/categories?pageNumber={request.PageNumber}&pageSize={request.PageSize}", "[E026] Falha ao obter categorias.");


          public async Task<Response<Category?>> GetByIdAsync(GetCategoryByIdRequest request)
        =>
            await _client.GetResponseAsync<Category?>($"v1/categories/{request.Id}", "[E027] Falha ao obter a categoria.");


       
        public async Task<Response<Category?>> UpdateAsync(UpdateCategoryRequest request)
        {
            using var result = await _client.PutAsJsonAsync($"v1/categories/{request.Id}", request);
            return await HttpResponseReader.ReadAsync<Category?>(result, "Falha ao atualizar a categoria.");
        }
    }
}
