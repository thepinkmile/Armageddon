namespace Armageddon.Web.Services;

public class GameSettingsService(IArmageddonApiClient apiClient)
{
    private const string MatchDurationKey = "MatchDurationMinutes";
    private const int DefaultMatchDuration = 20;

    private int? _matchDurationMinutes;

    /// <summary>In-memory cache of the match duration. Call <see cref="LoadAsync"/> first.</summary>
    public int MatchDurationMinutes => _matchDurationMinutes ?? DefaultMatchDuration;

    /// <summary>Loads all settings from the API. Safe to call multiple times.</summary>
    public async Task LoadAsync()
    {
        var setting = await apiClient.GetSettingAsync(MatchDurationKey);
        _matchDurationMinutes = setting is not null && int.TryParse(setting.Value, out var v)
            ? v
            : DefaultMatchDuration;
    }

    /// <summary>Persists the match duration to the API and updates the local cache.</summary>
    public async Task SaveMatchDurationAsync(int minutes)
    {
        minutes = Math.Max(1, minutes);
        await apiClient.UpsertSettingAsync(MatchDurationKey, minutes.ToString());
        _matchDurationMinutes = minutes;
    }
}
