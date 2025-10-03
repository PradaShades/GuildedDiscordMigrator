Guilded to Discord Migrator

A tool desgined to help you migrate your Guilded server to Discord, preserving your server structure including categories, channels, and roles.

Why I made this tool?

Guilded is being shut down by Roblox. https://devforum.roblox.com/t/update-on-guilded-and-communities/3966775/14

This tool was created to help Guilded server owners:

Preserve their community structure during the transition

Save hours of manual work recreating channels and categories

Features:

Extract Guilded Server Data: Pulls categories, channels, and roles from your Guilded server

Recreate Structure in Discord: Automatically recreates the entire server structure in Discord

Role Migration: Transfers role names and colors

Channel Organization: Maintains category channel relationships

User Friendly Interface: Simple Windows Forms application with progress tracking

Discord Bot: Easy setup with bot commands

How It Works?

Extract: Uses your Guilded authentication cookie to extract server structure

Migrate: Uses a Discord bot to recreate everything in your Discord server

How To:

Log into Guilded in your browser

Press F12 → Console tab → type 'document.cookie' and copy the result

Enable Developer Mode in Guilded settings

Right click your server and "Copy Server ID"

Create Discord Bot
Go to Discord Developer Portal https://discord.com/developers/applications

Create new application → Bot tab

Reset and copy bot token

Enable Privileged Gateway Intents:

Server Members Intent Message Content Intent

Invite bot to your server with Administrator permissions

Run Migration.exe
Launch the application

Paste your Guilded cookie and server ID

Click "Extract Guilded Server Data"

Paste your Discord bot token

Click "Start Discord Bot"

In your Discord server, type !migrate setup

How to build?

Open Command Prompt (or PowerShell).

Clone the repository:

git clone https://github.com/PradaShades/Guilded-to-Discord-Migrator.git
cd GuildedDiscordMigrator

Restore dependencies:

dotnet restore

Build the project:

dotnet build -c Release

The compiled executable will be in:

bin/Release/net8.0-windows/

Made with ❤ to help communities transition, during these dark times.
