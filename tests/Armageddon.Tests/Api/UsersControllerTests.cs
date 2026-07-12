using Armageddon.Abstractions.Models;
using Armageddon.Api.Controllers;
using Armageddon.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Armageddon.Tests.Api;

public class UsersControllerTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(IEnumerable<ApplicationUser>? users = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();

        var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var pwdValidators = new List<IPasswordValidator<ApplicationUser>>();
        var keyNormalizer = new Mock<ILookupNormalizer>().Object;
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>().Object;
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<ApplicationUser>>>().Object;

        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, options.Object, passwordHasher, userValidators, pwdValidators, keyNormalizer, errors, services, logger);

        users ??= Enumerable.Empty<ApplicationUser>();
        mgr.Setup(m => m.Users).Returns(users.AsQueryable());

        return mgr;
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithUsers()
    {
        var users = new[] { new ApplicationUser { Id = Guid.NewGuid(), UserName = "alice", Email = "a@x.com", MustChangePassword = false } };
        var mgrMock = CreateUserManagerMock(users);

        var controller = new UsersController(mgrMock.Object);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value!);
        Assert.Single(list);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenMissing()
    {
        var mgrMock = CreateUserManagerMock();
        mgrMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var controller = new UsersController(mgrMock.Object);

        var result = await controller.Get("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsOk_WhenFound()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "bob", Email = "b@x.com", MustChangePassword = true };
        var mgrMock = CreateUserManagerMock(new[] { user });
        mgrMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);

        var controller = new UsersController(mgrMock.Object);

        var result = await controller.Get("1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenSuccess()
    {
        var mgrMock = CreateUserManagerMock();
        mgrMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
               .ReturnsAsync(IdentityResult.Success)
               .Callback<ApplicationUser, string>((u, p) => u.Id = Guid.NewGuid());

        var controller = new UsersController(mgrMock.Object);

        var req = new UsersController.CreateUserRequest("sam", "s@x.com", "P@ssw0rd", false);
        var result = await controller.Create(req);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("Get", created.ActionName);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenFails()
    {
        var mgrMock = CreateUserManagerMock();
        var failed = IdentityResult.Failed(new IdentityError { Description = "bad" });
        mgrMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(failed);

        var controller = new UsersController(mgrMock.Object);

        var req = new UsersController.CreateUserRequest("sam", "s@x.com", "P@ssw0rd", false);
        var result = await controller.Create(req);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        var mgrMock = CreateUserManagerMock();
        mgrMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var controller = new UsersController(mgrMock.Object);

        var result = await controller.Delete("nope");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenSuccess()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "del", Email = "d@x.com", MustChangePassword = false };
        var mgrMock = CreateUserManagerMock([user]);
        mgrMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mgrMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var controller = new UsersController(mgrMock.Object);

        var result = await controller.Delete(userId.ToString());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenDeleteFails()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "del2", Email = "d2@x.com", MustChangePassword = false };
        var mgrMock = CreateUserManagerMock([user]);
        mgrMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mgrMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "err" }));

        var controller = new UsersController(mgrMock.Object);

        var result = await controller.Delete(userId.ToString());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }
}
