private static readonly IHost _host = Host
    .CreateDefaultBuilder()
    .ConfigureLogging(logging =>
    {
        // Clear all default providers to remove EventLog provider
        logging.ClearProviders();
        
        // Add only safe providers
        logging.AddConsole();
        logging.AddDebug();
        
        // Set minimum log level to reduce noise
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureAppConfiguration(config =>
    {
        config.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)!);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddNavigationViewPageProvider();
        services.AddHostedService<ApplicationHostService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ITaskBarService, TaskBarService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INavigationWindow, MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<DashboardPage>();
        services.AddSingleton<DashboardViewModel>();

        services.AddSingleton<DeploymentPage>();
        services.AddSingleton<DataViewModel>();

        services.AddSingleton<SettingsPage>();
        services.AddSingleton<SettingsViewModel>();

        services.AddSingleton<AboutPage>();
        services.AddSingleton<AboutViewModel>();

        services.AddSingleton<LaunchPage>();
        services.AddSingleton<LaunchPageViewModel>();

        services.AddSingleton<UninstallPage>();
        services.AddSingleton<UninstallViewModel>();

        services.AddSingleton<InstallViewModel>();

        services.AddSingleton<ModsPage>();
        services.AddSingleton<ModsViewModel>();

        services.AddSingleton<FastFlagsViewModel>();
        services.AddSingleton<FastFlagsPage>();

        services.AddSingleton<EditorViewModel>();
        services.AddSingleton<FastFlagEditor>();

        services.AddSingleton<RobloxVersionsViewModel>();
        services.AddSingleton<VersionsPage>();

        services.AddSingleton<TweaksPage>();
        services.AddSingleton<TweaksViewModel>();

        services.AddSingleton<PluginsViewModel>();
        services.AddSingleton<PluginsPage>();
    })
    .Build();