using Dima.Core.Models.Vouchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dima.Api.Data.Mappings;

public class VoucherRedemptionMapping
    : IEntityTypeConfiguration<VoucherRedemption>
{
    public void Configure(
        EntityTypeBuilder<VoucherRedemption> builder)
    {
        builder.ToTable("VoucherRedemption");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasColumnType("BIGINT");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasColumnType("SMALLINT");

        builder.Property(x => x.ReservedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(x => x.RedeemedAt)
            .IsRequired(false)
            .HasColumnType("DATETIME2");

        builder.Property(x => x.ReleasedAt)
            .IsRequired(false)
            .HasColumnType("DATETIME2");

        builder.HasOne(x => x.Voucher)
            .WithMany(x => x.Redemptions)
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Order)
            .WithOne()
            .HasForeignKey<VoucherRedemption>(
                x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Dima.Api.Models.User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.VoucherId,
            x.UserId,
            x.Status
        });
    }
}