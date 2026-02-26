using TaskDockr.Models;
using TaskDockr.Services;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskDockr.Tests.Services
{
    public class GroupServiceTests
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly GroupService _groupService;

        public GroupServiceTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _groupService = new GroupService(_mockConfigService.Object);
        }

        [Fact]
        public async Task CreateGroupAsync_ValidData_ReturnsGroup()
        {
            // Arrange
            var config = new AppConfig { Groups = new List<Group>() };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);
            _mockConfigService.Setup(x => x.SaveConfigAsync(config)).Returns(Task.CompletedTask);

            // Act
            var result = await _groupService.CreateGroupAsync("Test Group");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Group", result.Name);
            Assert.Equal(0, result.Position);
            Assert.NotEmpty(result.Id);
        }

        [Fact]
        public async Task CreateGroupAsync_EmptyName_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _groupService.CreateGroupAsync(""));
        }

        [Fact]
        public async Task UpdateGroupAsync_ValidGroup_ReturnsTrue()
        {
            // Arrange
            var existingGroup = new Group { Id = "1", Name = "Old Name", Position = 0 };
            var config = new AppConfig { Groups = new List<Group> { existingGroup } };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);
            _mockConfigService.Setup(x => x.SaveConfigAsync(config)).Returns(Task.CompletedTask);

            var updatedGroup = new Group { Id = "1", Name = "New Name", Position = 0 };

            // Act
            var result = await _groupService.UpdateGroupAsync(updatedGroup);

            // Assert
            Assert.True(result);
            Assert.Equal("New Name", existingGroup.Name);
        }

        [Fact]
        public async Task DeleteGroupAsync_ExistingGroup_ReturnsTrue()
        {
            // Arrange
            var group = new Group { Id = "1", Name = "Test Group", Position = 0 };
            var config = new AppConfig { Groups = new List<Group> { group } };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);
            _mockConfigService.Setup(x => x.SaveConfigAsync(config)).Returns(Task.CompletedTask);

            // Act
            var result = await _groupService.DeleteGroupAsync("1");

            // Assert
            Assert.True(result);
            Assert.Empty(config.Groups);
        }

        [Fact]
        public async Task ValidateGroupAsync_ValidGroup_ReturnsTrue()
        {
            // Arrange
            var config = new AppConfig { Groups = new List<Group>() };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);

            var group = new Group { Id = "1", Name = "Valid Group", Position = 0 };

            // Act
            var result = await _groupService.ValidateGroupAsync(group);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task MoveGroupAsync_ValidMove_ReturnsTrue()
        {
            // Arrange
            var groups = new List<Group>
            {
                new Group { Id = "1", Name = "Group 1", Position = 0 },
                new Group { Id = "2", Name = "Group 2", Position = 1 }
            };
            var config = new AppConfig { Groups = groups };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);
            _mockConfigService.Setup(x => x.SaveConfigAsync(config)).Returns(Task.CompletedTask);

            // Act
            var result = await _groupService.MoveGroupAsync("1", 1);

            // Assert
            Assert.True(result);
            Assert.Equal("Group 2", config.Groups[0].Name);
            Assert.Equal("Group 1", config.Groups[1].Name);
        }

        [Fact]
        public async Task GetAllGroupsAsync_ReturnsOrderedGroups()
        {
            // Arrange
            var groups = new List<Group>
            {
                new Group { Id = "1", Name = "Group 1", Position = 1 },
                new Group { Id = "2", Name = "Group 2", Position = 0 }
            };
            var config = new AppConfig { Groups = groups };
            _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

            // Act
            var result = await _groupService.GetAllGroupsAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Group 2", result[0].Name);
            Assert.Equal("Group 1", result[1].Name);
        }

        [Fact]
        public async Task GroupExistsAsync_ExistingGroup_ReturnsTrue()
        {
            // Arrange
            var groups = new List<Group> { new Group { Id = "1", Name = "Test Group", Position = 0 } };
            var config = new AppConfig { Groups = groups };
            _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

            // Act
            var result = await _groupService.GroupExistsAsync("1");

            // Assert
            Assert.True(result);
        }
    }
}