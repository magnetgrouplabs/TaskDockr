using TaskDockr.Models;
using TaskDockr.Services;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace TaskDockr.Tests.Services
{
    public class ShortcutServiceTests
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly ShortcutService _shortcutService;

        public ShortcutServiceTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _shortcutService = new ShortcutService(_mockConfigService.Object);
        }

        [Fact]
        public async Task CreateShortcutAsync_ValidData_ReturnsShortcut()
        {
            // Arrange
            var group = new Group { Id = "1", Name = "Test Group", Shortcuts = new List<Shortcut>() };
            var config = new AppConfig { Groups = new List<Group> { group } };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);
            _mockConfigService.Setup(x => x.SaveConfigAsync(config)).Returns(Task.CompletedTask);

            // Act
            var result = await _shortcutService.CreateShortcutAsync("1", "Test Shortcut", "notepad.exe", ShortcutType.App);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Shortcut", result.Name);
            Assert.Equal("notepad.exe", result.TargetPath);
            Assert.Equal(ShortcutType.App, result.Type);
        }

        [Fact]
        public async Task CreateShortcutAsync_InvalidGroup_ThrowsException()
        {
            // Arrange
            var config = new AppConfig { Groups = new List<Group>() };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _shortcutService.CreateShortcutAsync("invalid", "Test", "target", ShortcutType.App));
        }

        [Fact]
        public async Task UpdateShortcutAsync_ValidShortcut_ReturnsTrue()
        {
            // Arrange
            var shortcut = new Shortcut { Id = "1", Name = "Old Name", TargetPath = "old.exe", Type = ShortcutType.App };
            var group = new Group { Id = "1", Name = "Test Group", Shortcuts = new List<Shortcut> { shortcut } };
            var config = new AppConfig { Groups = new List<Group> { group } };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);
            _mockConfigService.Setup(x => x.SaveConfigAsync(config)).Returns(Task.CompletedTask);

            var updatedShortcut = new Shortcut { Id = "1", Name = "New Name", TargetPath = "new.exe", Type = ShortcutType.App };

            // Act
            var result = await _shortcutService.UpdateShortcutAsync(updatedShortcut);

            // Assert
            Assert.True(result);
            Assert.Equal("New Name", shortcut.Name);
        }

        [Fact]
        public async Task DeleteShortcutAsync_ExistingShortcut_ReturnsTrue()
        {
            // Arrange
            var shortcut = new Shortcut { Id = "1", Name = "Test Shortcut", TargetPath = "test.exe", Type = ShortcutType.App };
            var group = new Group { Id = "1", Name = "Test Group", Shortcuts = new List<Shortcut> { shortcut } };
            var config = new AppConfig { Groups = new List<Group> { group } };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);
            _mockConfigService.Setup(x => x.SaveConfigAsync(config)).Returns(Task.CompletedTask);

            // Act
            var result = await _shortcutService.DeleteShortcutAsync("1", "1");

            // Assert
            Assert.True(result);
            Assert.Empty(group.Shortcuts);
        }

        [Fact]
        public async Task MoveShortcutAsync_ValidMove_ReturnsTrue()
        {
            // Arrange
            var shortcuts = new List<Shortcut>
            {
                new Shortcut { Id = "1", Name = "Shortcut 1", TargetPath = "app1.exe", Type = ShortcutType.App },
                new Shortcut { Id = "2", Name = "Shortcut 2", TargetPath = "app2.exe", Type = ShortcutType.App }
            };
            var group = new Group { Id = "1", Name = "Test Group", Shortcuts = shortcuts };
            var config = new AppConfig { Groups = new List<Group> { group } };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);
            _mockConfigService.Setup(x => x.SaveConfigAsync(config)).Returns(Task.CompletedTask);

            // Act
            var result = await _shortcutService.MoveShortcutAsync("1", "1", 1);

            // Assert
            Assert.True(result);
            Assert.Equal("Shortcut 2", group.Shortcuts[0].Name);
            Assert.Equal("Shortcut 1", group.Shortcuts[1].Name);
        }

        [Fact]
        public async Task ValidateShortcutAsync_ValidShortcut_ReturnsTrue()
        {
            // Arrange
            var shortcut = new Shortcut { Id = "1", Name = "Valid Shortcut", TargetPath = "valid.exe", Type = ShortcutType.App };
            var group = new Group { Id = "1", Name = "Test Group", Shortcuts = new List<Shortcut>() };
            var config = new AppConfig { Groups = new List<Group> { group } };
            _mockConfigService.Setup(x => x.CurrentConfig).Returns(config);

            // Act
            var result = await _shortcutService.ValidateShortcutAsync(shortcut);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetShortcutsByGroupAsync_ReturnsShortcuts()
        {
            // Arrange
            var shortcuts = new List<Shortcut>
            {
                new Shortcut { Id = "1", Name = "Shortcut 1", TargetPath = "app1.exe", Type = ShortcutType.App },
                new Shortcut { Id = "2", Name = "Shortcut 2", TargetPath = "app2.exe", Type = ShortcutType.App }
            };
            var group = new Group { Id = "1", Name = "Test Group", Shortcuts = shortcuts };
            var config = new AppConfig { Groups = new List<Group> { group } };
            _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

            // Act
            var result = await _shortcutService.GetShortcutsByGroupAsync("1");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Shortcut 1", result[0].Name);
        }

        [Fact]
        public async Task GetShortcutByIdAsync_ExistingShortcut_ReturnsShortcut()
        {
            // Arrange
            var shortcut = new Shortcut { Id = "1", Name = "Test Shortcut", TargetPath = "test.exe", Type = ShortcutType.App };
            var group = new Group { Id = "1", Name = "Test Group", Shortcuts = new List<Shortcut> { shortcut } };
            var config = new AppConfig { Groups = new List<Group> { group } };
            _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

            // Act
            var result = await _shortcutService.GetShortcutByIdAsync("1", "1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Shortcut", result.Name);
        }

        [Fact]
        public async Task LaunchShortcutAsync_ValidAppShortcut_ReturnsTrue()
        {
            // Arrange
            var shortcut = new Shortcut { Name = "Test App", TargetPath = "notepad.exe", Type = ShortcutType.App };

            // Act
            var result = await _shortcutService.LaunchShortcutAsync(shortcut);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task LaunchShortcutAsync_ValidFileShortcut_ReturnsTrue()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var shortcut = new Shortcut { Name = "Test File", TargetPath = tempFile, Type = ShortcutType.File };

            try
            {
                // Act
                var result = await _shortcutService.LaunchShortcutAsync(shortcut);

                // Assert
                Assert.True(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LaunchShortcutAsync_ValidURLShortcut_ReturnsTrue()
        {
            // Arrange
            var shortcut = new Shortcut { Name = "Test URL", TargetPath = "https://example.com", Type = ShortcutType.URL };

            // Act
            var result = await _shortcutService.LaunchShortcutAsync(shortcut);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task LaunchShortcutAsync_InvalidTarget_ReturnsFalse()
        {
            // Arrange
            var shortcut = new Shortcut { Name = "Invalid App", TargetPath = "nonexistent.exe", Type = ShortcutType.App };

            // Act
            var result = await _shortcutService.LaunchShortcutAsync(shortcut);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task LaunchShortcutAsync_NullShortcut_ReturnsFalse()
        {
            // Act
            var result = await _shortcutService.LaunchShortcutAsync(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TargetExistsAsync_ValidAppPath_ReturnsTrue()
        {
            // Arrange
            var targetPath = "notepad.exe";

            // Act
            var result = await _shortcutService.TargetExistsAsync(targetPath, ShortcutType.App);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TargetExistsAsync_ValidURL_ReturnsTrue()
        {
            // Arrange
            var targetPath = "https://example.com";

            // Act
            var result = await _shortcutService.TargetExistsAsync(targetPath, ShortcutType.URL);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TargetExistsAsync_InvalidPath_ReturnsFalse()
        {
            // Arrange
            var targetPath = "nonexistent.exe";

            // Act
            var result = await _shortcutService.TargetExistsAsync(targetPath, ShortcutType.App);

            // Assert
            Assert.False(result);
        }
    }
}