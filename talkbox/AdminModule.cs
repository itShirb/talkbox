using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace talkbox
{
    internal class AdminModule : BaseCommandModule
    {
        [Command("prefix")]
        [Description("Gets/Sets the bot's prefix for this guild.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        public async Task PrefixCommand(CommandContext ctx, string newPrefix)
        {
            await Database.Guilds.SetPrefix(ctx.Guild.Id, newPrefix);
            await ctx.RespondAsync($"Set this guild's prefix to '{newPrefix}'");
        }
    }
}
