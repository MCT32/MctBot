#![allow(non_snake_case)]
mod commands;

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
    }
}


#[tokio::main]
async fn main() {
    let token = dotenv::var("DISCORD_TOKEN").expect("Discord token not present in environment variables");
    
    let mut client = Client::builder(token, GatewayIntents::empty()).event_handler(Handler).await.expect("Error creating client");

    if let Err(why) = client.start().await {
        println!("Client error: {why:?}");
    }
}
