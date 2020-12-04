using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordUtils;
using Joumachi.Module;
using Newtonsoft.Json;
using NTextCat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WeCantSpell.Hunspell;

namespace Joumachi
{
    class Program
    {
        public static void Main()
                  => new Program().MainAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient Client { private set; get; }
        private readonly CommandService _commands = new CommandService();

        public static Random Rand = new Random();
        public static DateTime StartTime { private set; get; }

        private RankedLanguageIdentifier _identifier;

        private Dictionary<string, WordList> _dictionaries = new Dictionary<string, WordList>();

        private char[] _splitChar = new[]
        {
            '.', ' ', '/', '\\', '!', '?', ',', ';', '(', ')', '\n', ':', '_', '`', '*', '"'
        };

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

            var factory = new RankedLanguageIdentifierFactory();
            _identifier = factory.Load("Keys/Core14.profile.xml");

            foreach (var file in Directory.GetFiles("Dictionaries"))
            {
                FileInfo fi = new FileInfo(file);
                if (fi.Name.Split('.')[1] == "dic")
                    _dictionaries.Add(fi.Name.Split('.')[0], WordList.CreateFromFiles(file));
            }

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
            if (msg == null || msg.Author.Id == Client.CurrentUser.Id) return;
            int pos = 0;
            if (!arg.Author.IsBot && (msg.HasMentionPrefix(Client.CurrentUser, ref pos) || msg.HasStringPrefix("j.", ref pos)))
            {
                SocketCommandContext context = new SocketCommandContext(Client, msg);
                var result = await _commands.ExecuteAsync(context, pos, null);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.Error.ToString() + ": " + result.ErrorReason);
                }
                else
                    return;
            }
            var languages = _identifier.Identify(msg.Content);
            var mostCertainLanguage = languages.FirstOrDefault();
            if (mostCertainLanguage != null && File.Exists("Dictionaries/" + mostCertainLanguage.Item1.Iso639_2T + ".dic"))
            {
                var checker = _dictionaries[mostCertainLanguage.Item1.Iso639_2T];
                foreach (string s in msg.Content.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!s.Any(x => char.IsLetter(x)))
                        continue;
                    if (!checker.Check(s))
                    {
                        var word = s.Trim(_splitChar);
                        var suggestions = checker.Suggest(word).ToArray();
                        if (suggestions.Length == 0)
                            await msg.Channel.SendMessageAsync("\"" + s + "\" doesn't exists");
                        else
                            await msg.Channel.SendMessageAsync("\"" + s + "\" doesn't exists, maybe you meant \"" + suggestions[0] + "\"?");
                        break;
                    }
                }
            }
        }
    }
}
