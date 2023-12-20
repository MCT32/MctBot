use postgres::{Client, NoTls};
use serenity::builder::{CreateCommand, CreateCommandOption};
use serenity::model::application::ResolvedOption;
use serenity::all::{CommandOptionType, ResolvedValue, UserId};
use std::thread;


pub fn run(options: &[ResolvedOption]) -> String {
    match options[0].value {
        ResolvedValue::User(user, _) => {
            let user = user.clone();

            let query = thread::spawn(move || {
                let mut client = Client::connect("host=localhost user=postgres dbname=mctbot", NoTls).expect("Could not connect to database");
                client.query_opt("SELECT balance FROM users WHERE id = $1", &[&<UserId as Into<i64>>::into(user.id)]).expect("Multiple occurrences in database")
            }).join().unwrap();

            match query {
                Some(balance) => balance.get::<usize, i64>(0).to_string(),
                None => "User not registered".to_string()
            }
        },
        _ => "Please supply user".to_string()
    }
}

pub fn register() -> CreateCommand {
    CreateCommand::new("balance").description("Get account balance").add_option(CreateCommandOption::new(CommandOptionType::User, "user", "User who's balance is being requested").required(true))
}