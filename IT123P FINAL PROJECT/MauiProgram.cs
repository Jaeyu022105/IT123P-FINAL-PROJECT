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

            // DI services
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

            // Base API url config for backend
            builder.Services.AddHttpClient<IApiService, ApiService>(client =>
            {
#if ANDROID
                // Android emulator localhost translation
                client.BaseAddress = new Uri("http://10.0.2.2:5000/");
#else
                // Real server address goes here
                client.BaseAddress = new Uri("https://localhost:5001/");
#endif
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            // ViewModels
            builder.Services.AddTransient<DietSummaryViewModel>();
            builder.Services.AddTransient<CameraViewModel>();
            builder.Services.AddTransient<PortionViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<HistoryViewModel>();
            builder.Services.AddTransient<ChatViewModel>();

            // Views
            builder.Services.AddTransient<DietSummaryPage>();
            builder.Services.AddTransient<CameraPage>();
            builder.Services.AddTransient<PortionPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddTransient<ChatPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
