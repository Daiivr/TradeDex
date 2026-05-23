using Discord.Commands;
using SysBot.Pokemon.Localization;
using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public sealed class RequireOwnerAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

        if (context.User.Id == application.Owner.Id)
            return PreconditionResult.FromSuccess();

        return PreconditionResult.FromError(AppLocalization.Format(LocalizationKeys.DiscordPreconditionOwnerRequired, context.User.Mention));
    }
}
