using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TaskDockr.Models;

namespace TaskDockr.Services
{
    public class DragDropService
    {
        private readonly IGroupService _groupService;
        private readonly IShortcutService _shortcutService;
        private FrameworkElement? _draggedElement;
        private object? _draggedData;
        private Point _dragStartPosition;
        private bool _isDragging;

        public event EventHandler<DragDropEventArgs>? DragStarted;
        public event EventHandler<DragDropEventArgs>? DragCompleted;
        public event EventHandler<DragDropEventArgs>? DropCompleted;

        public DragDropService(IGroupService groupService, IShortcutService shortcutService)
        {
            _groupService    = groupService;
            _shortcutService = shortcutService;
        }

        public void EnableDragDropForGroup(FrameworkElement element, Group group)
        {
            element.MouseLeftButtonDown += OnGroupMouseDown;
            element.MouseMove           += OnGroupMouseMove;
            element.MouseLeftButtonUp   += OnGroupMouseUp;
            element.AllowDrop           = true;
            element.DragOver            += OnGroupDragOver;
            element.Drop                += OnGroupDrop;
        }

        public void EnableDragDropForShortcut(FrameworkElement element, Shortcut shortcut)
        {
            element.MouseLeftButtonDown += OnShortcutMouseDown;
            element.MouseMove           += OnShortcutMouseMove;
            element.MouseLeftButtonUp   += OnShortcutMouseUp;
            element.AllowDrop           = true;
            element.DragOver            += OnShortcutDragOver;
            element.Drop                += OnShortcutDrop;
        }

        // --- Group drag handlers ---

        private void OnGroupMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Group)
            {
                _draggedElement   = element;
                _draggedData      = element.DataContext;
                _dragStartPosition = e.GetPosition(element);
                _isDragging       = false;
            }
        }

        private void OnGroupMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedElement == null || _isDragging || e.LeftButton != MouseButtonState.Pressed) return;
            var current = e.GetPosition(_draggedElement);
            if (Math.Abs(current.X - _dragStartPosition.X) + Math.Abs(current.Y - _dragStartPosition.Y) < 10) return;

            _isDragging = true;
            StartDragVisual(_draggedElement);
            DragStarted?.Invoke(this, new DragDropEventArgs { Data = _draggedData!, Source = _draggedElement });

            var data = new DataObject("Group", _draggedData!);
            DragDrop.DoDragDrop(_draggedElement, data, DragDropEffects.Move);

            EndDragVisual(_draggedElement);
            DragCompleted?.Invoke(this, new DragDropEventArgs { Data = _draggedData!, Source = _draggedElement });
            ResetDragState();
        }

        private void OnGroupMouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetDragState();
        }

        private void OnGroupDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent("Group") ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private async void OnGroupDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("Group")) return;
            if (e.Data.GetData("Group") is not Group draggedGroup) return;
            if (sender is not FrameworkElement targetElement) return;
            if (targetElement.DataContext is not Group targetGroup) return;

            try
            {
                var groups = await _groupService.GetAllGroupsAsync();
                var targetIndex = groups.FindIndex(g => g.Id == targetGroup.Id);
                if (targetIndex >= 0)
                {
                    await _groupService.MoveGroupAsync(draggedGroup.Id, targetIndex);
                    DropCompleted?.Invoke(this, new DragDropEventArgs
                    {
                        Data   = draggedGroup,
                        Source = _draggedElement,
                        Target = targetElement
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Group drop failed: {ex.Message}");
            }
            e.Handled = true;
        }

        // --- Shortcut drag handlers ---

        private void OnShortcutMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Shortcut)
            {
                _draggedElement   = element;
                _draggedData      = element.DataContext;
                _dragStartPosition = e.GetPosition(element);
                _isDragging       = false;
            }
        }

        private void OnShortcutMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedElement == null || _isDragging || e.LeftButton != MouseButtonState.Pressed) return;
            var current = e.GetPosition(_draggedElement);
            if (Math.Abs(current.X - _dragStartPosition.X) + Math.Abs(current.Y - _dragStartPosition.Y) < 10) return;

            _isDragging = true;
            StartDragVisual(_draggedElement);
            DragStarted?.Invoke(this, new DragDropEventArgs { Data = _draggedData!, Source = _draggedElement });

            var data = new DataObject("Shortcut", _draggedData!);
            DragDrop.DoDragDrop(_draggedElement, data, DragDropEffects.Move);

            EndDragVisual(_draggedElement);
            DragCompleted?.Invoke(this, new DragDropEventArgs { Data = _draggedData!, Source = _draggedElement });
            ResetDragState();
        }

        private void OnShortcutMouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetDragState();
        }

        private void OnShortcutDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent("Shortcut") ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private async void OnShortcutDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("Shortcut")) return;
            if (e.Data.GetData("Shortcut") is not Shortcut draggedShortcut) return;
            if (sender is not FrameworkElement targetElement) return;

            try
            {
                int targetPosition = GetDropIndex(targetElement);
                if (targetPosition >= 0)
                {
                    var groups = await _groupService.GetAllGroupsAsync();
                    foreach (var group in groups)
                    {
                        if (group.Shortcuts?.Exists(s => s.Id == draggedShortcut.Id) == true)
                        {
                            await _shortcutService.MoveShortcutAsync(group.Id, draggedShortcut.Id, targetPosition);
                            DropCompleted?.Invoke(this, new DragDropEventArgs
                            {
                                Data   = draggedShortcut,
                                Source = _draggedElement,
                                Target = targetElement
                            });
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shortcut drop failed: {ex.Message}");
            }
            e.Handled = true;
        }

        private int GetDropIndex(FrameworkElement target)
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(target);
            while (parent != null && parent is not ItemsControl)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is ItemsControl itemsControl)
            {
                for (int i = 0; i < itemsControl.Items.Count; i++)
                    if (itemsControl.Items[i] == target.DataContext)
                        return i;
            }
            return -1;
        }

        private void StartDragVisual(FrameworkElement element)
        {
            element.Opacity         = 0.7;
            element.RenderTransform = new ScaleTransform(1.05, 1.05);
            element.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void EndDragVisual(FrameworkElement element)
        {
            element.Opacity         = 1.0;
            element.RenderTransform = Transform.Identity;
        }

        private void ResetDragState()
        {
            _draggedElement = null;
            _draggedData    = null;
            _isDragging     = false;
        }
    }

    public class DragDropEventArgs : EventArgs
    {
        public object Data { get; set; } = null!;
        public FrameworkElement? Source { get; set; }
        public FrameworkElement? Target { get; set; }
    }
}
