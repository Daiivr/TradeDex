using FluentAssertions;
using SysBot.Pokemon.Localization;
using Xunit;

namespace SysBot.Tests;

public class AppLocalizationTests
{
    [Theory]
    [InlineData("Re-Download Latest Version", "Volver a descargar la última versión")]
    [InlineData("You are on the latest version. You can re-download if needed.", "Ya tienes la versión más reciente. Puedes volver a descargarla si lo necesitas.")]
    [InlineData("Download Fonts", "Descargar fuentes")]
    [InlineData("Do not display a link to Download Fonts again", "No volver a mostrar el enlace para descargar fuentes")]
    [InlineData("No restart in progress", "No hay reinicio en progreso")]
    [InlineData("Update process started in background. Use /api/bot/update/active to check status.", "Proceso de actualización iniciado en segundo plano. Usa /api/bot/update/active para revisar el estado.")]
    [InlineData("Unable to fetch changelog from GitHub repository.", "No se pudo obtener el changelog desde el repositorio de GitHub.")]
    public void RuntimeMessagesUseSelectedSpanishLanguage(string english, string spanish)
    {
        AppLocalization.SetLanguage(AppLanguage.Spanish);

        AppLocalization.LocalizeRuntimeMessage(english).Should().Be(spanish);
    }

    [Fact]
    public void NewReleaseBannerUsesSelectedLanguage()
    {
        AppLocalization.SetLanguage(AppLanguage.English);
        AppLocalization.Get(LocalizationKeys.BotsNewRelease).Should().Be("NEW RELEASE!");

        AppLocalization.SetLanguage(AppLanguage.Spanish);
        AppLocalization.Get(LocalizationKeys.BotsNewRelease).Should().Be("NUEVA VERSION!");
    }
}
