using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Armageddon.Api.Controllers;
using Armageddon.Api.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Armageddon.Api.Tests.UnitTests;

public class UsersControllerUnitTests
{
    [Fact]
    public async Task GetAll_Returns_ListOfUsers()
    {
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = "1", UserName = "u1", Email = "a@b.com", MustChangePassword = false }
        };

        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(u => u.Users).Returns(users.AsQueryable());

        var controller = new UsersController(userManager.Object);
        var result = await controller.GetAll();
        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeAssignableTo<IEnumerable<object>>();
    }

    [Fact]
    public async Task Create_Returns_Created_When_Success()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var controller = new UsersController(userManager.Object);
        var req = new UsersController.CreateUserRequest("newuser", "n@e.com", "P@ssw0rd", false);
        var result = await controller.Create(req);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Delete_Returns_NotFound_When_UserMissing()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(u => u.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var controller = new UsersController(userManager.Object);
        var result = await controller.Delete("nope");
        result.Should().BeOfType<NotFoundResult>();
    }
}
