using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskDockr; // reference to the main project

namespace TaskDockr.IntegrationTests;

public class StartupTests : WebApplicationFactory<Program>
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    [Fact]
    public async Task Application_Starts_Successfully()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>();

        // Act
        var client = factory.CreateClient();

        // Assert
        client.Should().NotBeNull();
    }
}