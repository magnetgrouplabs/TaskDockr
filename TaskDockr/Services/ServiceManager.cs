using Microsoft.Extensions.DependencyInjection;
using System;
using TaskDockr.ViewModels;

namespace TaskDockr.Services
{
    public static class ServiceManager
    {
        public static IServiceCollection AddTaskDockrServices(this IServiceCollection services)
        {
            services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IconService>();
            services.AddSingleton<EnhancedIconService>();
            services.AddSingleton<IIconService>(provider => provider.GetRequiredService<EnhancedIconService>());
            services.AddSingleton<ITaskbarService, TaskbarService>();
            services.AddSingleton<IGroupService, GroupService>();
            services.AddSingleton<IShortcutService, ShortcutService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IWindowManagerService, WindowManagerService>();
            services.AddSingleton<IPopoutService, PopoutService>();
            services.AddSingleton<DragDropService>();
            services.AddSingleton<IStartupService, StartupService>();
            services.AddSingleton<ITaskbarIntegrationService, TaskbarIntegrationService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IIconCatalogService, IconCatalogService>();
            services.AddSingleton<IMemoryOptimizationService, MemoryOptimizationService>();
            services.AddSingleton<MainViewModel>();

            return services;
        }

public static T? GetService<T>() where T : class
        => App.GetService<T>();
    }
}
