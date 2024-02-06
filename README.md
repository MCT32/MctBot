# MctBot
Discord economy and games bot written in C#.

## Usage
The simplest way to get the bot running is using [docker](#building-docker).

If you want to use this bot without docker, first you must [configure](#configuration) it, then [build](#building-release) it.
You can then run the bot with this command:
`dotnet <path to build .dll>`

## Configuration
To use this bot, you need to set up a discord bot through discord's [developer portal](https://docs.dcs.aitsys.dev/articles/getting_started/bot_account) (only follow the first section), then you need to set up [postgresql](https://www.postgresqltutorial.com/postgresql-getting-started/).

Once this is done, copy `example.env` to `.env` and copy the credentials for your discord bot and the postgresql service.

## Building (Docker)
To build the docker image, run this command:
`docker build -t mctbot .`

Then, copy `compose.example.yaml` to `compose.yaml` and set your discord token in the file. Then use this command to run the containers:
`docker compose up`

## Building (Release)
To build for production without debugging, use the following command:
`dotnet build -c Release`

This will output the `.dll` to `bin/Release/net8.0/MctBot.dll`

If instead you want to run the bot with one command, use this:
`dotnet run -c Release`

## Building (Debug)
To build for development with debugging, use the following command:
`dotnet build`

This will output the `.dll` to `bin/Debug/net8.0/MctBot.dll`

If instead you want to run the bot with one command, use this:
`dotnet run`