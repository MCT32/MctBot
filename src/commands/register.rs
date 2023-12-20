use postgres::{Client, NoTls};
use serenity::builder::CreateCommand;
use serenity::model::application::ResolvedOption;
use serenity::all::UserId;
use std::thread;


pub fn run(_options: &[ResolvedOption], user_id: UserId) -> String {
    let query = thread::spawn(move || {
        let mut client = Client::connect("host=localhost user=postgres dbname=mctbot", NoTls).expect("Could not connect to database");
        client.query_opt("SELECT * FROM users WHERE id = $1", &[&<UserId as Into<i64>>::into(user_id)]).expect("Multiple occurrences in database")
    }).join().unwrap();

    match query {
        None => {
            thread::spawn(move || {
                let mut client = Client::connect("host=localhost user=postgres dbname=mctbot", NoTls).expect("Could not connect to database");
                client.execute("INSERT INTO users(id) VALUES ($1)", &[&<UserId as Into<i64>>::into(user_id)]).unwrap();
            }).join().unwrap();

            "User registered".to_string()
        },
        Some(_) => "User already registered".to_string(),
    }
}

pub fn register() -> CreateCommand {
    CreateCommand::new("register").description("Add user to database")
}