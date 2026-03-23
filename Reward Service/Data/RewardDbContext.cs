using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Reward_Service.Models;

namespace Reward_Service.Data;

public partial class RewardDbContext : DbContext
{
    public RewardDbContext()
    {
    }

    public RewardDbContext(DbContextOptions<RewardDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Reward> Rewards { get; set; }

    public virtual DbSet<RewardTransaction> RewardTransactions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=WalletReward;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reward>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rewards__3214EC07EB3D02D3");

            entity.HasIndex(e => e.UserId, "UQ_Rewards_UserId").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Tier)
                .HasMaxLength(20)
                .HasDefaultValue("Bronze");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<RewardTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RewardTr__3214EC071A525B64");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Reason).HasMaxLength(100);
            entity.Property(e => e.Reference).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
