using ECommerceStore.Auth.Api.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace ECommerceStore.Auth.Api.Tests.Authorization;

[TestFixture]
public class AuthorizationAttributeTests
{
    private Mock<HttpContext> _mockHttpContext;
    private Mock<HttpRequest> _mockRequest;
    private Mock<HttpResponse> _mockResponse;
    private AuthorizationFilterContext _filterContext;

    [SetUp]
    public void Setup()
    {
        _mockHttpContext = new Mock<HttpContext>();
        _mockRequest = new Mock<HttpRequest>();
        _mockResponse = new Mock<HttpResponse>();
        
        _mockHttpContext.Setup(x => x.Request).Returns(_mockRequest.Object);
        _mockHttpContext.Setup(x => x.Response).Returns(_mockResponse.Object);
        
        var actionContext = new ActionContext(
            _mockHttpContext.Object,
            new RouteData(),
            new ActionDescriptor());
        
        _filterContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());
    }

    [Test]
    public void AuthorizeAdminOrManager_ShouldSetCorrectPolicy()
    {
        // Arrange & Act
        var attribute = new AuthorizeAdminOrManagerAttribute();

        // Assert
        attribute.Policy.Should().Be("RequireAdminOrManager");
    }

    [Test]
    public void AuthorizeAdmin_ShouldSetCorrectPolicy()
    {
        // Arrange & Act
        var attribute = new AuthorizeAdminAttribute();

        // Assert
        attribute.Policy.Should().Be("RequireAdminRole");
    }

    [Test]
    public void AuthorizeManager_ShouldSetCorrectPolicy()
    {
        // Arrange & Act
        var attribute = new AuthorizeManagerAttribute();

        // Assert
        attribute.Policy.Should().Be("RequireManagerRole");
    }

    [Test]
    public void AuthorizeCustomer_ShouldSetCorrectPolicy()
    {
        // Arrange & Act
        var attribute = new AuthorizeCustomerAttribute();

        // Assert
        attribute.Policy.Should().Be("RequireCustomerRole");
    }

    [Test]
    public void AuthorizeVerifiedEmail_ShouldSetCorrectPolicy()
    {
        // Arrange & Act
        var attribute = new AuthorizeVerifiedEmailAttribute();

        // Assert
        attribute.Policy.Should().Be("RequireVerifiedEmail");
    }

    [Test]
    public void AuthorizeActiveUser_ShouldSetCorrectPolicy()
    {
        // Arrange & Act
        var attribute = new AuthorizeActiveUserAttribute();

        // Assert
        attribute.Policy.Should().Be("RequireActiveUser");
    }

    [Test]
    public void AuthorizeRole_WithSingleRole_ShouldSetRolesProperty()
    {
        // Arrange & Act
        var attribute = new AuthorizeRoleAttribute("Admin");

        // Assert
        attribute.Roles.Should().Be("Admin");
    }

    [Test]
    public void AuthorizeRole_WithMultipleRoles_ShouldSetRolesProperty()
    {
        // Arrange & Act
        var attribute = new AuthorizeRoleAttribute("Admin", "Manager");

        // Assert
        attribute.Roles.Should().Be("Admin,Manager");
    }

    [Test]
    public void AuthorizeRole_WithMultipleRoles_ShouldJoinRolesWithComma()
    {
        // Arrange & Act
        var attribute = new AuthorizeRoleAttribute("Admin", "Manager", "Employee");

        // Assert
        attribute.Roles.Should().Be("Admin,Manager,Employee");
    }

    [Test]
    public void AuthorizeRole_WithEmptyRoles_ShouldSetEmptyRolesProperty()
    {
        // Arrange & Act
        var attribute = new AuthorizeRoleAttribute();

        // Assert
        attribute.Roles.Should().Be("");
    }

    [Test]
    public void AuthorizeRole_WithNullRoles_ShouldSetNullRolesProperty()
    {
        // Arrange & Act
        var attribute = new AuthorizeRoleAttribute((string[])null!);

        // Assert
        attribute.Roles.Should().Be("");
    }
}