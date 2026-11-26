using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Pokemon.Localization;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord.Commands.Bots.SlashCommands;

public class SlashProfileModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    [SlashCommand("myinfo", "Display your trainer profile information.")]
    [RequireCommandAccessInteraction]
    public async Task MyInfoAsync(
        [Summary("user", "Whose profile to show. Defaults to yourself.")] SocketGuildUser? user = null)
    {
        var target = user ?? Context.User;
        var details = new TradeCodeStorage().GetTradeDetails(target.Id);
        if (details is null)
        {
            await RespondAsync(AppLocalization.Get(LocalizationKeys.DiscordProfileNoLastTrade), ephemeral: true).ConfigureAwait(false);
            return;
        }

        var privateDetails = target.Id == Context.User.Id
            ? $"\nOT: {details.OT}\nTID: {details.TID}\nSID: {details.SID}"
            : string.Empty;
        var embed = new EmbedBuilder()
            .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordProfileTitle))
            .WithDescription($"{target.Mention}{privateDetails}")
            .Build();
        await RespondAsync(embed: embed, ephemeral: target.Id == Context.User.Id).ConfigureAwait(false);
    }
}
