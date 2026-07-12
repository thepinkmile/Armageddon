using Armageddon.Abstractions.Models;

namespace Armageddon.UnitTests.Models;

public class AbstractionsModelTests
{
    // ── Role record ────────────────────────────────────────────────────────

    [Fact]
    public void Role_CanBeConstructed()
    {
        var id = Guid.NewGuid();
        var role = new Role(id, "Admin", true);

        role.Id.Should().Be(id);
        role.Name.Should().Be("Admin");
        role.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Role_Equality_ByValue()
    {
        var id = Guid.NewGuid();
        var r1 = new Role(id, "Admin", true);
        var r2 = new Role(id, "Admin", true);

        r1.Should().Be(r2);
    }

    // ── User record ────────────────────────────────────────────────────────

    [Fact]
    public void User_CanBeConstructed()
    {
        var id = Guid.NewGuid();
        var user = new User(id, "alice", "alice@test.com", false, true, ["Admin"]);

        user.Id.Should().Be(id);
        user.UserName.Should().Be("alice");
        user.Email.Should().Be("alice@test.com");
        user.MustChangePassword.Should().BeFalse();
        user.Enabled.Should().BeTrue();
        user.Roles.Should().ContainSingle("Admin");
    }

    [Fact]
    public void User_Equality_ByValue()
    {
        var id = Guid.NewGuid();
        var u1 = new User(id, "bob", "b@x.com", true, true, []);
        var u2 = new User(id, "bob", "b@x.com", true, true, []);

        u1.Should().Be(u2);
    }

    // ── Objective.UsageLabel ───────────────────────────────────────────────

    [Theory]
    [InlineData(null, "Recurring")]
    [InlineData(0, "Recurring")]
    [InlineData(-1, "Recurring")]
    [InlineData(1, "one-time")]
    [InlineData(3, "limited: 3")]
    [InlineData(10, "limited: 10")]
    public void Objective_UsageLabel_ReturnsCorrectString(int? maxUsage, string expected)
    {
        var obj = new Objective { Name = "Test", Points = 10, MaxUsage = maxUsage };

        obj.UsageLabel.Should().Be(expected);
    }

    [Fact]
    public void Objective_DefaultProperties()
    {
        var obj = new Objective();

        obj.Name.Should().Be(string.Empty);
        obj.Points.Should().Be(100);
        obj.MaxUsage.Should().BeNull();
        obj.Scores.Should().BeEmpty();
    }

    // ── TournamentResult ───────────────────────────────────────────────────

    [Fact]
    public void TournamentResult_DefaultProperties()
    {
        var result = new TournamentResult();

        result.Id.Should().Be(0);
        result.WinnerName.Should().Be(string.Empty);
        result.Entries.Should().BeEmpty();
        result.DatePlayed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TournamentResult_CanSetProperties()
    {
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var result = new TournamentResult
        {
            Id = 42,
            WinnerName = "Champions",
            DatePlayed = date
        };

        result.Id.Should().Be(42);
        result.WinnerName.Should().Be("Champions");
        result.DatePlayed.Should().Be(date);
    }

    // ── TournamentResultEntry ──────────────────────────────────────────────

    [Fact]
    public void TournamentResultEntry_DefaultProperties()
    {
        var entry = new TournamentResultEntry();

        entry.Id.Should().Be(0);
        entry.TeamName.Should().Be(string.Empty);
        entry.Position.Should().Be(0);
        entry.RoundReached.Should().Be(0);
        entry.TotalPoints.Should().Be(0);
        entry.TournamentResultId.Should().Be(0);
    }

    [Fact]
    public void TournamentResultEntry_CanSetAllProperties()
    {
        var parent = new TournamentResult { WinnerName = "Champions" };
        var entry = new TournamentResultEntry
        {
            Id = 5,
            TournamentResultId = 1,
            TournamentResult = parent,
            TeamName = "Alpha",
            Position = 1,
            RoundReached = 3,
            TotalPoints = 850
        };

        entry.TeamName.Should().Be("Alpha");
        entry.Position.Should().Be(1);
        entry.RoundReached.Should().Be(3);
        entry.TotalPoints.Should().Be(850);
        entry.TournamentResult.Should().Be(parent);
    }

    // ── Match model ────────────────────────────────────────────────────────

    [Fact]
    public void Match_DefaultProperties()
    {
        var match = new Armageddon.Abstractions.Models.Match();

        match.IsPlayoff.Should().BeFalse();
        match.Status.Should().Be(Armageddon.Abstractions.Models.MatchStatus.Pending);
        match.Slot.Should().Be(0);
    }

    // ── Tournament model ───────────────────────────────────────────────────

    [Fact]
    public void Tournament_DefaultProperties()
    {
        var t = new Armageddon.Abstractions.Models.Tournament();

        t.Status.Should().Be(Armageddon.Abstractions.Models.TournamentStatus.NotStarted);
        t.Matches.Should().BeEmpty();
    }

    // ── Round model ────────────────────────────────────────────────────────

    [Fact]
    public void Round_DefaultProperties()
    {
        var r = new Armageddon.Abstractions.Models.Round();

        r.Id.Should().Be(0);
        r.Number.Should().Be(0);
        r.Scores.Should().BeEmpty();
    }

    // ── Score model ────────────────────────────────────────────────────────

    [Fact]
    public void Score_DefaultProperties()
    {
        var s = new Armageddon.Abstractions.Models.Score();

        s.Id.Should().Be(0);
        s.Points.Should().Be(100);
    }

    // ── Team model ─────────────────────────────────────────────────────────

    [Fact]
    public void Team_DefaultProperties()
    {
        var t = new Armageddon.Abstractions.Models.Team();

        t.Id.Should().Be(0);
        t.Name.Should().Be(string.Empty);
        t.Scores.Should().BeEmpty();
    }
}
