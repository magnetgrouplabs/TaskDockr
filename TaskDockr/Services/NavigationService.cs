using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TaskDockr.Services
{
    public enum NavigationTarget
    {
        MainWindow,
        PopoutWindow,
        Settings,
        GroupManagement,
        GroupCreation,
        GroupEdit,
        ShortcutManagement
    }

    public class NavigationEventArgs : EventArgs
    {
        public NavigationTarget Target { get; set; }
        public object? Parameter { get; set; }
        public bool IsBackNavigation { get; set; }
    }

    public interface INavigationService
    {
        event EventHandler<NavigationEventArgs> Navigating;
        event EventHandler<NavigationEventArgs> Navigated;

        Task<bool> NavigateToAsync(NavigationTarget target, object? parameter = null);
        Task<bool> NavigateBackAsync();
        bool CanNavigateBack { get; }

        void RegisterContentControl(ContentControl contentControl);
        void RegisterWindow(Window window, NavigationTarget target);

        NavigationTarget CurrentTarget { get; }
        Stack<NavigationTarget> NavigationHistory { get; }
    }

    public class NavigationService : INavigationService
    {
        private ContentControl? _contentControl;
        private readonly Dictionary<NavigationTarget, Window> _windows = new();
        private readonly Stack<NavigationTarget> _navigationHistory = new();

        public event EventHandler<NavigationEventArgs>? Navigating;
        public event EventHandler<NavigationEventArgs>? Navigated;

        public NavigationTarget CurrentTarget { get; private set; } = NavigationTarget.MainWindow;
        public Stack<NavigationTarget> NavigationHistory => new(_navigationHistory);
        public bool CanNavigateBack => _navigationHistory.Count > 0;

        public void RegisterContentControl(ContentControl contentControl)
        {
            _contentControl = contentControl;
        }

        public void RegisterWindow(Window window, NavigationTarget target)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            _windows[target] = window;
        }

        public async Task<bool> NavigateToAsync(NavigationTarget target, object? parameter = null)
        {
            var args = new NavigationEventArgs { Target = target, Parameter = parameter };
            Navigating?.Invoke(this, args);

            try
            {
                bool success = false;

                if (_windows.TryGetValue(target, out var window))
                {
                    window.Show();
                    window.Activate();
                    success = true;
                }
                else if (target is NavigationTarget.GroupCreation or NavigationTarget.GroupEdit)
                {
                    success = await HandleDialogNavigationAsync(target, parameter);
                }
                else if (_contentControl != null)
                {
                    var view = CreateViewForTarget(target, parameter);
                    if (view != null)
                    {
                        _contentControl.Content = view;
                        success = true;
                    }
                }

                if (success)
                {
                    if (CurrentTarget != target)
                    {
                        _navigationHistory.Push(CurrentTarget);
                        CurrentTarget = target;
                    }
                    Navigated?.Invoke(this, args);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
            }

            return false;
        }

        public Task<bool> NavigateBackAsync()
        {
            if (!CanNavigateBack) return Task.FromResult(false);

            var previousTarget = _navigationHistory.Pop();
            var args = new NavigationEventArgs { Target = previousTarget, IsBackNavigation = true };
            Navigating?.Invoke(this, args);

            try
            {
                if (_windows.TryGetValue(previousTarget, out var window))
                {
                    window.Activate();
                    CurrentTarget = previousTarget;
                    Navigated?.Invoke(this, args);
                    return Task.FromResult(true);
                }

                if (_contentControl != null)
                {
                    var view = CreateViewForTarget(previousTarget, null);
                    if (view != null)
                    {
                        _contentControl.Content = view;
                        CurrentTarget = previousTarget;
                        Navigated?.Invoke(this, args);
                        return Task.FromResult(true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Back navigation failed: {ex.Message}");
            }

            return Task.FromResult(false);
        }

        private object? CreateViewForTarget(NavigationTarget target, object? parameter)
        {
            var mainVm = App.GetService<ViewModels.MainViewModel>();
            return target switch
            {
                NavigationTarget.Settings        => new Views.SettingsView(),
                NavigationTarget.GroupManagement => new Views.GroupManagementView { DataContext = mainVm },
                NavigationTarget.ShortcutManagement => parameter is Models.Group g
                    ? new Views.ShortcutManagementView(g) { DataContext = mainVm }
                    : new Views.ShortcutManagementView()  { DataContext = mainVm },
                _ => null
            };
        }

        private Task<bool> HandleDialogNavigationAsync(NavigationTarget target, object? parameter)
        {
            bool result = false;
            switch (target)
            {
                case NavigationTarget.GroupCreation:
                    var creationForm = new Views.GroupCreationForm();
                    result = creationForm.ShowDialog() == true;
                    break;

                case NavigationTarget.GroupEdit:
                    if (parameter is Models.Group group)
                    {
                        var editForm = new Views.GroupEditForm(group);
                        result = editForm.ShowDialog() == true;
                    }
                    break;
            }
            return Task.FromResult(result);
        }

        public void ClearHistory() => _navigationHistory.Clear();
    }
}
