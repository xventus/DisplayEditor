using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using DisplayEditorApp.Services;
using DisplayEditorApp.Settings; 
using DisplayEditorApp.Views;
using System;
using System.Threading.Tasks;

namespace DisplayEditorApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Vytvoříme MainWindow hned a inicializaci provedeme asynchronně
            desktop.MainWindow = new MainWindow();
            
            // Asynchronní inicializace nastavení na pozadí
            _ = Task.Run(async () =>
            {
                try
                {
                    var configService = new ConfigurationService();
                    var config = await configService.LoadConfigAsync();
                    GlobalSettings.Initialize(configService, config);
                    System.Diagnostics.Debug.WriteLine("Configuration loaded successfully (async)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                    // Použijeme výchozí konfiguraci
                    var configService = new ConfigurationService();
                    GlobalSettings.Initialize(configService, new DisplayEditorApp.Models.AppConfig());
                }
            });
        }
     
        base.OnFrameworkInitializationCompleted();
    }
}
