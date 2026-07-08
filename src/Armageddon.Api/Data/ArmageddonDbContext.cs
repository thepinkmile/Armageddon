using Armageddon.Abstractions.Models;
using Microsoft.EntityFrameworkCore;

namespace Armageddon.Api.Data;

public class ArmageddonDbContext(DbContextOptions<ArmageddonDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<Score> Scores => Set<Score>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Score>()
            .HasOne(s => s.Team)
            .WithMany(t => t.Scores)
            .HasForeignKey(s => s.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Score>()
            .HasOne(s => s.Round)
            .WithMany(r => r.Scores)
            .HasForeignKey(s => s.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Score>()
            .HasOne(s => s.Objective)
            .WithMany(o => o.Scores)
            .HasForeignKey(s => s.ObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
