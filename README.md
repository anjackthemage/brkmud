# BRK MUD

This is a simple MUD server I built in 2011. It was written by hand in a text editor as an exercise to improve my coding skills. Compiling was done using [csc](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/command-line-building-with-csc-exe). There are a lot of hardcoded strings and magic numbers in here, so you'll have to update those if you want to test it out. (I did say "improve" my coding, not "perfect".)

In its current state, it requires a SQL server configured in a specific way in order to work, though that can easily be changed.

The server works by creating a socket and listening for incoming connections. Whenever a connection is received, it will spin off a thread to handle all interaction with that connection. Users and rooms are loaded from a database and some basic commands are supported.(n,s,e,w, say, tell, help, etc.) I never got around to adding mobs/npcs so it's really just an enhanced chat room at this stage.