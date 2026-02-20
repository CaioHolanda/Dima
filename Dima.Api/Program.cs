var builder = WebApplication.CreateBuilder(args);
// 1) Registrando os servicos antes do Build()
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<Handler>();

var app = builder.Build();

// 2) Configurando middleware antes do Run()
app.UseSwagger();
app.UseSwaggerUI();

// 3) Mapeando os endpoints (antes do Run())
app.MapPost("/v1/transaction", (Request request, Handler handler) => handler.Handlers(request))
    .WithName("Transaction: Create")
    .WithSummary("Cria uma nova transacao")
    .Produces<Response>();

app.Run();

//Request
public class Request
{
    public string Title { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; } = DateTime.Now;
    public DateTime? PaidOrReceivedAt { get; set; }
    public int Type { get; set; } 
    public decimal Amount { get; set; }
    public long CategoryId { get; set; }
    public string UserId { get; set; } = string.Empty;

}

//Response
public class Response
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

//Handler
public class Handler
{
    public Response Handlers(Request request)
    {
        return new Response
        {
            Id = 4,
            Title = request.Title
        };
    }
}