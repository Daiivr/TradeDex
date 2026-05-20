using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.Discord.Helpers;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace SysBot.Pokemon.Discord.Modules
{
    public class HOMEReadyModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private static T? GetRequest(Download<PKM> dl)
        {
            if (!dl.Success)
                return null;
            return dl.Data switch
            {
                null => null,
                T pk => pk,
                _ => EntityConverter.ConvertToType(dl.Data, typeof(T), out _) as T,
            };
        }

        private string HOMEFolder => SysCord<T>.Runner.Config.Folder.HOMEReadyPKMFolder;

        private string Prefix => SysCord<T>.Runner.Config.Discord.CommandPrefix;

        // ============================================================================
        //  INSTRUCTIONS
        // ============================================================================
        [Command("homeready")]
        [Alias("hr")]
        [Summary("Displays instructions on how to use the HOME-Ready module.")]
        private async Task HomeReadyInstructionsAsync()
        {
            if (string.IsNullOrWhiteSpace(HOMEFolder))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyNotConfigured)).ConfigureAwait(false);
                return;
            }

            // Using your modern embed style
            async Task<IUserMessage> SendBreak(string title, string description)
            {
                var embed = new EmbedBuilder()
                    .WithTitle(title)
                    .WithDescription(description)
                    .WithColor(Color.Blue)
                    .WithImageUrl("https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/homereadybreak.png");

                return await ReplyAsync(embed: embed.Build());
            }

            var m0 = await SendBreak(
                AppLocalization.Get(LocalizationKeys.DiscordHomeReadyInstructionsTitle),
                AppLocalization.Get(LocalizationKeys.DiscordHomeReadyInstructionsDescription)
            );

            var m1 = await SendBreak(
                AppLocalization.Format(LocalizationKeys.DiscordHomeReadyGetListTitle, Prefix),
                AppLocalization.Format(LocalizationKeys.DiscordHomeReadyGetListDescription, Prefix)
            );

            var m2 = await SendBreak(
                AppLocalization.Format(LocalizationKeys.DiscordHomeReadyChangePagesTitle, Prefix),
                AppLocalization.Format(LocalizationKeys.DiscordHomeReadyChangePagesDescription, Prefix)
            );

            var m3 = await SendBreak(
                AppLocalization.Format(LocalizationKeys.DiscordHomeReadyTradeFileTitle, Prefix),
                AppLocalization.Format(LocalizationKeys.DiscordHomeReadyTradeFileDescription, Prefix)
            );

            _ = Task.Run(async () =>
            {
                await Task.Delay(60_000);
                try
                {
                    await m0.DeleteAsync();
                    await m1.DeleteAsync();
                    await m2.DeleteAsync();
                    await m3.DeleteAsync();
                }
                catch { }
            });
        }

        // ============================================================================
        //  REQUEST
        // ============================================================================
        [Command("homereadyrequest")]
        [Alias("hrr")]
        [Summary("Downloads a HOME-ready PKM and queues it for trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        private async Task HOMEReadyRequestAsync(int index)
        {
            if (string.IsNullOrWhiteSpace(HOMEFolder))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyNotConfigured)).ConfigureAwait(false);
                return;
            }

            var userID = Context.User.Id;
            if (SysCord<T>.Runner.Hub.Queues.Info.IsUserInQueue(userID))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyAlreadyInQueue)).ConfigureAwait(false);
                return;
            }

            try
            {
                var files = Directory.GetFiles(HOMEFolder)
                    .OrderBy(f => f)
                    .ToList();

                if (files.Count == 0)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyNoPkmFiles)).ConfigureAwait(false);
                    return;
                }

                if (index < 1 || index > files.Count)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyInvalidEntry)).ConfigureAwait(false);
                    return;
                }

                var filePath = files[index - 1];
                var data = await File.ReadAllBytesAsync(filePath);
                var entity = EntityFormat.GetFromBytes(data);

                if (entity == null)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyConvertInvalid)).ConfigureAwait(false);
                    return;
                }

                var download = new Download<PKM>
                {
                    Data = entity,
                    Success = true
                };

                var pk = GetRequest(download);
                if (pk == null && entity is PKM rawPkm)
                {
                    pk = EntityConverter.ConvertToType(rawPkm, typeof(T), out _) as T;
                }

                if (pk == null)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyConvertTradeFormatFailed)).ConfigureAwait(false);
                    return;
                }

                var code = SysCord<T>.Runner.Hub.Queues.Info.GetRandomTradeCode(userID);
                var lgcode = SysCord<T>.Runner.Hub.Queues.Info.GetRandomLGTradeCode();
                var sig = Context.User.GetFavor();

                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyAddedQueue)).ConfigureAwait(false);

                await Helpers<T>.AddTradeToQueueAsync(
                    context: Context,
                    code: code,
                    trainerName: Context.User.Username,
                    pk: pk,
                    sig: sig,
                    usr: Context.User,
                    isBatchTrade: false,
                    batchTradeNumber: 1,
                    totalBatchTrades: 1,
                    isHiddenTrade: false,
                    isMysteryEgg: false,
                    lgcode: lgcode ?? SysCord<T>.Runner.Hub.Queues.Info.GetRandomLGTradeCode(),
                    tradeType: PokeTradeType.Specific,
                    ignoreAutoOT: false,
                    setEdited: false,
                    isNonNative: false
                );
            }
            catch (Exception ex)
            {
                await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordGenericError, ex.Message)).ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    if (Context.Message is IUserMessage msg)
                        await msg.DeleteAsync().ConfigureAwait(false);
                }
                catch { }
            }
        }


        // ============================================================================
        //  LIST
        // ============================================================================
        [Command("homereadylist")]
        [Alias("hrl")]
        [Summary("Lists available HOME-Ready files with filtering + pagination.")]
        private async Task HOMEListAsync([Remainder] string args = "")
        {
            if (string.IsNullOrWhiteSpace(HOMEFolder))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyNotConfigured));
                return;
            }

            const int itemsPerPage = 10;

            var files = Directory.GetFiles(HOMEFolder)
                .Select(Path.GetFileName)
                .OrderBy(x => x)
                .ToList();

            if (files.Count == 0)
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyNoFiles));
                return;
            }

            // Parse filter + page
            var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string filter = "";
            int page = 1;

            if (parts.Length > 0)
            {
                if (int.TryParse(parts.Last(), out int parsedPage))
                {
                    page = parsedPage;
                    filter = string.Join(" ", parts.Take(parts.Length - 1));
                }
                else
                {
                    filter = string.Join(" ", parts);
                }
            }

            var filtered = files
                .Where(f => string.IsNullOrWhiteSpace(filter) ||
                            f.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtered.Count == 0)
            {
                await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordHomeReadyNoMatches, filter));
                return;
            }

            var pageCount = (int)Math.Ceiling(filtered.Count / (double)itemsPerPage);
            page = Math.Clamp(page, 1, pageCount);

            var pageItems = filtered
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToList();

            var embed = new EmbedBuilder()
                .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordHomeReadyListTitle, filter))
                .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordHomeReadyListDescription, page, pageCount))
                .WithColor(Color.Blue);

            // Map file extensions to game names
            var extensionToGame = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                 { ".pb7", "LGPE" },
                 { ".pk8", "SWSH" },
                 { ".pb8", "BDSP" },
                 { ".pa8", "PLA" },
                 { ".pa9", "PLZA" },
                 { ".pk9", "SV" }
            };

            foreach (var item in pageItems)
            {
                var index = files.IndexOf(item) + 1;

                // Get the file extension, trim whitespace, and uppercase it
                var ext = Path.GetExtension(item)?.Trim().ToUpperInvariant() ?? "";

                // Lookup in dictionary
                string game = extensionToGame.TryGetValue(ext, out var g) ? g : "Unknown";

                // Add embed field
                embed.AddField(
                    $"{index}. {item}",
                    AppLocalization.Format(LocalizationKeys.DiscordHomeReadyListField, Prefix, index, game)
                );
            }

            var embedMsg = await ReplyAsync(embed: embed.Build());

            await Task.Delay(20_000);

            try
            {
                await embedMsg.DeleteAsync();
            }
            catch { }
        }

        // ============================================================================
        //  VIEW
        // ============================================================================
        [Command("homereadyview")]
        [Alias("hrv")]
        [Summary("Views a HOME-ready PKM in Showdown format before downloading.")]
        private async Task HOMEReadyViewAsync(int index)
        {
            if (string.IsNullOrWhiteSpace(HOMEFolder))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyNotConfigured)).ConfigureAwait(false);
                return;
            }

            try
            {
                var files = Directory.GetFiles(HOMEFolder)
                    .OrderBy(f => f)
                    .ToList();

                if (files.Count == 0)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyNoPkmFiles)).ConfigureAwait(false);
                    return;
                }

                if (index < 1 || index > files.Count)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyInvalidEntry));
                    return;
                }

                var filePath = files[index - 1];
                var raw = await File.ReadAllBytesAsync(filePath);

                var entity = EntityFormat.GetFromBytes(raw);
                if (entity == null)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyReadFailed)).ConfigureAwait(false);
                    return;
                }

                // Convert to correct PKM type for this bot
                PKM? typed = entity as T
                    ?? EntityConverter.ConvertToType(entity, typeof(T), out _) as T;

                if (typed == null)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyLoadedConvertFailed)).ConfigureAwait(false);
                    return;
                }

                // Generate showdown text
                string showdown = ShowdownParsing.GetShowdownText(typed);

                // ============================
                // METADATA
                // ============================

                string otName = typed.OriginalTrainerName;
                string tid = typed.TrainerTID7.ToString();

                string versionName = GameInfo.GetVersionName(typed.Version);

                string metDate = typed.MetDate?.ToString("yyyy-MM-dd") ?? "Unknown";

                string metLocStr = GameInfo.Strings.GetLocationName(
                    isEggLocation: false,
                    location: typed.MetLocation,
                    format: typed.Format,
                    generation: typed.Generation,
                    version: typed.Version
                );

                string game = versionName;
                ulong homeTracker = GetHomeTrackerSafe(typed);
                string homeTrackerStr = homeTracker == 0
                    ? AppLocalization.Format(LocalizationKeys.DiscordHomeReadyTrackerEmpty, game)
                    : $"{homeTracker:X16}";

                string details = AppLocalization.Format(LocalizationKeys.DiscordHomeReadyAdditionalDetails, otName, tid, versionName, typed.MetLocation, metLocStr, metDate, homeTrackerStr);


                string finalText =
                $"```text\n{showdown}\n```\n{details}";

                var embed = new EmbedBuilder()
                    .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordHomeReadyViewTitle, index))
                    .WithDescription(finalText)
                    .WithColor(Color.Magenta)
                    .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordHomeReadyViewFooter, Prefix, index));

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordGenericError, ex.Message));
            }
            finally
            {
                try { if (Context.Message is IUserMessage m) await m.DeleteAsync(); } catch { }
            }
        }

        private static ulong GetHomeTrackerSafe(PKM pkm)
        {
            var type = pkm.GetType();
            var prop = type.GetProperty("Tracker");

            if (prop == null)
                return 0; // This format doesn’t support HOME tracking

            object? value = prop.GetValue(pkm);
            if (value is ulong tracker)
                return tracker;

            return 0;
        }


        // ============================================================================
        //  DOWNLOAD FILE
        // ============================================================================
        [Command("homereadydownload")]
        [Alias("hrd")]
        [Summary("Downloads a HOME-ready PKM file by its number from the list.")]
        private async Task HOMEReadyDownloadAsync(int index)
        {
            if (string.IsNullOrWhiteSpace(HOMEFolder))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyNotConfigured)).ConfigureAwait(false);
                return;
            }

            try
            {
                var files = Directory.GetFiles(HOMEFolder)
                    .OrderBy(f => f)
                    .ToList();

                if (files.Count == 0)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyNoPkmFiles)).ConfigureAwait(false);
                    return;
                }

                if (index < 1 || index > files.Count)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHomeReadyInvalidEntry));
                    return;
                }

                var filePath = files[index - 1];
                var fileName = Path.GetFileName(filePath);

                // Send file directly
                await using (var fs = File.OpenRead(filePath))
                {
                    var msg = await Context.Channel.SendFileAsync(
                        stream: fs,
                        filename: fileName,
                        text: AppLocalization.Format(LocalizationKeys.DiscordHomeReadyDownloadText, fileName)
                    );
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordGenericError, ex.Message));
            }
            finally
            {
                try { if (Context.Message is IUserMessage m) await m.DeleteAsync(); } catch { }
            }
        }
    }
}
