namespace Armageddon.Abstractions.Models;

public record Role
(
    Guid Id,
    string? Name,
    bool Enabled
);
