using Dima.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dima.Api.Data.Mappings;

public sealed class AdminAuditLogMapping : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("AdminAuditLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ActorName).HasMaxLength(256);
        builder.Property(x => x.TargetType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Operation).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.TargetType, x.TargetId });
        // No foreign keys: history survives deletion of its actor or target.
    }
}
