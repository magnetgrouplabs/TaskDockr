using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TaskDockr.Utils
{
    public static class DragDropHelper
    {
        public static void ApplyDragVisual(FrameworkElement element)
        {
            if (element == null) return;
            element.Opacity = 0.7;
            var scale = new ScaleTransform(1.05, 1.05);
            element.RenderTransform       = scale;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        public static void RemoveDragVisual(FrameworkElement element)
        {
            if (element == null) return;
            element.Opacity         = 1.0;
            element.RenderTransform = Transform.Identity;
        }

        public static void ApplyDropZoneVisual(FrameworkElement element, bool isActive = true)
        {
            if (element == null) return;
            if (isActive)
            {
                if (element is Border border)
                {
                    border.BorderBrush     = SystemColors.HighlightBrush;
                    border.BorderThickness = new Thickness(2);
                }
                else if (element is Panel panel)
                {
                    panel.Background = new SolidColorBrush(SystemColors.HighlightColor) { Opacity = 0.1 };
                }
            }
            else
            {
                if (element is Border border)
                {
                    border.BorderBrush     = null;
                    border.BorderThickness = new Thickness(1);
                }
                else if (element is Panel panel)
                {
                    panel.Background = null;
                }
            }
        }

        public static void CreateDropIndicator(FrameworkElement container, int insertIndex)
        {
            if (container is not Panel panel) return;
            RemoveDropIndicator(container);
            if (insertIndex < 0 || insertIndex > panel.Children.Count) return;

            var indicator = new Border
            {
                Name                = "DropIndicator",
                Background          = SystemColors.HighlightBrush,
                Height              = 2,
                Margin              = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Opacity             = 0
            };

            panel.Children.Insert(insertIndex, indicator);

            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            indicator.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        public static void RemoveDropIndicator(FrameworkElement container)
        {
            if (container is not Panel panel) return;
            var indicators = panel.Children.OfType<Border>()
                .Where(b => b.Name == "DropIndicator").ToList();
            foreach (var indicator in indicators)
                panel.Children.Remove(indicator);
        }

        public static double GetDragThreshold() => 10;
    }
}
