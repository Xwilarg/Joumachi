using Discord.Commands;
using DiscordUtils;
using System.Threading.Tasks;

namespace Joumachi.Module
{
    public class Communication : ModuleBase
    {
        [Command("Info")]
        public async Task Info()
        {
            await ReplyAsync(embed: Utils.GetBotInfo(Program.StartTime, "Joumachi", Program.Client.CurrentUser));
        }
    }
}
