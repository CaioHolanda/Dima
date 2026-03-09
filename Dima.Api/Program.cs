using Dima.Api.Data;
using Dima.Api.Endpoints;
using Dima.Api.Handlers;
using Dima.Api.Migrations;
using Dima.Api.Models;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Categories;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(x => { x.CustomSchemaIds(n => n.FullName); });
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.AddAuthorization();


var cnnStr = builder.Configuration.GetConnectionString("DefaultConnection")??string.Empty;
// 1) Registrando os servicos antes do Build()
builder.Services.AddDbContext<AppDbContext>(x => { x.UseSqlServer(cnnStr); });
builder.Services
        .AddIdentityCore<User>()
        .AddRoles<IdentityRole<long>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddApiEndpoints();

builder.Services.AddTransient<ICategoryHandler, CategoryHandler>();
builder.Services.AddTransient<ITransactionHandler, TransactionHandler>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();


// 2) Configurando middleware antes do Run()
app.UseSwagger();
app.UseSwaggerUI();

// Api Health-check
app.MapGet("/", () => new { message = "OK" });
app.MapEndpoints();

app.MapGroup("v1/identity")
    .WithTags("Identity")
    .MapIdentityApi<User>();

app.MapGroup("v1/identity")
    .WithTags("Identity")
    .MapPost("/logout", async(SignInManager<User> signInManager) =>
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    })
    .RequireAuthorization();

app.MapGroup("v1/identity")
    .WithTags("Identity")
    .MapGet("/roles", async(ClaimsPrincipal user) =>
    {
        if (user.Identity is null || !user.Identity.IsAuthenticated)
            return Results.Unauthorized();

        var identity = (ClaimsIdentity)user.Identity;
        var roles = identity.FindAll(identity.RoleClaimType)
        .Select(c=>new
        { 
            c.Issuer,
            c.OriginalIssuer,
            c.Type,
            c.Value,
            c.ValueType

        });
        return TypedResults.Json(roles);
    })
    .RequireAuthorization(); 

app.Run();


