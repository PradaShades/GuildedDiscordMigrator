# Guilded to Discord Migrator

Migrate your Guilded server (channels, roles, categories) to Discord.

A tool desgined to help you migrate your Guilded server to Discord, preserving your server structure including categories, channels, and roles.

## Why I made this tool?

Guilded is being shut down by Roblox. https://devforum.roblox.com/t/update-on-guilded-and-communities/3966775/14

This tool was created to help Guilded server owners:

- Preserve their community structure during the transition
- Save hours of manual work recreating channels and categories

## Features

- Extract Guilded Server Data: Pulls categories, channels, and roles from your Guilded server
- Recreate Structure in Discord: Automatically recreates the entire server structure in Discord
- Role Migration: Transfers role names and colors
- Channel Organization: Maintains category channel relationships
- User Friendly Interface: Simple Windows Forms application with progress tracking
- Discord Bot: Easy setup with bot commands

## How It Works?

- Extract: Uses your Guilded authentication cookie to extract server structure
- Migrate: Uses a Discord bot to recreate everything in your Discord server

## How To

1. Log into Guilded in your browser
2. Press F12 → Console tab → type `document.cookie` and copy the result
3. Enable Developer Mode in Guilded settings
4. Right click your server and "Copy Server ID"

### Create Discord Bot

1. Go to Discord Developer Portal https://discord.com/developers/applications
2. Create new application → Bot tab
3. Reset and copy bot token
4. Enable Privileged Gateway Intents: Server Members Intent, Message Content Intent
5. Invite bot to your server with Administrator permissions

### Run Migration.exe

1. Launch the application
2. Paste your Guilded cookie and server ID
3. Click "Extract Guilded Server Data"
4. Paste your Discord bot token
5. Click "Start Discord Bot"
6. In your Discord server, type `!migrate setup`

## How to build?

1. Open Command Prompt (or PowerShell)
2. Clone the repository:  
   `git clone https://github.com/PradaShades/Guilded-to-Discord-Migrator.git`  
   `cd GuildedDiscordMigrator`
3. Restore dependencies:  
   `dotnet restore`
4. Build the project:  
   `dotnet build -c Release`
5. The compiled executable will be in:  
   `bin/Release/net8.0-windows/`

   <img width="946" height="759" alt="guilded_image_6050b629464ad4139e86237f25e9c826" src="https://github.com/user-attachments/assets/74dc5900-d4b1-4269-a9aa-2d93f765d8be" />


Made with ❤ to help communities transition, during these dark times.
