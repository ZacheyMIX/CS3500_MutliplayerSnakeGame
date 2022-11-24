using Plugin.Maui.Audio;

namespace SnakeGame;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // added to use sounds. Uses the installed Plugin.Maui.Audio package
        builder.Services.AddSingleton(AudioManager.Current);

        return builder.Build();
    }
}

