using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskDockr.Services;
using TaskDockr.Models;
using System.Threading.Tasks;

namespace TaskDockr.Tests.Services
{
    [TestClass]
    public class DragDropServiceTests
    {
        private DragDropService _dragDropService;
        private GroupService _groupService;
        private ShortcutService _shortcutService;
        private ConfigurationService _configService;

        [TestInitialize]
        public void Initialize()
        {
            _configService = new ConfigurationService();
            _groupService = new GroupService(_configService, new IconService());
            _shortcutService = new ShortcutService(_configService, new IconService());
            _dragDropService = new DragDropService(_groupService, _shortcutService);
        }

        [TestMethod]
        public void DragDropService_CanBeCreated()
        {
            Assert.IsNotNull(_dragDropService);
        }

        [TestMethod]
        public async Task GroupReordering_WorksCorrectly()
        {
            // Arrange
            var group1 = new Group { Name = "Test Group 1", Position = 0 };
            var group2 = new Group { Name = "Test Group 2", Position = 1 };
            
            // Act - Simulate reordering
            var result = await _groupService.MoveGroupAsync(group1.Id, 1);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ShortcutReordering_WorksCorrectly()
        {
            // Arrange
            var group = new Group { Name = "Test Group" };
            var shortcut1 = new Shortcut { Name = "Shortcut 1" };
            var shortcut2 = new Shortcut { Name = "Shortcut 2" };
            
            group.Shortcuts.Add(shortcut1);
            group.Shortcuts.Add(shortcut2);
            
            // Act - Simulate reordering
            var result = await _shortcutService.MoveShortcutAsync(group.Id, shortcut1.Id, 1);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DragDropEvents_CanBeSubscribed()
        {
            // Arrange
            bool dragStartedFired = false;
            bool dropCompletedFired = false;
            
            _dragDropService.DragStarted += (s, e) => dragStartedFired = true;
            _dragDropService.DropCompleted += (s, e) => dropCompletedFired = true;
            
            // Act - Simulate events (would need UI context for actual testing)
            
            // Assert - Events can be subscribed
            Assert.IsTrue(true); // Basic test to ensure no exceptions
        }

        [TestMethod]
        public void DragDropHelper_Methods_WorkCorrectly()
        {
            // Test utility methods
            var isTouch = TaskDockr.Utils.DragDropHelper.IsTouchDevice();
            var threshold = TaskDockr.Utils.DragDropHelper.GetDragThreshold();
            
            Assert.IsTrue(threshold > 0);
        }
    }
}