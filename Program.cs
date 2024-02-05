using System.Data;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Npgsql;

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
            var postgresBuilder = new NpgsqlDataSourceBuilder();
            postgresBuilder.ConnectionStringBuilder.Host = System.Environment.GetEnvironmentVariable("PGHOST");

            var postgres = postgresBuilder.Build();

            await postgres.CreateCommand(@"CREATE TABLE IF NOT EXISTS users (
                id bigserial PRIMARY KEY,
                balance integer NOT NULL DEFAULT 1000
            );").ExecuteNonQueryAsync();

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = System.Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent
            });

            var appCommands = discord.UseApplicationCommands();

            appCommands.RegisterGlobalCommands<PingCommand>();
            appCommands.RegisterGlobalCommands<BalanceCommand>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }

    public class PingCommand : ApplicationCommandsModule
    {
        [SlashCommand("ping", "Replies with pong!")]
        public async Task PingSlashCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "Pong!"
            });
        }
    }

    public class BalanceCommand : ApplicationCommandsModule
    {
        [SlashCommand("balance", "Retrieve account balance.")]
        public async Task BalanceSlashCommand(InteractionContext ctx, [Option("user", "User who's balance to get.")] DiscordUser user)
        {
            var postgresBuilder = new NpgsqlDataSourceBuilder();
            postgresBuilder.ConnectionStringBuilder.Host = System.Environment.GetEnvironmentVariable("PGHOST");

            var postgres = postgresBuilder.Build();

            var balance = await postgres.CreateCommand($"SELECT balance FROM users WHERE id = {user.Id}").ExecuteScalarAsync();

            if (balance == null)
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = $"User {user.GlobalName} is not registered yet!"
                });
            else
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = $"User {user.GlobalName} has ${balance} in the bank."
                });
        }
    }
}
