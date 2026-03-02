using Dima.Api.Data;
using Dima.Api.Endpoints;
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

// Api Health-check
app.MapGet("/", () => new { message = "OK" });

app.MapEndpoints();
app.Run();


