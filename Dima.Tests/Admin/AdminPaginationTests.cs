using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Core.Enums;
using Dima.Core.Requests.Products;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Requests.Users;
using Dima.Core.Requests.Order;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace Dima.Tests.Admin;
public class AdminPaginationTests
{
    [Fact]
    public async Task Search_and_filters_cover_records_beyond_first_hundred_and_count_matches()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var context = new TestContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        for (var i = 1; i <= 125; i++)
        {
            context.Products.Add(new Product { Id = i, Title = $"Produto {i:000}", Slug = $"produto-{i}", Description = "Descrição", IsActive = i != 125, AccessDurationMonths = 1 });
            context.Users.Add(new User { Id = i, Email = $"user{i:000}@test.com", UserName = $"user{i:000}", NormalizedUserName = $"USER{i:000}", LockoutEnd = i == 125 ? DateTimeOffset.MaxValue : null });
            context.Vouchers.Add(new Voucher { Id = i, Code = $"V{i:000}", Title = "Voucher", Description = "Descrição", IsActive = i != 125 });
            context.Orders.Add(new Order { Id = i, Number = $"ORDER{i:000}", ProductId = i, UserId = i, CreatedAt = new DateTime(2026, 1, 1), Status = i == 125 ? EOrderStatus.Paid : EOrderStatus.Canceled, AccessStartsAt = i == 125 ? DateTime.UtcNow.AddDays(-1) : null, AccessEndsAt = i == 125 ? DateTime.UtcNow.AddDays(5) : null });
        }
        await context.SaveChangesAsync();
        var products = new ProductHandler(context);
        var result = await products.GetAllForAdminAsync(new GetAllAdminProductsRequest { SearchTerm = "  PRODUTO 125 ", IsActive = false, PageSize = 10 });
        Assert.True(result.IsSuccess); Assert.Equal(1, result.TotalCount); Assert.Equal(125, Assert.Single(result.Data!).Id);
        var p1 = await products.GetAllForAdminAsync(new GetAllAdminProductsRequest { PageSize = 100 });
        var p2 = await products.GetAllForAdminAsync(new GetAllAdminProductsRequest { PageNumber = 2, PageSize = 100 });
        Assert.Equal(125, p1.TotalCount); Assert.Equal(25, p2.Data!.Count); Assert.Empty(p1.Data!.Select(x => x.Id).Intersect(p2.Data.Select(x => x.Id)));
        var vouchers = await new AdminVoucherHandler(context).GetAllForAdminAsync(new GetAllAdminVouchersRequest { SearchTerm = "v125", IsActive = false });
        Assert.True(vouchers.IsSuccess); Assert.Equal(1, vouchers.TotalCount); Assert.Equal(125, Assert.Single(vouchers.Data!).Id);
        var users = await new AdminUserHandler(context, null!, TimeProvider.System).GetAllAsync(new GetAllAdminUsersRequest { SearchTerm = "USER125", IsActive = false, IsPremium = true });
        Assert.True(users.IsSuccess); Assert.Equal(1, users.TotalCount); Assert.Equal(125, Assert.Single(users.Data!).Id);
        var ordersHandler = new AdminOrderHandler(context);
        var orders = await ordersHandler.GetAllAsync(new GetAllAdminOrdersRequest { SearchTerm = "order125", Status = EOrderStatus.Paid });
        Assert.True(orders.IsSuccess); Assert.Equal(1, orders.TotalCount); Assert.Equal(125, Assert.Single(orders.Data!).Id);
        var o1 = await ordersHandler.GetAllAsync(new GetAllAdminOrdersRequest { PageSize = 100 });
        var o2 = await ordersHandler.GetAllAsync(new GetAllAdminOrdersRequest { PageNumber = 2, PageSize = 100 });
        Assert.True(o1.IsSuccess); Assert.True(o2.IsSuccess); Assert.Equal(25, o2.Data!.Count);
        Assert.Empty(o1.Data!.Select(x => x.Id).Intersect(o2.Data.Select(x => x.Id)));
        var empty = await products.GetAllForAdminAsync(new GetAllAdminProductsRequest { SearchTerm = "não existe" });
        Assert.True(empty.IsSuccess); Assert.Equal(0, empty.TotalCount); Assert.Empty(empty.Data!);
    }
    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-10, 10000, 1, 100)]
    [InlineData(2, 25, 2, 25)]
    public void Pagination_bounds_are_shared(int page, int size, int expectedPage, int expectedSize)
    {
        var request = new GetAllAdminProductsRequest { PageNumber = page, PageSize = size };
        Assert.Equal(expectedPage, request.PageNumber); Assert.Equal(expectedSize, request.PageSize);
    }
    private sealed class TestContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
                foreach (var constraint in entity.GetCheckConstraints().ToList()) entity.RemoveCheckConstraint(constraint.Name!);
            modelBuilder.Entity<Order>().Property<byte[]>("RowVersion").HasDefaultValue(new byte[8]);
            modelBuilder.Entity<User>().Property(x => x.LockoutEnd)
                .HasConversion(x => x.HasValue ? x.Value.UtcTicks : (long?)null, x => x.HasValue ? new DateTimeOffset(x.Value, TimeSpan.Zero) : null);
        }
    }
}
