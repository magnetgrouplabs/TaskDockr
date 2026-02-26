using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskDockr.Utils
{
    public static class FluentIconHelper
    {
        public static ImageSource CreateFluentIcon(string symbol, SolidColorBrush foreground, int size = 32)
        {
            // Stub; return a solid-color placeholder
            var drawing = new GeometryDrawing(foreground, null,
                new RectangleGeometry(new System.Windows.Rect(0, 0, size, size)));
            var image = new DrawingImage(drawing);
            image.Freeze();
            return image;
        }

        public static ImageSource? RenderSvg(string svgContent, int size = 32) => null;

        public static SolidColorBrush GetFluentColor(FluentColor color) => color switch
        {
            FluentColor.Accent  => new SolidColorBrush(Color.FromArgb(255, 0,   120, 215)),
            FluentColor.Success => new SolidColorBrush(Color.FromArgb(255, 16,  124, 16)),
            FluentColor.Warning => new SolidColorBrush(Color.FromArgb(255, 255, 185, 0)),
            FluentColor.Error   => new SolidColorBrush(Color.FromArgb(255, 197, 15,  31)),
            _                   => new SolidColorBrush(Color.FromArgb(255, 118, 118, 118))
        };
    }

    public enum FluentColor { Accent, Success, Warning, Error, Neutral }
}
