using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Windows;

namespace SMWYG
{
    public partial class App : Application
    {
        // Make services and configuration available everywhere
        public static IServiceProvider Services { get; private set; } = null!;
        public static IConfiguration Configuration { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Build configuration from appsettings.json
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. Set up Dependency Injection
            var services = new ServiceCollection();

            // Register configuration so it can be injected
            services.AddSingleton<IConfiguration>(Configuration);

            // Register DbContext using the connection string from config
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            // Register your ViewModels and Windows
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddTransient<LoginWindow>();

            // Build and store the service provider
            Services = services.BuildServiceProvider();

            // Show the login window first
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var login = Services.GetRequiredService<LoginWindow>();
            bool? result = login.ShowDialog();
            if (result == true && login.SignedInUser != null)
            {
                // Sign in
                var mainVm = Services.GetRequiredService<MainViewModel>();
                mainVm.SignIn(login.SignedInUser);

                var mainWindow = Services.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }
    }
}