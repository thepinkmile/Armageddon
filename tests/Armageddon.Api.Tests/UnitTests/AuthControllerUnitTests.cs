#nullable enable
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Armageddon.Api.Controllers;
using Armageddon.Api.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Armageddon.Api.Tests.UnitTests;

public class AuthControllerUnitTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(Mock<UserManager<ApplicationUser>> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(userManager.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
    }

    [Fact]
    public async Task Login_BadRequest_OnMissingCredentials()
    {
        var um = CreateUserManagerMock();
        var sm = CreateSignInManagerMock(um);
        var config = new Mock<IConfiguration>();
        var controller = new AuthController(sm.Object, um.Object, config.Object);

        var result = await controller.Login(new AuthController.LoginRequest(null!, null!));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_Unauthorized_When_User_NotFound()
    {
        var um = CreateUserManagerMock();
        um.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        um.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var sm = CreateSignInManagerMock(um);
        var config = new Mock<IConfiguration>();
        var controller = new AuthController(sm.Object, um.Object, config.Object);

        var result = await controller.Login(new AuthController.LoginRequest("nosuch", "pw"));

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Login_Unauthorized_When_PasswordInvalid()
    {
        var user = new ApplicationUser { Id = "1", UserName = "bob" };
        var um = CreateUserManagerMock();
        um.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
        um.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

        var sm = CreateSignInManagerMock(um);
        var config = new Mock<IConfiguration>();
        var controller = new AuthController(sm.Object, um.Object, config.Object);

        var result = await controller.Login(new AuthController.LoginRequest("bob", "bad"));

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Login_Returns_Ok_With_Jwt_On_Success()
    {
        var user = new ApplicationUser { Id = "1", UserName = "admin", MustChangePassword = true, Email = "admin@x" };
        var um = CreateUserManagerMock();
        um.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
        um.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        um.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        um.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Administrator" });

        var sm = CreateSignInManagerMock(um);
        var config = new Mock<IConfiguration>();
        // provide symmetric key and issuer used by controller
        config.Setup(c => c[It.Is<string>(s => s == "Jwt:Key")]).Returns(new string('a', 64));
        config.Setup(c => c[It.Is<string>(s => s == "Jwt:Issuer")]).Returns("UnitTests");

        var controller = new AuthController(sm.Object, um.Object, config.Object);

        var result = await controller.Login(new AuthController.LoginRequest("admin", "admin"));

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeAssignableTo<AuthController.LoginResponse>();
        var token = ((AuthController.LoginResponse)ok.Value!).Token;
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Me_Returns_Unauthorized_When_NoUser()
    {
        var um = CreateUserManagerMock();
        um.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);
        var sm = CreateSignInManagerMock(um);
        var config = new Mock<IConfiguration>();
        var controller = new AuthController(sm.Object, um.Object, config.Object);

        // set an authenticated principal
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "no-user") }, "Test")) } };

        var result = await controller.Me();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Me_Returns_CurrentUser_When_UserFound()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "admin", Email = "a@b" };
        var um = CreateUserManagerMock();
        um.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Administrator" });
        var sm = CreateSignInManagerMock(um);
        var config = new Mock<IConfiguration>();
        var controller = new AuthController(sm.Object, um.Object, config.Object);

        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }, "Test")) } };

        var result = await controller.Me();

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeAssignableTo<AuthController.CurrentUserResponse>();
        var body = (AuthController.CurrentUserResponse)ok.Value!;
        body.UserName.Should().Be("admin");
    }
}
