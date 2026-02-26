using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TaskDockr.Models;
using TaskDockr.Services;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TaskDockr.Tests.Services
{
    [TestClass]
    public class IconServiceTests
    {
        private Mock<IConfigurationService> _mockConfigService;
        private IconService _iconService;
        
        [TestInitialize]
        public void Setup()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _iconService = new IconService(_mockConfigService.Object);
        }
        
        [TestMethod]
        public async Task LoadIconAsync_WithNullPath_ReturnsDefaultErrorIcon()
        {
            // Act
            var result = await _iconService.LoadIconAsync(null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ImageSource));
        }
        
        [TestMethod]
        public async Task LoadIconAsync_WithEmptyPath_ReturnsDefaultErrorIcon()
        {
            // Act
            var result = await _iconService.LoadIconAsync(string.Empty);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ImageSource));
        }
        
        [TestMethod]
        public async Task ExtractIconFromExecutableAsync_WithInvalidPath_ReturnsDefaultAppIcon()
        {
            // Arrange
            var invalidPath = "nonexistent.exe";
            
            // Act
            var result = await _iconService.ExtractIconFromExecutableAsync(invalidPath);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ImageSource));
        }
        
        [TestMethod]
        public async Task LoadIconFromUrlAsync_WithInvalidUrl_ReturnsDefaultUrlIcon()
        {
            // Arrange
            var invalidUrl = "invalid-url";
            
            // Act
            var result = await _iconService.LoadIconFromUrlAsync(invalidUrl);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ImageSource));
        }
        
        [TestMethod]
        public async Task GetDefaultIconAsync_WithValidType_ReturnsIcon()
        {
            // Act
            var result = await _iconService.GetDefaultIconAsync(IconType.DefaultGroup);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ImageSource));
        }
        
        [TestMethod]
        public async Task CacheIconAsync_WithValidData_ReturnsTrue()
        {
            // Arrange
            var testIcon = await _iconService.GetDefaultIconAsync(IconType.DefaultApp);
            
            // Act
            var result = await _iconService.CacheIconAsync("test_key", testIcon);
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public async Task GetCachedIconAsync_WithCachedKey_ReturnsIcon()
        {
            // Arrange
            var testIcon = await _iconService.GetDefaultIconAsync(IconType.DefaultFile);
            await _iconService.CacheIconAsync("test_key", testIcon);
            
            // Act
            var result = await _iconService.GetCachedIconAsync("test_key");
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ImageSource));
        }
        
        [TestMethod]
        public async Task GetCachedIconAsync_WithUncachedKey_ReturnsNull()
        {
            // Act
            var result = await _iconService.GetCachedIconAsync("nonexistent_key");
            
            // Assert
            Assert.IsNull(result);
        }
        
        [TestMethod]
        public async Task ClearIconCacheAsync_ReturnsTrue()
        {
            // Act
            var result = await _iconService.ClearIconCacheAsync();
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public async Task IsValidIconSourceAsync_WithValidFilePath_ReturnsTrue()
        {
            // Arrange
            var validPath = "test.png";
            
            // Act
            var result = await _iconService.IsValidIconSourceAsync(validPath);
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public async Task IsValidIconSourceAsync_WithInvalidPath_ReturnsFalse()
        {
            // Arrange
            var invalidPath = "invalid.file";
            
            // Act
            var result = await _iconService.IsValidIconSourceAsync(invalidPath);
            
            // Assert
            Assert.IsFalse(result);
        }
        
        [TestMethod]
        public async Task ResolveIconPathAsync_WithValidUrl_ReturnsUrl()
        {
            // Arrange
            var validUrl = "https://example.com/icon.png";
            
            // Act
            var result = await _iconService.ResolveIconPathAsync(validUrl, IconSourceType.Url);
            
            // Assert
            Assert.AreEqual(validUrl, result);
        }
        
        [TestMethod]
        public async Task ScaleIconAsync_WithValidIcon_ReturnsScaledIcon()
        {
            // Arrange
            var originalIcon = await _iconService.GetDefaultIconAsync(IconType.DefaultApp);
            
            // Act
            var result = await _iconService.ScaleIconAsync(originalIcon, 64);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ImageSource));
        }
        
        [TestMethod]
        public async Task GetIconCountInExecutableAsync_WithInvalidPath_ReturnsZero()
        {
            // Arrange
            var invalidPath = "nonexistent.exe";
            
            // Act
            var result = await _iconService.GetIconCountInExecutableAsync(invalidPath);
            
            // Assert
            Assert.AreEqual(0, result);
        }
        
        [TestMethod]
        public void GetCacheSize_Initially_ReturnsZero()
        {
            // Act
            var result = _iconService.GetCacheSize();
            
            // Assert
            Assert.AreEqual(0, result);
        }
        
        [TestMethod]
        public void GetCacheHitRate_Initially_ReturnsZero()
        {
            // Act
            var result = _iconService.GetCacheHitRate();
            
            // Assert
            Assert.AreEqual(0, result);
        }
        
        [TestMethod]
        public async Task RemoveCachedIconAsync_WithExistingKey_ReturnsTrue()
        {
            // Arrange
            var testIcon = await _iconService.GetDefaultIconAsync(IconType.DefaultApp);
            await _iconService.CacheIconAsync("test_key", testIcon);
            
            // Act
            var result = await _iconService.RemoveCachedIconAsync("test_key");
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public async Task RemoveCachedIconAsync_WithNonExistingKey_ReturnsFalse()
        {
            // Act
            var result = await _iconService.RemoveCachedIconAsync("nonexistent_key");
            
            // Assert
            Assert.IsFalse(result);
        }
    }
}