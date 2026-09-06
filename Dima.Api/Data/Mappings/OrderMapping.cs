using Dima.Core.Models;
using Dima.Core.Enums;
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

        // Índice geral para consultar os pedidos do usuário.
        builder.HasIndex(x => x.UserId);

        // Cada usuário pode ter apenas um pedido aguardando pagamento.
        builder.HasIndex(
                x => x.UserId,
                "UX_Order_UserId_WaitingPayment")
            .IsUnique()
            .HasFilter(
                $"[Status] = {(int)EOrderStatus.WaintingPayment}");

        builder.Property(x => x.VoucherCodeSnapshot)
            .IsRequired(false)
            .HasColumnType("VARCHAR")
            .HasMaxLength(20);

        builder.Property(x => x.VoucherDiscountTypeSnapshot)
            .IsRequired(false)
            .HasColumnType("SMALLINT");

        builder.Property(x => x.VoucherValueSnapshot)
            .IsRequired(false)
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(x => x.OriginalPrice)
            .IsRequired()
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(x => x.DiscountAmount)
            .IsRequired()
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(x => x.Total)
            .IsRequired()
            .HasColumnType("DECIMAL(18,2)");

        builder.Property(x => x.AccessDurationMonths)
            .IsRequired()
            .HasColumnType("INT");

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

        builder.Property(x => x.RefundReason)
            .HasColumnType("SMALLINT")
            .IsRequired(false);

        builder.Property(x => x.RefundReasonDetails)
            .HasColumnType("NVARCHAR(500)")
            .IsRequired(false);

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
                "CK_Order_AccessDurationMonths_Positive",
                "[AccessDurationMonths] > 0");

            table.HasCheckConstraint(
                "CK_Order_AccessPeriod",
                "[AccessStartsAt] IS NULL OR [AccessEndsAt] IS NULL OR [AccessEndsAt] > [AccessStartsAt]");

            table.HasCheckConstraint(
                "CK_Order_VoucherSnapshot_Consistency",
                """
                (
                    [VoucherId] IS NULL
                    AND [VoucherCodeSnapshot] IS NULL
                    AND [VoucherDiscountTypeSnapshot] IS NULL
                    AND [VoucherValueSnapshot] IS NULL
                )
                OR
                (
                    [VoucherId] IS NOT NULL
                    AND [VoucherCodeSnapshot] IS NOT NULL
                    AND [VoucherDiscountTypeSnapshot] IS NOT NULL
                    AND [VoucherValueSnapshot] IS NOT NULL
                )
                """);

            table.HasCheckConstraint(
                "CK_Order_VoucherSnapshot_DiscountType",
                """
                [VoucherDiscountTypeSnapshot] IS NULL
                OR [VoucherDiscountTypeSnapshot] IN (1, 2)
                """);

            table.HasCheckConstraint(
                "CK_Order_VoucherSnapshot_Value",
                """
                [VoucherValueSnapshot] IS NULL
                OR [VoucherValueSnapshot] > 0
                """);
        });
    }
}