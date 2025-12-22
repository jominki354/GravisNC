using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GCode.Core.Services;
using GCode.Modules.FileIO;
using GCode.Modules.Settings;
using GCode.App.WPF.Commands;

namespace GCode.App.WPF
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register Services
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDialogService, GCode.App.WPF.Services.ModernDialogService>();
            services.AddSingleton<ISettingsService, GCode.Modules.Settings.SettingsService>();

            // Register Main Window
            services.AddTransient<MainWindow>();
            
            // Register Command Handler (if needed as a service, though currently it's instantiated by MainWindow)
            // services.AddTransient<EditorCommandHandler>(); 
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 전역 예외 처리
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                LogCrash((Exception)args.ExceptionObject);
            
            DispatcherUnhandledException += (s, args) => 
            {
                LogCrash(args.Exception);
                args.Handled = true;
            };

            try 
            {
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                LogCrash(ex);
                MessageBox.Show($"Startup Error: {ex.Message}");
                Shutdown();
            }
        }

        private void LogCrash(Exception ex)
        {
            string log = $"[{DateTime.Now}] CRIITICAL ERROR:\n{ex}\n\n";
            File.AppendAllText("crash.log", log);
        }
    }
}
