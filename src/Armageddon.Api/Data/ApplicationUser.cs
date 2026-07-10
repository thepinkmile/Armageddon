using Microsoft.AspNetCore.Identity;

namespace Armageddon.Api.Data;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool MustChangePassword { get; set; } = true;
    public bool Enabled { get; set; } = true;

    public Abstractions.Models.User ToDto()
        => new(Id, UserName, Email, MustChangePassword, Enabled, []);

    public static ApplicationUser FromDto(Abstractions.Models.User dto)
        => new()
        {
            Id = dto.Id,
            UserName = dto.UserName,
            Email = dto.Email,
            MustChangePassword = dto.MustChangePassword,
            Enabled = dto.Enabled,
            EmailConfirmed = true
        };
}
