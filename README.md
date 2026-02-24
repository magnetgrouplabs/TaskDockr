# TaskDockr

A modern task management application built with WinUI 3 and Windows App SDK.

## Project Structure

```
TaskDockr/
├── TaskDockr.sln                 # Solution file
└── TaskDockr/                    # Main project
    ├── Models/                    # Data models
    │   └── TaskItem.cs
    ├── ViewModels/               # ViewModels for MVVM pattern
    │   └── MainViewModel.cs
    ├── Views/                    # UI Views
    │   ├── App.xaml
    │   ├── App.xaml.cs
    │   ├── MainWindow.xaml
    │   └── MainWindow.xaml.cs
    ├── Services/                 # Business logic services
    │   └── TaskService.cs
    └── Utils/                    # Utility classes
        └── ObservableObject.cs
```

## Prerequisites

- Windows 10 version 1809 (build 17763) or later
- .NET 8.0 SDK
- Windows App SDK 1.5
- Visual Studio 2022 with Windows App SDK extension

## Setup Instructions

1. **Install .NET 8.0 SDK**
   Download from: https://dotnet.microsoft.com/download/dotnet/8.0

2. **Install Windows App SDK**
   Download from: https://aka.ms/windowsappsdk

3. **Open in Visual Studio**
   - Open `TaskDockr.sln` in Visual Studio 2022
   - Build the solution (Ctrl+Shift+B)
   - Run the application (F5)

## Features

- MVVM architecture pattern
- Modern Windows 11 UI design
- Task management (add, delete, mark complete)
- Data persistence using JSON
- Observable collections for real-time UI updates

## Development

The project follows MVVM pattern:
- **Models**: Data structures (TaskItem)
- **ViewModels**: Business logic and data binding
- **Views**: XAML UI definitions
- **Services**: Data persistence and external operations
- **Utils**: Reusable utility classes

## Building

```bash
# Restore packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

## Architecture

- **MVVM Pattern**: Clean separation of UI and business logic
- **Data Binding**: Two-way binding between UI and ViewModels
- **Dependency Injection**: Services injected where needed
- **Async/Await**: Non-blocking operations for better UX

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request