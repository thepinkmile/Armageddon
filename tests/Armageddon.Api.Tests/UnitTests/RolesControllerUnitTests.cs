#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Armageddon.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Armageddon.Api.Tests.UnitTests;

public class RolesControllerUnitTests
{
    [Fact]
    public void GetAll_Returns_List()
    {
        var roles = new List<IdentityRole> { new IdentityRole { Id = "r1", Name = "Administrator" } };
        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        var roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null!, null!, null!, null!);
        roleManager.Setup(r => r.Roles).Returns(roles.AsQueryable());

        var controller = new RolesController(roleManager.Object);
        var result = controller.GetAll();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Returns_BadRequest_OnEmptyName()
    {
        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        var roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null!, null!, null!, null!);
        var controller = new RolesController(roleManager.Object);
        var result = await controller.Create(new RolesController.CreateRoleRequest("   "));
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
