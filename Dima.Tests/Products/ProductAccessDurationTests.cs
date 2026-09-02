using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Microsoft.EntityFrameworkCore;

namespace Dima.Tests.Products;

public class ProductAccessDurationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_rejects_non_positive_duration(
        int duration)
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"DimaTests-{Guid.NewGuid()}")
                .Options;

        await using var context =
            new AppDbContext(options);

        var handler = new ProductHandler(context);

        var request = new CreateProductRequest
        {
            Title = "Plano de teste",
            Description = "Produto para teste",
            Price = 100m,
            Slug = "plano-teste",
            IsActive = true,
            AccessDurationMonths = duration
        };

        var result = await handler.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Code);
        Assert.Contains(
            "[E172]",
            result.Message ?? string.Empty);
        Assert.Empty(context.Products);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Update_rejects_non_positive_duration(
        int duration)
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"DimaTests-{Guid.NewGuid()}")
                .Options;

        await using var context =
            new AppDbContext(options);

        var product = new Product
        {
            Title = "Plano Semestral",
            Description = "Produto para teste",
            Price = 600m,
            Slug = "plano-semestral",
            IsActive = true,
            AccessDurationMonths = 6
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new ProductHandler(context);

        var request = new UpdateProductRequest
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            Price = product.Price,
            Slug = product.Slug,
            IsActive = product.IsActive,
            AccessDurationMonths = duration
        };

        var result = await handler.UpdateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Code);
        Assert.Contains(
            "[E173]",
            result.Message ?? string.Empty);

        var storedProduct =
            await context.Products.SingleAsync();

        Assert.Equal(
            6,
            storedProduct.AccessDurationMonths);
    }
    [Fact]
    public async Task Create_persists_valid_duration()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"DimaTests-{Guid.NewGuid()}")
                .Options;

        await using var context =
            new AppDbContext(options);

        var handler = new ProductHandler(context);

        var request = new CreateProductRequest
        {
            Title = "Plano Anual",
            Description = "Produto para teste",
            Price = 780m,
            Slug = "plano-anual",
            IsActive = true,
            AccessDurationMonths = 12
        };

        var result = await handler.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.Code);

        var storedProduct =
            await context.Products.SingleAsync();

        Assert.Equal(
            12,
            storedProduct.AccessDurationMonths);
    }

    [Fact]
    public async Task Update_changes_to_valid_duration()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"DimaTests-{Guid.NewGuid()}")
                .Options;

        await using var context =
            new AppDbContext(options);

        var product = new Product
        {
            Title = "Plano Mensal",
            Description = "Produto para teste",
            Price = 280m,
            Slug = "plano-mensal",
            IsActive = true,
            AccessDurationMonths = 1
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new ProductHandler(context);

        var request = new UpdateProductRequest
        {
            Id = product.Id,
            Title = "Plano Trimestral",
            Description = product.Description,
            Price = 350m,
            Slug = "plano-trimestral",
            IsActive = true,
            AccessDurationMonths = 3
        };

        var result = await handler.UpdateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Code);

        var storedProduct =
            await context.Products.SingleAsync();

        Assert.Equal(
            3,
            storedProduct.AccessDurationMonths);
    }
}