namespace Armageddon.Abstractions.Models;

public record User
(
    Guid Id,
    string? UserName,
    string? Email,
    bool MustChangePassword,
    bool Enabled,
    string[] Roles
);
