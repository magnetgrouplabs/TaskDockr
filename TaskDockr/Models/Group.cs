using System.Text.Json.Serialization;
using System.Collections.Generic;
using System;

namespace TaskDockr.Models
{
    public class Group
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("iconPath")]
        public string IconPath { get; set; } = string.Empty;

        [JsonPropertyName("iconGlyph")]
        public string IconGlyph { get; set; } = string.Empty;

        [JsonPropertyName("iconColor")]
        public string IconColor { get; set; } = string.Empty;

        [JsonPropertyName("shortcuts")]
        public List<Shortcut> Shortcuts { get; set; } = new List<Shortcut>();
        
        [JsonPropertyName("position")]
        public int Position { get; set; }
    }
}