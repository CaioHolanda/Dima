using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Migrations;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Categories;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var cnnStr = builder.Configuration.GetConnectionString("DefaultConnection")??string.Empty;
// 1) Registrando os servicos antes do Build()
builder.Services.AddDbContext<AppDbContext>(x => { x.UseSqlServer(cnnStr); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<ICategoryHandler, CategoryHandler>();

var app = builder.Build();


// 2) Configurando middleware antes do Run()
app.UseSwagger();
app.UseSwaggerUI();

// 3) Mapeando os endpoints (antes do Run())
app.MapPost("/v1/categories", 
    ([FromBody]     CreateCategoryRequest request, 
     [FromServices] ICategoryHandler handler) => 
    handler.CreateAsync(request))
    .WithName("Categories: Create")
    .WithSummary("Create a new Category")
    .Produces<Response<Category>>();

app.MapPut("/v1/categories/{id}",
    async([FromRoute]       long id,
          [FromBody]        UpdateCategoryRequest request,
          [FromServices]    ICategoryHandler handler) =>
    {
        request.Id=id;
        return await handler.UpdateAsync(request);
    })
    .WithName("Categories: Update")
    .WithSummary("Update a Category")
    .Produces<Response<Category?>>();

app.MapDelete("/v1/categories/{id}",
    async([FromRoute]       long id,
          [FromServices]    ICategoryHandler handler) =>
    {
        var request=new DeleteCategoryRequest {Id=id};
        // para teste
        request.UserId = "test1@mail.com";
        return await handler.DeleteAsync(request);
    })
    .WithName("Categories: Delete")
    .WithSummary("Remove a Category")
    .Produces<Response<Category?>>();

app.MapGet("/v1/categories/",
    async([FromServices]    ICategoryHandler handler) =>
    {
        var request=new GetAllCategoriesRequest {UserId="test2@mail.com"};
        return await handler.GetAllAsync(request);
    })
    .WithName("Categories: GetAll")
    .WithSummary("Return all Categories")
    .Produces<PagedResponse<List<Category>?>>();

app.Run();


