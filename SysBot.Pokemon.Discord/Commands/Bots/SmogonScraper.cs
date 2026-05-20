using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using HtmlAgilityPack;
using PKHeX.Core;
using System.Collections.Generic;
using SysBot.Pokemon.Localization;

namespace SysBot.Pokemon.Discord.Commands.Bots
{
    public class SmogonScraper : ModuleBase<SocketCommandContext>
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly Random random = new Random();

        [Command("smogon")]
        [Summary("Fetches a Smogon set for the specified Pokémon and game.")]
        public async Task ScrapeSmogonSet(string pokemon, string game)
        {
            try
            {
                var set = await GetSmogonSet(pokemon, game);
                if (string.IsNullOrEmpty(set))
                {
                    await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordSmogonNoSet, pokemon, game)).ConfigureAwait(false);
                    return;
                }

                await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordSmogonSet, pokemon, game, set)).ConfigureAwait(false);

                var pkmn = GeneratePKMFromSmogonSet(set, game);
                if (pkmn != null)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordSmogonGenerateSuccess)).ConfigureAwait(false);
                    // Add your code here to handle the generated PKM object
                }
                else
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordSmogonGenerateFailed)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(AppLocalization.Format(LocalizationKeys.LogSmogonScrapeError, ex.Message));
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordSmogonRequestError)).ConfigureAwait(false);
            }
        }

        private static async Task<string?> GetSmogonSet(string pokemon, string game)
        {
            var url = $"https://www.smogon.com/dex/{game}/pokemon/{pokemon}/";
            try
            {
                var response = await client.GetStringAsync(url).ConfigureAwait(false);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var setNodes = doc.DocumentNode.SelectNodes("//pre[contains(@class, 'tooltip-content')]");
                if (setNodes == null || setNodes.Count == 0)
                    return null;

                var sets = setNodes.Select(node => node.InnerText.Trim()).ToList();
                var randomSet = sets[random.Next(sets.Count)];
                return randomSet;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(AppLocalization.Format(LocalizationKeys.LogSmogonHttpError, ex.Message));
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(AppLocalization.Format(LocalizationKeys.LogSmogonGenericError, ex.Message));
                return null;
            }
        }

        private static PKM? GeneratePKMFromSmogonSet(string set, string game)
        {
            var species = ExtractSpeciesFromSet(set);
            if (string.IsNullOrEmpty(species))
                return null;

            PKM pk;
            switch (game.ToLower())
            {
                case "swsh":
                    pk = new PK8();
                    break;
                case "sv":
                    pk = new PK9();
                    break;
                case "bdsp":
                    pk = new PB8();
                    break;
                case "pla":
                    pk = new PA8();
                    break;
                case "lgpe":
                    pk = new PB7();
                    break;
                default:
                    return null;
            }

            var speciesIndex = GameInfo.GetStrings(game).Species
                .Select((item, index) => new { item, index })
                .FirstOrDefault(x => x.item.Equals(species, StringComparison.OrdinalIgnoreCase))?.index ?? -1;
            if (speciesIndex < 0)
                return null;

            pk.Species = (ushort)speciesIndex;
            return pk;
        }

        private static string? ExtractSpeciesFromSet(string set)
        {
            var lines = set.Split('\n');
            return lines.Length > 0 ? lines[0].Split('@')[0].Trim() : null;
        }
    }
}
