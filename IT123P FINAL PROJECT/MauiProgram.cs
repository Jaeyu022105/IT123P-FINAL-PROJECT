using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using IT123P_FINAL_PROJECT.Services;
using IT123P_FINAL_PROJECT.ViewModels;
using IT123P_FINAL_PROJECT.Views;

namespace IT123P_FINAL_PROJECT
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

            // Register ViewModels
            builder.Services.AddTransient<DietSummaryViewModel>();
            builder.Services.AddTransient<CameraViewModel>();
            builder.Services.AddTransient<PortionViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            // Register Views
            builder.Services.AddTransient<DietSummaryPage>();
            builder.Services.AddTransient<CameraPage>();
            builder.Services.AddTransient<PortionPage>();
            builder.Services.AddTransient<SettingsPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
