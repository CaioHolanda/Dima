using System.Reflection;
using Dima.Api.Data;
using Dima.Api.Data.Mappings;
using Dima.Api.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Dima.Tests.Observability;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Dima.Tests.Integrity;

public sealed class ProductSlugTests
{
    [Fact]
    public async Task DatabaseRejectsDuplicateSlugEvenWhenProductIsInactive()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();
        db.Products.Add(Product("same-slug", false));
        await db.SaveChangesAsync();
        db.Products.Add(Product("same-slug", true));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.Products.CountAsync());
    }

    [Fact]
    public async Task UpdateKeepsOwnSlugButRejectsAnotherProductsSlug()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();
        var first = Product("first-slug", true);
        db.Products.AddRange(first, Product("second-slug", false));
        await db.SaveChangesAsync();
        var handler = new ProductHandler(db);
        var same = await handler.UpdateAsync(Update(first.Id, "first-slug"));
        Assert.Equal(200, same.Code);
        var duplicate = await handler.UpdateAsync(Update(first.Id, "second-slug"));
        Assert.Equal(409, duplicate.Code);
        Assert.Equal("first-slug", (await db.Products.FindAsync(first.Id))!.Slug);
    }

    [Theory]
    [InlineData(2601, false)]
    [InlineData(2627, false)]
    [InlineData(2601, true)]
    [InlineData(2627, true)]
    public async Task DuplicateDiscoveredAtSaveReturnsConflict(int sqlNumber, bool update)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();
        var product = Product("original-slug", true);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        db.SaveFailure = SqlFailure(sqlNumber, "UX_Product_Slug");
        var logs = new RecordingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        var handler = new ProductHandler(db, factory.CreateLogger<ProductHandler>());
        var response = update
            ? await handler.UpdateAsync(Update(product.Id, "new-slug"))
            : await handler.CreateAsync(Create("new-slug"));
        Assert.Equal(409, response.Code);
        Assert.StartsWith(update ? "[E117]" : "[E114]", response.Message);
        Assert.Contains(logs.Entries, x => x.Level == LogLevel.Warning && x.Exception == db.SaveFailure);
    }

    [Theory]
    [InlineData(2601, "OTHER_INDEX")]
    [InlineData(2627, "OTHER_INDEX")]
    [InlineData(1205, "UX_Product_Slug")]
    public async Task OtherDatabaseFailuresRemainServerErrorsAndAreLogged(int sqlNumber, string index)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();
        db.SaveFailure = SqlFailure(sqlNumber, index);
        var logs = new RecordingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        var result = await new ProductHandler(db, factory.CreateLogger<ProductHandler>()).CreateAsync(Create("new-slug"));
        Assert.Equal(500, result.Code);
        Assert.DoesNotContain(index, result.Message);
        var log = Assert.Single(logs.Entries, x => x.Level == LogLevel.Error);
        Assert.Same(db.SaveFailure, log.Exception);
        Assert.Equal("CreateAsync", log.Properties["Operation"]);
    }

    [Fact]
    public void SqlServerModelContainsUniqueSlugIndex()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true").Options);
        var index = Assert.Single(db.Model.FindEntityType(typeof(Product))!.GetIndexes(),
            x => x.Properties.Count == 1 && x.Properties[0].Name == "Slug");
        Assert.True(index.IsUnique);
        Assert.Equal("UX_Product_Slug", index.GetDatabaseName());
    }

    private static ProductContext CreateContext(SqliteConnection connection) => new(
        new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
    private static Product Product(string slug, bool active) => new()
    { Title = "Produto", Description = "Descrição", Slug = slug, IsActive = active, Price = 10, AccessDurationMonths = 1 };
    private static CreateProductRequest Create(string slug) => new()
    { Title = "Produto", Description = "Descrição", Slug = slug, Price = 10, AccessDurationMonths = 1 };
    private static UpdateProductRequest Update(long id, string slug) => new()
    { Id = id, Title = "Produto", Description = "Descrição", Slug = slug, IsActive = true, Price = 10, AccessDurationMonths = 1 };

    // SqlClient exposes no public exception constructor; this simulates the SQL Server
    // error after the handler's pre-check, independently of the SQLite uniqueness test.
    private static DbUpdateException SqlFailure(int number, string index)
    {
        var constructor = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .First(x => x.GetParameters().Length == 8);
        var error = (SqlError)constructor.Invoke(new object?[]
            { number, (byte)1, (byte)14, "server", $"Cannot insert duplicate key in {index}", "procedure", 1, null });
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(errors, new[] { error });
        var method = typeof(SqlException).GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(SqlErrorCollection), typeof(string) }, null)!;
        var sql = (SqlException)method.Invoke(null, new object[] { errors, "16.0" })!;
        return new DbUpdateException("Save failed", sql);
    }

    private sealed class ProductContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public DbUpdateException? SaveFailure { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Isolate the real product mapping from unrelated SQL Server-only models.
            foreach (var entity in builder.Model.GetEntityTypes().ToArray()) builder.Ignore(entity.ClrType);
            new ProductMapping().Configure(builder.Entity<Product>());
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => SaveFailure is not null ? Task.FromException<int>(SaveFailure) : base.SaveChangesAsync(cancellationToken);
    }
}
