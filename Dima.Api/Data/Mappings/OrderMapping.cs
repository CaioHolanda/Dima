using Dima.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dima.Api.Data.Mappings;

public class OrderMapping : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Order");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .IsRequired()
            .HasColumnType("CHAR")
            .HasMaxLength(8);

        builder.HasIndex(x => x.Number)
            .IsUnique();

        builder.Property(x => x.ExternalReference)
            .IsRequired(false)
            .HasColumnType("NVARCHAR")
            .HasMaxLength(60);

        builder.Property(x => x.Gateway)
            .IsRequired()
            .HasColumnType("SMALLINT");

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(x => x.PaidAt)
            .IsRequired(false)
            .HasColumnType("DATETIME2");

        builder.Property(x => x.AccessStartsAt)
            .IsRequired(false)
            .HasColumnType("DATETIME2");

        builder.Property(x => x.AccessEndsAt)
            .IsRequired(false)
            .HasColumnType("DATETIME2");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasColumnType("SMALLINT");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasColumnType("BIGINT");

        builder.Property(x => x.OriginalPrice)
            .IsRequired()
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(x => x.DiscountAmount)
            .IsRequired()
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(x => x.Total)
            .IsRequired()
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(x => x.RefundReference)
            .IsRequired(false)
            .HasMaxLength(60)
            .HasColumnType("NVARCHAR");

        builder.Property(x => x.RefundFailureReason)
            .IsRequired(false)
            .HasMaxLength(100)
            .HasColumnType("NVARCHAR");

        builder.Property(x => x.RefundedAt)
            .IsRequired(false)
            .HasColumnType("DATETIME2");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Dima.Api.Models.User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Order_OriginalPrice_NonNegative",
                "[OriginalPrice] >= 0");

            table.HasCheckConstraint(
                "CK_Order_DiscountAmount_NonNegative",
                "[DiscountAmount] >= 0");

            table.HasCheckConstraint(
                "CK_Order_Total_NonNegative",
                "[Total] >= 0");

            table.HasCheckConstraint(
                "CK_Order_Discount_NotGreaterThanPrice",
                "[DiscountAmount] <= [OriginalPrice]");

            table.HasCheckConstraint(
                "CK_Order_Total_Calculation",
                "[Total] = [OriginalPrice] - [DiscountAmount]");

            table.HasCheckConstraint(
                "CK_Order_AccessPeriod",
                "[AccessStartsAt] IS NULL OR [AccessEndsAt] IS NULL OR [AccessEndsAt] > [AccessStartsAt]");
        });
    }
}