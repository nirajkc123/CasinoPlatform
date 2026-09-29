using CasinoPlatform.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CasinoPlatform.Api.Data;

/// <summary>
/// IdentityDbContext<...,Guid> gives us AspNetUsers/AspNetRoles/etc for free
/// (registration, login, roles - Stage 2) on top of our own casino tables.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<GameRound> GameRounds => Set<GameRound>();
    public DbSet<BlackjackRound> BlackjackRounds => Set<BlackjackRound>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Wallet>(entity =>
        {
            entity.HasIndex(w => w.UserId).IsUnique();
            entity.Property(w => w.Balance).HasColumnType("decimal(18,2)");
            entity.HasOne(w => w.User)
                  .WithOne(u => u.Wallet)
                  .HasForeignKey<Wallet>(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            entity.Property(t => t.BalanceAfter).HasColumnType("decimal(18,2)");
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => t.CreatedAtUtc);
        });

        builder.Entity<GameRound>(entity =>
        {
            entity.Property(g => g.BetAmount).HasColumnType("decimal(18,2)");
            entity.Property(g => g.PayoutAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(g => g.UserId);
            // This unique index is the actual replay-protection mechanism:
            // a second insert with the same RequestId throws a DbUpdateException.
            entity.HasIndex(g => g.RequestId).IsUnique();
        });

        builder.Entity<BlackjackRound>(entity =>
        {
            entity.Property(b => b.BetAmount).HasColumnType("decimal(18,2)");
            entity.Property(b => b.PayoutAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(b => b.UserId);
            entity.HasIndex(b => b.RequestId).IsUnique();
        });
    }
}
