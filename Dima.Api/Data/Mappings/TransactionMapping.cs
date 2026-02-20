using Dima.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dima.Api.Data.Mappings
{
    public class TransactionMapping : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transaction");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Title)
                .IsRequired(true)
                .HasColumnType("NVARCHAR")
                .HasMaxLength(80);
            builder.Property(x => x.Type)
                .IsRequired(true)
                .HasColumnType("SMALLINT");
            builder.Property(t => t.Amount)
                .IsRequired(true)
                .HasColumnType("MONEY")
                .HasMaxLength(255);
            builder.Property(t => t.CreateAt)
                .IsRequired(true);
            builder.Property(t => t.PaidOrReceivedAt)
                .IsRequired(false);
            builder.Property(t => t.UserId)
                .IsRequired(true)
                .HasColumnType("VARCHAR")
                .HasMaxLength(160);
        }
    }
}
