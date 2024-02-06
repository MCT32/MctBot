using System.Linq.Expressions;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Npgsql;
using Sentry;

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
            appCommands.RegisterGlobalCommands<RegisterCommand>();
            appCommands.RegisterGlobalCommands<TransferCommand>();

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

    public class RegisterCommand : ApplicationCommandsModule
    {
        [SlashCommand("register", "Register yourself to the database.")]
        public async Task RegisterSlashCommand(InteractionContext ctx)
        {
            var postgresBuilder = new NpgsqlDataSourceBuilder();
            postgresBuilder.ConnectionStringBuilder.Host = System.Environment.GetEnvironmentVariable("PGHOST");

            var postgres = postgresBuilder.Build();

            var result = await postgres.CreateCommand($"SELECT 1 FROM users WHERE id = {ctx.UserId}").ExecuteScalarAsync();

            if (result == null)
            {
                await postgres.CreateCommand($"INSERT INTO users VALUES ({ctx.UserId})").ExecuteNonQueryAsync();

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = "You are now registered!"
                });
            }
            else
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = "You are already registered!"
                });
        }
    }

    public class TransferCommand : ApplicationCommandsModule
    {
        [SlashCommand("transfer", "Transfer money to another account.")]
        public async Task TransferSlashCommand(InteractionContext ctx, [Option("user", "User to transfer money to.")] DiscordUser user, [Option("amount", "Amount to transfer"), MinimumValue(1)] int amount)
        {
            if (user.Id == ctx.UserId)
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = "Cannot transfer to yourself!"
                });
            else
            {
                var postgresBuilder = new NpgsqlDataSourceBuilder();
                postgresBuilder.ConnectionStringBuilder.Host = System.Environment.GetEnvironmentVariable("PGHOST");

                var postgres = postgresBuilder.Build();

                var result = await postgres.CreateCommand($"SELECT 1 FROM users WHERE id = {user.Id}").ExecuteScalarAsync();

                if (result == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    {
                        Content = $"User {user.GlobalName} is not registered yet!"
                    });
                    return;
                }

                result = await postgres.CreateCommand($"SELECT 1 FROM users WHERE id = {ctx.UserId}").ExecuteScalarAsync();

                if (result == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    {
                        Content = $"You are not registered yet!"
                    });
                    return;
                }

                var connection = await postgres.OpenConnectionAsync();

                var transaction = await connection.BeginTransactionAsync();

                try
                {
                    await new NpgsqlCommand(@$"DO
                        $$
                        DECLARE bal int;
                        BEGIN
                        SELECT balance FROM users INTO bal WHERE id = {ctx.UserId} FOR UPDATE;
                        IF bal < {amount} THEN
                        RAISE EXCEPTION 'Insufficient funds.';
                        END IF;
                        UPDATE users SET balance = balance - {amount} WHERE id = {ctx.UserId};
                        UPDATE users SET balance = balance + {amount} WHERE id = {user.Id};
                        END;
                        $$;", 
                        connection, transaction).ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                }
                catch (PostgresException ex)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    {
                        Content = $"Error: {ex.Message}"
                    });
                    return;
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = "Transfer complete!"
                });
            }
        }
    }
}
