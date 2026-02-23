using Dima.Api.Data;
using Dima.Core.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var cnnStr = builder.Configuration.GetConnectionString("DefaultConnection")??string.Empty;
// 1) Registrando os servicos antes do Build()
builder.Services.AddDbContext<AppDbContext>(x => { x.UseSqlServer(cnnStr); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<Handler>();

var app = builder.Build();


// 2) Configurando middleware antes do Run()
app.UseSwagger();
app.UseSwaggerUI();

// 3) Mapeando os endpoints (antes do Run())
app.MapPost("/v1/categories", (Request request, Handler handler) => handler.Handlers(request))
    .WithName("Categories: Create")
    .WithSummary("Cria uma nova Categoria")
    .Produces<Response>();

app.Run();

//Request

public class Request
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

}

//Response
public class Response
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

//Handler
public class Handler(AppDbContext context)
{
    public Response Handlers(Request request)
    {
        var category = new Category
        {
            Title = request.Title,
            Description = request.Description
        };
        context.Categories.Add(category);
        context.SaveChanges();
        return new Response
        {
            Id = category.Id,
            Title = category.Title
        };
    }
}