using Dima.Api.Models;
using Dima.Core.Models;
using Dima.Core.Models.Reports;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace Dima.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options):IdentityDbContext
        <Dima.Api.Models.User,
                IdentityRole<long>,
                long,
                IdentityUserClaim<long>,
                IdentityUserRole<long>,
                IdentityUserLogin<long>,
                IdentityRoleClaim<long>,
                IdentityUserToken<long>
    >(options)
    {
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<IncomesAndExpenses> IncomesAndExpenses { get; set; } = null!;
        public DbSet<IncomesByCategory> IncomesByCategory { get; set; } = null!;
        public DbSet<ExpensesByCategory> ExpensesByCategory { get; set; } = null!;
        public DbSet<VoucherRedemption> VoucherRedemptions { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Voucher> Vouchers { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly);

            modelBuilder.Entity<IncomesAndExpenses>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vwGetIncomesAndExpenses");

                entity.Property(x => x.Incomes)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Expenses)
                    .HasPrecision(18, 2);
            });

            modelBuilder.Entity<IncomesByCategory>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vwGetIncomesByCategory");

                entity.Property(x => x.Incomes)
                    .HasPrecision(18, 2);
            });

            modelBuilder.Entity<ExpensesByCategory>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vwGetExpensesByCategory");

                entity.Property(x => x.Expenses)
                    .HasPrecision(18, 2);
            });
        }
    }
}
