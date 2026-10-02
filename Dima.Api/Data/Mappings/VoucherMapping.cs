using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dima.Core.Enums;
using Dima.Core.Models.Vouchers;

namespace Dima.Api.Data.Mappings
{
    public class VoucherMapping : IEntityTypeConfiguration<Voucher>
    {
        public void Configure(EntityTypeBuilder<Voucher> builder)
        {
            builder.ToTable("Voucher");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasColumnType("VARCHAR")
                .HasMaxLength(20);

            builder.HasIndex(x => x.Code)
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
                .HasColumnType("SMALLINT");

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
                .HasColumnType("BIGINT");

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

            builder.HasOne<Dima.Api.Models.User>()
                .WithMany()
                .HasForeignKey(x => x.AssignedUserId)
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
                    "CK_Voucher_Code_Length",
                    "LEN([Code]) BETWEEN 4 AND 20");
            });
        }
    }
}