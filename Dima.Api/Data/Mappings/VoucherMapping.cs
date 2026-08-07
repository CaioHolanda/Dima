using Dima.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dima.Core.Enums;

namespace Dima.Api.Data.Mappings
{
    public class VoucherMapping : IEntityTypeConfiguration<Voucher>
    {
        public void Configure(EntityTypeBuilder<Voucher> builder)
        {
            builder.ToTable("Voucher");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Number)
                .IsRequired()
                .HasColumnType("CHAR")
                .HasMaxLength(8);

            builder.HasIndex(x => x.Number)
                .IsUnique();

            builder.Property(x => x.Title)
                .IsRequired()
                .HasColumnType("NVARCHAR")
                .HasMaxLength(80);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasColumnType("NVARCHAR")
                .HasMaxLength(255);

            builder.Property(x => x.DiscountType)
                .IsRequired()
                .HasColumnType("SMALLINT")
                .HasDefaultValue(EVoucherDiscountType.FixedAmount); 

            builder.Property(x => x.Value)
                .IsRequired()
                .HasColumnType("DECIMAL(18,2)");

            builder.Property(x => x.StartsAt)
                .IsRequired(false)
                .HasColumnType("DATETIME2");

            builder.Property(x => x.EndsAt)
                .IsRequired(false)
                .HasColumnType("DATETIME2");

            builder.Property(x => x.MaxTotalUses)
                .IsRequired(false)
                .HasColumnType("INT");

            builder.Property(x => x.MaxUsesPerUser)
                .IsRequired(false)
                .HasColumnType("INT");

            builder.Property(x => x.AssignedUserId)
                .IsRequired(false)
                .HasColumnType("VARCHAR")
                .HasMaxLength(160);

            builder.Property(x => x.ProductId)
                .IsRequired(false);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasColumnType("BIT")
                .HasDefaultValue(true);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Redemptions)
                .WithOne(x => x.Voucher)
                .HasForeignKey(x => x.VoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Voucher_Value_Positive",
                    "[Value] > 0");

                table.HasCheckConstraint(
                    "CK_Voucher_Percentage_Range",
                    "[DiscountType] <> 2 OR [Value] <= 100");

                table.HasCheckConstraint(
                    "CK_Voucher_Validity",
                    "[StartsAt] IS NULL OR [EndsAt] IS NULL OR [EndsAt] > [StartsAt]");

                table.HasCheckConstraint(
                    "CK_Voucher_MaxTotalUses",
                    "[MaxTotalUses] IS NULL OR [MaxTotalUses] > 0");

                table.HasCheckConstraint(
                    "CK_Voucher_MaxUsesPerUser",
                    "[MaxUsesPerUser] IS NULL OR [MaxUsesPerUser] > 0");

                table.HasCheckConstraint(
                    "CK_Voucher_DiscountType",
                    "[DiscountType] IN (1, 2)");

                table.HasCheckConstraint(
                    "CK_Voucher_Number_Length",
                    "LEN([Number]) = 8");
            });
        }
    }
}