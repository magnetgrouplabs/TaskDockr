using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.ViewModels;

namespace TaskDockr.Views
{
 public partial class ShortcutManagementView : UserControl
 {
 private Border? _dragOverlay;
 private bool _isExternalDragOver;
 private Storyboard? _dragEnterAnimation;
 private Storyboard? _dragLeaveAnimation;
 private readonly DragDropService _dragDropService;

private Group? _pendingGroup;

    public ShortcutManagementView() : this(null) { }

    public ShortcutManagementView(Group? group)
    {
        _pendingGroup = group;
        InitializeComponent();
        _dragDropService = App.GetService<DragDropService>();
        InitializeAnimations();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_pendingGroup != null && DataContext is MainViewModel vm)
        {
            vm.SelectedGroup = _pendingGroup;
            _pendingGroup = null;
        }
    }

 private void OnLoaded(object sender, RoutedEventArgs e)
 {
 SetupDragDrop();
 }

        private void InitializeAnimations()
        {
            // Drag enter animation - fade in and scale up
            _dragEnterAnimation = new Storyboard();
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            var scaleX = new DoubleAnimation(0.95, 1, TimeSpan.FromMilliseconds(200));
            var scaleY = new DoubleAnimation(0.95, 1, TimeSpan.FromMilliseconds(200));

            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("RenderTransform.ScaleY"));

            _dragEnterAnimation.Children.Add(fadeIn);
            _dragEnterAnimation.Children.Add(scaleX);
            _dragEnterAnimation.Children.Add(scaleY);

            // Drag leave animation - fade out
            _dragLeaveAnimation = new Storyboard();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
            _dragLeaveAnimation.Children.Add(fadeOut);
            _dragLeaveAnimation.Completed += (s, e) =>
            {
                if (_dragOverlay != null)
                    _dragOverlay.Visibility = Visibility.Collapsed;
            };
        }

