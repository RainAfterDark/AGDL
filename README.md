# AGDL (Anime Game Damage Logger)
A console app damage logger for a certain anime game.

You can get the latest version in [releases](https://github.com/RainAfterDark/AGDL/releases) or the latest build in the [actions](https://github.com/RainAfterDark/AGDL/actions) tab (both for win-x64).

## Prerequisite/s

- [Npcap](https://npcap.com/#download) for windows

## How to use
- Make sure to open the application before starting the game (after this, subsequent restarts of the app/relogging in game should work).
- Log-in to the game. If you don't see a player log-in message right after the seed is found, please relog-in.
  - If you don't see a seed found message, let alone a server handshake, the network interface you're listening to probably isn't used by the game (either you have multiple adapters or you're using a VPN). If this is the case, change the `ChooseInterface` setting in the generated `config.json` to true, restart the app, and manually choose the correct interface.
- If you did see a player log-in message, and the team updated, then congrats, it works.

Logs should be saved by default in `DamageLogs/<name-of-the-characters-in-your-team>`. These are automatically generated, and a new log will be created every time your team updates (by changing scenes or manually changing your characters). You can also manually reset the log (and create a new one). The console will output logs by default as a table, but in the files they will come in a TSV format. You can use this [sheet](https://docs.google.com/spreadsheets/d/1oHRyBSnGIyMt5oFoOUKOTPwu9zJ3OCaJ4uyTUODp5To/edit?usp=sharing) to format the data and apply filters, etc.

## FAQ
> Is it safe?

This app is simply built on top of a packet sniffer + decryptor for a particular anime game to capture gameplay data. Nothing is injected/modified into the game.

> Why are the damage source names weird/confusing?

Those are the internal names of abilities/gadgets/reactions extracted from the game. A sort of friendly name translation feature could be added in the future, but it will take a while to map out which name corresponds to which ability/reaction/etc. Feel free to open an issue about this or maybe even a PR.

> Are the logs actually accurate?

This is still subject to testing. One thing however is that **electro-charged reactions aren't associated properly**, so please double check the logs if you're trying to count or determine who owns which tick. As far as other reactions go, they should be pretty accurate.

## Many thanks to
- Original project [DNTK](https://github.com/Crepe-Inc/DNTK) and its contributors
- Crepe + Slushy teams for protos
- Dim for AnimeGameData
