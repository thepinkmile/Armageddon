using Armageddon.Abstractions.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Armageddon.Api.Data;

public class ArmageddonDbContext(DbContextOptions<ArmageddonDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Objective> Objectives { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<Score> Scores { get; set; } = null!;
    public DbSet<Round> Rounds { get; set; } = null!;
    public DbSet<Tournament> Tournaments { get; set; } = null!;
    public DbSet<Match> Matches { get; set; } = null!;
    public DbSet<TournamentResult> TournamentResults { get; set; } = null!;
    public DbSet<TournamentResultEntry> TournamentResultEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Score>()
            .Property(s => s.Points)
            .HasDefaultValue(100);

        builder.Entity<Match>(e =>
        {
            e.HasOne(m => m.TeamA).WithMany().HasForeignKey(m => m.TeamAId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.TeamB).WithMany().HasForeignKey(m => m.TeamBId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Winner).WithMany().HasForeignKey(m => m.WinnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Round).WithMany().HasForeignKey(m => m.RoundId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Tournament).WithMany(t => t.Matches).HasForeignKey(m => m.TournamentId);
        });

        builder.Entity<TournamentResult>(e =>
        {
            e.HasMany(r => r.Entries)
             .WithOne(en => en.TournamentResult)
             .HasForeignKey(en => en.TournamentResultId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
