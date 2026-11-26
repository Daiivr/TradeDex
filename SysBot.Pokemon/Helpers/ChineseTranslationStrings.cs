using System;
using System.IO;
using System.Text.Json;

namespace SysBot.Pokemon;

/// <summary>Chinese lookup tables used by the legacy Chinese-to-Showdown translator.</summary>
internal sealed class ChineseTranslationStrings
{
    public string[] Species { get; } = Load("Species");
    public string[] Item { get; } = Load("Item");
    public string[] balllist { get; } = Load("balllist");
    public string[] Ability { get; } = Load("Ability");
    public string[] Natures { get; } = Load("Natures");
    public string[] Types { get; } = Load("Types");
    public string[] Move { get; } = Load("Move");

    private static string[] Load(string name)
    {
        using var stream = typeof(ChineseTranslationStrings).Assembly.GetManifestResourceStream($"SysBot.Pokemon.Resources.ChineseTranslation.{name}.json")
            ?? throw new InvalidOperationException($"Missing Chinese translation resource: {name}");
        return JsonSerializer.Deserialize<string[]>(stream)
            ?? throw new InvalidDataException($"Invalid Chinese translation resource: {name}");
    }
}
