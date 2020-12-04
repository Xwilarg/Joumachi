using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordUtils;
using Joumachi.Module;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Joumachi
{
    class Program
    {
        public static void Main(string[] args)
                  => new Program().MainAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient Client { private set; get; }
        private readonly CommandService _commands = new CommandService();

        public static Random Rand = new Random();
        public static DateTime StartTime { private set; get; }

        private Program()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
            });
            Client.Log += Utils.Log;
            _commands.Log += Utils.LogErrorAsync;
        }

        private async Task MainAsync()
        {
            var credentials = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("Keys/Credentials.json"));
            if (credentials.BotToken == null)
                throw new NullReferenceException("Invalid Credentials file");

            await _commands.AddModuleAsync<Communication>(null);

            Client.MessageReceived += HandleCommandAsync;

            StartTime = DateTime.Now;
            await Client.LoginAsync(TokenType.Bot, credentials.BotToken);
            await Client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage msg = arg as SocketUserMessage;
            if (msg == null) return;
            int pos = 0;
            if (!arg.Author.IsBot && (msg.HasMentionPrefix(Client.CurrentUser, ref pos) || msg.HasStringPrefix("j.", ref pos)))
            {
                SocketCommandContext context = new SocketCommandContext(Client, msg);
                var result = await _commands.ExecuteAsync(context, pos, null);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.Error.ToString() + ": " + result.ErrorReason);
                }
            }
        }
    }
}