private void SetupDragDrop()
 {
 // Set up UserControl-level drag-drop for external items
 DragEnter += OnExternalDragEnter;
 DragOver += OnExternalDragOver;
 DragLeave += OnExternalDragLeave;
 Drop += OnExternalDrop;

 // Also set up on the ListBox itself (if it exists)
 if (ShortcutsListBox != null)
 {
 ShortcutsListBox.DragEnter += OnExternalDragEnter;
 ShortcutsListBox.DragOver += OnExternalDragOver;
 ShortcutsListBox.DragLeave += OnExternalDragLeave;
 ShortcutsListBox.Drop += OnExternalDrop;
 }
 }

        private void OnExternalDragEnter(object sender, DragEventArgs e)
        {
            if (!IsExternalDrag(e))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            _isExternalDragOver = true;
            ShowDragOverlay();
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void OnExternalDragOver(object sender, DragEventArgs e)
        {
            if (!IsExternalDrag(e))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void OnExternalDragLeave(object sender, DragEventArgs e)
        {
            // Check if we're actually leaving the element (not just entering a child)
            var dependencyObject = sender as DependencyObject;
            if (dependencyObject != null && !IsMouseOverElement(dependencyObject))
            {
                _isExternalDragOver = false;
                HideDragOverlay();
            }
        }

        private bool IsMouseOverElement(DependencyObject element)
        {
            var pos = Mouse.GetPosition(this);
            var hitTestResult = VisualTreeHelper.HitTest(this, pos);
            if (hitTestResult == null) return false;

            // Walk up the tree to see if we're over the element
            DependencyObject? current = hitTestResult.VisualHit as DependencyObject;
            while (current != null)
            {
                if (current == element)
                    return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void OnExternalDrop(object sender, DragEventArgs e)
        {
            _isExternalDragOver = false;
            HideDragOverlay();

            if (DataContext is not MainViewModel vm || vm.SelectedGroup == null)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                {
                    foreach (var file in files)
                    {
                        CreateShortcutFromFile(file, vm.SelectedGroup);
                    }
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                var text = e.Data.GetData(DataFormats.Text)?.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    CreateShortcutFromText(text, vm.SelectedGroup);
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                var text = e.Data.GetData(DataFormats.UnicodeText)?.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    CreateShortcutFromText(text, vm.SelectedGroup);
                }
            }

            e.Handled = true;
        }

        private bool IsExternalDrag(DragEventArgs e)
        {
            // Check for file drop
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                return true;

            // Check for text/URL drop
            if (e.Data.GetDataPresent(DataFormats.Text))
                return true;

            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
                return true;

            // Check for URL drop
            if (e.Data.GetDataPresent("UniformResourceLocator"))
                return true;

            return false;
        }

        private void ShowDragOverlay()
        {
            if (_dragOverlay == null)
            {
                // Find the overlay in the visual tree
                _dragOverlay = FindName("DragOverlay") as Border;
                if (_dragOverlay == null) return;

                // Set up transform for animation
                _dragOverlay.RenderTransform = new ScaleTransform(1, 1);
                _dragOverlay.RenderTransformOrigin = new Point(0.5, 0.5);

                // Update animation targets
                foreach (var child in _dragEnterAnimation?.Children ?? new TimelineCollection())
                {
                    Storyboard.SetTarget(child, _dragOverlay);
                }
                foreach (var child in _dragLeaveAnimation?.Children ?? new TimelineCollection())
                {
                    Storyboard.SetTarget(child, _dragOverlay);
                }
            }

            _dragOverlay.Opacity = 0;
            _dragOverlay.Visibility = Visibility.Visible;
            _dragEnterAnimation?.Begin(_dragOverlay);
        }

        private void HideDragOverlay()
        {
            if (_dragOverlay != null)
            {
                _dragLeaveAnimation?.Begin(_dragOverlay);
            }
        }

        private void CreateShortcutFromFile(string filePath, Group group)
        {
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
                return;

            var shortcutType = DetectShortcutType(filePath);
            var name = GetNameFromPath(filePath, shortcutType);
            var iconPath = GetIconPathForFile(filePath, shortcutType);

            // Open creation form with pre-populated data
            var form = new ShortcutCreationForm(group);
            if (form.DataContext is ViewModels.ShortcutFormViewModel viewModel)
            {
                viewModel.TargetPath = filePath;
                viewModel.ShortcutName = name;
                viewModel.IconPath = iconPath ?? string.Empty;
                viewModel.SelectedType = shortcutType;
            }

            if (form.ShowDialog() == true)
            {
                // Refresh the view
                if (DataContext is MainViewModel vm)
                {
                    _ = vm.LoadGroupsAsync();
                }
            }
        }

        private void CreateShortcutFromText(string text, Group group)
        {
            // Try to detect if it's a URL
            var shortcutType = DetectShortcutType(text);
            var name = GetNameFromText(text, shortcutType);

            // Open creation form with pre-populated data
            var form = new ShortcutCreationForm(group);
            if (form.DataContext is ViewModels.ShortcutFormViewModel viewModel)
            {
                viewModel.TargetPath = text.Trim();
                viewModel.ShortcutName = name;
                viewModel.SelectedType = shortcutType;
            }

            if (form.ShowDialog() == true)
            {
                // Refresh the view
                if (DataContext is MainViewModel vm)
                {
                    _ = vm.LoadGroupsAsync();
                }
            }
        }

        private ShortcutType DetectShortcutType(string path)
        {
            // Check if it's a URL
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps ||
                    uri.Scheme == Uri.UriSchemeFtp ||
                    uri.Scheme == Uri.UriSchemeMailto)
                {
                    return ShortcutType.URL;
                }
            }

            // Check if it looks like a URL without scheme
            if (path.Contains("://") ||
                path.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ||
                (!Path.HasExtension(path) && path.Contains(".")))
            {
                return ShortcutType.URL;
            }

            // Check if it's a file or folder
            if (Directory.Exists(path))
                return ShortcutType.Folder;

            if (File.Exists(path))
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".exe" || ext == ".com" || ext == ".bat" ||
                    ext == ".cmd" || ext == ".msi" || ext == ".ps1")
                {
                    return ShortcutType.App;
                }
                return ShortcutType.File;
            }

            // Default to App if we can't determine
            return ShortcutType.App;
        }

        private string GetNameFromPath(string path, ShortcutType type)
        {
            switch (type)
            {
                case ShortcutType.Folder:
                    return Path.GetFileName(path) ?? Path.GetFileNameWithoutExtension(path) ?? "New Folder";
                case ShortcutType.File:
                case ShortcutType.App:
                    return Path.GetFileNameWithoutExtension(path) ?? "New Shortcut";
                default:
                    return "New Shortcut";
            }
        }

        private string GetNameFromText(string text, ShortcutType type)
        {
            if (type == ShortcutType.URL)
            {
                // Try to extract a friendly name from URL
                try
                {
                    var uri = new Uri(text);
                    var host = uri.Host.Replace("www.", "");
                    var segments = uri.Segments;
                    if (segments.Length > 1 && !string.IsNullOrWhiteSpace(segments.Last()))
                    {
                        return $"{host} - {Path.GetFileNameWithoutExtension(segments.Last())}";
                    }
                    return host;
                }
                catch
                {
                    return text.Trim();
                }
            }

            return text.Trim();
        }

        private string? GetIconPathForFile(string filePath, ShortcutType type)
        {
            if (type == ShortcutType.App && File.Exists(filePath))
            {
                // For executables, use the file itself as the icon source
                return filePath;
            }

            if (type == ShortcutType.File && File.Exists(filePath))
            {
                // For files, try to use the file itself if it's an image
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico")
                {
                    return filePath;
                }
            }

            return null;
        }

private void OnBackClick(object sender, RoutedEventArgs e)
 {
 App.GetService<INavigationService>().NavigateBackAsync();
 }

 /// <summary>
 /// Handles ListBoxItem loaded event to enable internal drag-drop reordering
 /// </summary>
 private void OnListBoxItemLoaded(object sender, RoutedEventArgs e)
 {
 if (sender is ListBoxItem item && item.DataContext is Shortcut shortcut)
 {
 _dragDropService.EnableDragDropForShortcut(item, shortcut);
 }
 }
 }
}
