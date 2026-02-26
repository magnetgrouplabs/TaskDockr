using System.Text.Json.Serialization;
using System;

namespace TaskDockr.Models
{
    public class Shortcut
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("targetPath")]
        public string TargetPath { get; set; } = string.Empty;
        
        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = string.Empty;
        
        [JsonPropertyName("iconPath")]
        public string IconPath { get; set; } = string.Empty;
        
        [JsonPropertyName("iconGlyph")]
        public string IconGlyph { get; set; } = string.Empty;
        
        [JsonPropertyName("type")]
        public ShortcutType Type { get; set; } = ShortcutType.App;
    }

    public enum ShortcutType
    {
        [JsonPropertyName("app")]
        App,
        [JsonPropertyName("file")]
        File,
        [JsonPropertyName("url")]
        URL,
        [JsonPropertyName("folder")]
        Folder
    }
}