using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;

namespace MctBot
{
    class MctBot
    {
        static void Main(string[] args)
        {
            DotNetEnv.Env.Load();

            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = System.Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent
            });

            var appCommands = discord.UseApplicationCommands();

            appCommands.RegisterGlobalCommands<PingCommand>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }

    public class PingCommand : ApplicationCommandsModule
    {
        [SlashCommand("ping", "Replies with pong!")]
        public async Task PingSlashCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DisCatSharp.Entities.DiscordInteractionResponseBuilder()
            {
                Content = "Pong!"
            });
        }
    }
}
