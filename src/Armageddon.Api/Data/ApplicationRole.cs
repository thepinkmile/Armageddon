using Microsoft.AspNetCore.Identity;

namespace Armageddon.Api.Data;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole(string roleName, bool enabled = true)
        : base(roleName)
    {
        Enabled = enabled;
    }

    public ApplicationRole(bool enabled = true)
        : base()
    {
        Enabled = enabled;
    }

    public bool Enabled { get; set; } = true;

    public Abstractions.Models.Role ToDto()
        => new(Id, Name, Enabled);

    public static ApplicationRole FromDto(Abstractions.Models.Role dto)
        => new(dto.Enabled)
        {
            Id = dto.Id,
            Name = dto.Name,
        };
}
