using Discord;
using Discord.Commands;
using SysBot.Pokemon.Localization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class BotAvatar : ModuleBase<SocketCommandContext>
    {
        [Command("setavatar")]
        [Alias("botavatar", "changeavatar", "sa", "ba")]
        [Summary("Sets the bot's avatar to a specified GIF.")]
        [RequireOwner]
        public async Task SetAvatarAsync()
        {
            var userMessage = Context.Message;

            if (userMessage.Attachments.Count == 0)
            {
                var reply = await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordAvatarAttachGif, Context.User.Mention)); // standard (boring) images can be set via dashboard
                await Task.Delay(60000);
                await userMessage.DeleteAsync();
                await reply.DeleteAsync();
                return;
            }
            var attachment = userMessage.Attachments.First();
            if (!attachment.Filename.EndsWith(".gif"))
            {
                var reply = await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordAvatarProvideGif, Context.User.Mention));
                await Task.Delay(60000);
                await userMessage.DeleteAsync();
                await reply.DeleteAsync();
                return;
            }

            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(attachment.Url);

            await using var ms = new MemoryStream(imageBytes);
            var image = new Image(ms);
            await Context.Client.CurrentUser.ModifyAsync(user => user.Avatar = image);

            var successReply = await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordAvatarUpdated, Context.User.Mention));
            await Task.Delay(60000);
            await userMessage.DeleteAsync();
            await successReply.DeleteAsync();
        }
    }
}
