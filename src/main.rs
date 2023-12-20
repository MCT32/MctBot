#![allow(non_snake_case)]
mod commands;

use std::thread;

use serenity::async_trait;
use serenity::builder::{CreateInteractionResponseMessage, CreateInteractionResponse};
use serenity::model::gateway::Ready;
use serenity::model::application::{Command, Interaction};
use serenity::prelude::*;


struct Handler;

#[async_trait]
impl EventHandler for Handler {
    async fn interaction_create(&self, ctx: Context, interaction: Interaction) {
        if let Interaction::Command(command) = interaction {
            let content = match command.data.name.as_str() {
                "ping" => commands::ping::run(&command.data.options()),
                "balance" => commands::balance::run(&command.data.options()),
                "register" => commands::register::run(&command.data.options(), command.user.id),
                _ => "not implemented".to_string()
            };

            let data = CreateInteractionResponseMessage::new().content(content);
            let builder = CreateInteractionResponse::Message(data);
            if let Err(why) = command.create_response(&ctx.http, builder).await {
                println!("Interaction failed: {}", why);
            }
        }
    }

    async fn ready(&self, ctx: Context, ready: Ready) {
        println!("{} is connected!", ready.user.name);

        Command::create_global_command(&ctx.http, commands::ping::register()).await.expect("Couldn't register command");
        Command::create_global_command(&ctx.http, commands::balance::register()).await.expect("Couldn't register command");
        Command::create_global_command(&ctx.http, commands::register::register()).await.expect("Couldn't register command");
    }
}


#[tokio::main]
async fn main() {
    let token = dotenv::var("DISCORD_TOKEN").expect("Discord token not present in environment variables");
    
    let mut client = Client::builder(token, GatewayIntents::empty()).event_handler(Handler).await.expect("Error creating client");

    thread::spawn(|| {
        let mut sql_client = postgres::Client::connect("host=localhost user=postgres dbname=mctbot", postgres::NoTls).expect("Could not connect to database");
    
        sql_client.execute("CREATE TABLE IF NOT EXISTS public.users
        (
            id bigint NOT NULL,
            balance bigint NOT NULL DEFAULT 1000
        )", &[]).unwrap();
    }).join().unwrap();

    if let Err(why) = client.start().await {
        println!("Client error: {why:?}");
    }
}
