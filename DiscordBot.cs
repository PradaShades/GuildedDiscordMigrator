
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Linq;
using System.Reflection;

namespace GuildedDiscordMigrator
{
    public class DiscordBot
    {
        private readonly string _token;
        private readonly ServerData _serverData;
        private DiscordSocketClient? _client;
        private CommandService? _commands;
        private IServiceProvider? _services;

        public DiscordBot(string token, ServerData serverData)
        {
            _token = token;
            _serverData = serverData;
        }

        public async Task StartAsync(IProgress<string>? progress = null)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All
            });

            _commands = new CommandService();
            
          
            _services = new ServiceCollection()
                .AddSingleton(this)
                .BuildServiceProvider();

            _client.Log += (logMessage) =>
            {
                progress?.Report($"[Discord] {logMessage.Message}");
                return Task.CompletedTask;
            };

            _client.Ready += () =>
            {
                progress?.Report("✅ Discord bot is connected and ready!");
                return Task.CompletedTask;
            };

            
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.MessageReceived += HandleCommandAsync;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (messageParam is not SocketUserMessage message) return;
            if (message.Author.IsBot) return;

            int argPos = 0;
            if (!message.HasCharPrefix('!', ref argPos)) return;

            var context = new SocketCommandContext(_client!, message);

            if (_commands != null && _services != null)
            {
                await _commands.ExecuteAsync(context, argPos, _services);
            }
        }

        [Group("migrate")]
        public class MigrationModule : ModuleBase<SocketCommandContext>
        {
            private readonly DiscordBot _bot;

            public MigrationModule(DiscordBot bot)
            {
                _bot = bot;
            }

            [Command("setup")]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task SetupCommand()
            {
                var startingEmbed = new EmbedBuilder()
                    .WithTitle("Server Migration Started")
                    .WithDescription("Starting server migration... This may take a few minutes.")
                    .WithColor(Discord.Color.LightGrey)
                    .WithThumbnailUrl("https://cdn.discordapp.com/emojis/1108455710372233246.png")
                    .Build();

                await Context.Message.ReplyAsync(embed: startingEmbed);

                try
                {
                    var guild = Context.Guild;

               
                    var rolesEmbed = new EmbedBuilder()
                        .WithTitle("Creating Roles")
                        .WithDescription("Creating roles from Guilded server...")
                        .WithColor(Discord.Color.LightGrey)
                        .Build();

                    await Context.Message.ReplyAsync(embed: rolesEmbed);

                    var roleMap = new Dictionary<int, IRole>();
                    int rolesCreated = 0;
                    int rolesSkipped = 0;
                    
                    foreach (var guildedRole in _bot._serverData.Roles)
                    {
                        try
                        {
                
                            var existingRole = guild.Roles.FirstOrDefault(r => r.Name == guildedRole.Name);
                            if (existingRole != null)
                            {
                                roleMap[guildedRole.Id] = existingRole;
                                rolesSkipped++;
                                continue;
                            }

                        
                            var color = ParseGuildedColor(guildedRole.Color);
                            
                            var role = await guild.CreateRoleAsync(guildedRole.Name, 
                                permissions: GuildPermissions.None, 
                                color: color, 
                                isHoisted: false, 
                                isMentionable: false);
                            
                            roleMap[guildedRole.Id] = role;
                            rolesCreated++;
                            await Task.Delay(500); 
                        }
                        catch (Exception ex)
                        {
                            var errorEmbed = new EmbedBuilder()
                                .WithTitle("Role Creation Error")
                                .WithDescription($"Failed to create role **{guildedRole.Name}**")
                                .AddField("Error", ex.Message)
                                .WithColor(Discord.Color.Red)
                                .Build();

                            await Context.Message.ReplyAsync(embed: errorEmbed);
                        }
                    }

                 
                    var categoriesEmbed = new EmbedBuilder()
                        .WithTitle("Creating Categories")
                        .WithDescription("Creating channel categories...")
                        .WithColor(Discord.Color.LightGrey)
                        .Build();

                    await Context.Message.ReplyAsync(embed: categoriesEmbed);

                    var categoryMap = new Dictionary<string, ICategoryChannel>();
                    int categoriesCreated = 0;
                    int categoriesSkipped = 0;
                    
                    foreach (var category in _bot._serverData.Categories)
                    {
                        try
                        {
                           
                            var existingCategory = guild.CategoryChannels.FirstOrDefault(c => c.Name == category.Name);
                            if (existingCategory != null)
                            {
                                categoryMap[category.Id] = existingCategory;
                                categoriesSkipped++;
                                continue;
                            }

                            var discordCategory = await guild.CreateCategoryChannelAsync(category.Name);
                            categoryMap[category.Id] = discordCategory;
                            categoriesCreated++;
                            await Task.Delay(500);
                        }
                        catch (Exception ex)
                        {
                            var errorEmbed = new EmbedBuilder()
                                .WithTitle("Category Creation Error")
                                .WithDescription($"Failed to create category **{category.Name}**")
                                .AddField("Error", ex.Message)
                                .WithColor(Discord.Color.Red)
                                .Build();

                            await Context.Message.ReplyAsync(embed: errorEmbed);
                        }
                    }

                
                    var channelsEmbed = new EmbedBuilder()
                        .WithTitle("Creating Channels")
                        .WithDescription("Creating text and voice channels...")
                        .WithColor(Discord.Color.LightGrey)
                        .Build();

                    await Context.Message.ReplyAsync(embed: channelsEmbed);

                    int channelsCreated = 0;
                    int channelsSkipped = 0;
                    int textChannels = 0;
                    int voiceChannels = 0;
                    
                    foreach (var channel in _bot._serverData.Channels)
                    {
                        try
                        {
                          
                            var existingChannel = guild.Channels.FirstOrDefault(c => c.Name == channel.Name);
                            if (existingChannel != null)
                            {
                                channelsSkipped++;
                                continue;
                            }

                            ICategoryChannel? parentCategory = null;
                            if (!string.IsNullOrEmpty(channel.ParentId) && categoryMap.ContainsKey(channel.ParentId))
                            {
                                parentCategory = categoryMap[channel.ParentId];
                            }

                            switch (channel.Type.ToLower())
                            {
                                case "chat":
                                case "text":
                                    await guild.CreateTextChannelAsync(channel.Name, props =>
                                    {
                                        props.CategoryId = parentCategory?.Id;
                                        props.Topic = channel.Topic;
                                    });
                                    channelsCreated++;
                                    textChannels++;
                                    break;

                                case "voice":
                                case "stream": 
                                    await guild.CreateVoiceChannelAsync(channel.Name, props =>
                                    {
                                        props.CategoryId = parentCategory?.Id;
                                    });
                                    channelsCreated++;
                                    voiceChannels++;
                                    break;

                                default:
                                    
                                    await guild.CreateTextChannelAsync(channel.Name, props =>
                                    {
                                        props.CategoryId = parentCategory?.Id;
                                        props.Topic = $"[{channel.Type}] {channel.Topic}";
                                    });
                                    channelsCreated++;
                                    textChannels++;
                                    break;
                            }

                            await Task.Delay(500);
                        }
                        catch (Exception ex)
                        {
                            var errorEmbed = new EmbedBuilder()
                                .WithTitle("Channel Creation Error")
                                .WithDescription($"Failed to create channel **{channel.Name}**")
                                .AddField("Error", ex.Message)
                                .WithColor(Discord.Color.Red)
                                .Build();

                            await Context.Message.ReplyAsync(embed: errorEmbed);
                        }
                    }

                   
                    var successEmbed = new EmbedBuilder()
                        .WithTitle("Migration Completed!")
                        .WithDescription($"Successfully migrated **{_bot._serverData.ServerName}** from Guilded to Discord!")
                        .WithColor(new Discord.Color(67, 181, 129)) 
                        .WithThumbnailUrl("https://cdn.discordapp.com/emojis/1108455710372233246.png")
                        .AddField("Migration Summary", $"**Roles:** {rolesCreated} created, {rolesSkipped} existing\n**Categories:** {categoriesCreated} created, {categoriesSkipped} existing\n**Channels:** {channelsCreated} created ({textChannels} text, {voiceChannels} voice), {channelsSkipped} existing", false)
                        .AddField("Next Steps", "• Review the created structure\n• Adjust permissions as needed\n• Invite your community members", false)
                        .WithFooter("Guilded to Discord Migrator • Thank you for using our tool!")
                        .WithCurrentTimestamp()
                        .Build();

                    await Context.Message.ReplyAsync(embed: successEmbed);

                }
                catch (Exception ex)
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("Migration Failed")
                        .WithDescription("An error occurred during the migration process")
                        .AddField("Error Details", ex.Message)
                        .WithColor(Discord.Color.Red)
                        .WithFooter("Please check the bot permissions and try again")
                        .WithCurrentTimestamp()
                        .Build();

                    await Context.Message.ReplyAsync(embed: errorEmbed);
                }
            }

            [Command("status")]
            public async Task StatusCommand()
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Migration Status")
                    .WithDescription($"Ready to migrate: **{_bot._serverData.ServerName}**")
                    .WithColor(new Discord.Color(201, 205, 218)) 
                    .WithThumbnailUrl("https://cdn.discordapp.com/emojis/1108455710372233246.png")
                    .AddField("Categories", _bot._serverData.Categories.Count, true)
                    .AddField("Channels", _bot._serverData.Channels.Count, true)
                    .AddField("Roles", _bot._serverData.Roles.Count, true)
                    .AddField("Ready to Go", "Use `!migrate setup` to start the migration", false)
                    .WithFooter("Ensure the bot has Administrator permissions")
                    .WithCurrentTimestamp()
                    .Build();

                await Context.Message.ReplyAsync(embed: embed);
            }

            [Command("help")]
            public async Task HelpCommand()
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Migration Bot Help")
                    .WithDescription("Commands to migrate your Guilded server to Discord")
                    .WithColor(new Discord.Color(201, 205, 218))
                    .WithThumbnailUrl("https://cdn.discordapp.com/emojis/1108455710372233246.png")
                    .AddField("`!migrate status`", "Shows the current migration status and server data", false)
                    .AddField("`!migrate setup`", "Starts the migration process (Admin only)", false)
                    .AddField("`!migrate help`", "Shows this help message", false)
                    .AddField("Requirements", "• Bot needs **Administrator** permissions\n• Server must have available role/channel slots", false)
                    .WithFooter("Made with Love by Cool")
                    .WithCurrentTimestamp()
                    .Build();

                await Context.Message.ReplyAsync(embed: embed);
            }

            private Discord.Color ParseGuildedColor(string guildedColor)
            {
                if (string.IsNullOrEmpty(guildedColor) || guildedColor.ToLower() == "transparent")
                    return Discord.Color.Default;

                try
                {
                  
                    var cleanColor = guildedColor.Replace("#", "");
                    
                    if (cleanColor.Length == 6)
                    {
                        var r = Convert.ToByte(cleanColor.Substring(0, 2), 16);
                        var g = Convert.ToByte(cleanColor.Substring(2, 2), 16);
                        var b = Convert.ToByte(cleanColor.Substring(4, 2), 16);
                        
                        return new Discord.Color(r, g, b);
                    }
                    else if (cleanColor.Length == 3)
                    {
                   
                        var r = Convert.ToByte(cleanColor.Substring(0, 1) + cleanColor.Substring(0, 1), 16);
                        var g = Convert.ToByte(cleanColor.Substring(1, 1) + cleanColor.Substring(1, 1), 16);
                        var b = Convert.ToByte(cleanColor.Substring(2, 1) + cleanColor.Substring(2, 1), 16);
                        
                        return new Discord.Color(r, g, b);
                    }
                    
                    return Discord.Color.Default;
                }
                catch
                {
                    return Discord.Color.Default;
                }
            }
        }
    }


    public class ServiceCollection
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public ServiceCollection AddSingleton<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));
            return this;
        }

        public IServiceProvider BuildServiceProvider()
        {
            return new ServiceProvider(_services);
        }
    }

    public class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services;

        public ServiceProvider(Dictionary<Type, object> services)
        {
            _services = services ?? new Dictionary<Type, object>();
        }

        public object? GetService(Type serviceType)
        {
            if (_services.ContainsKey(serviceType))
            {
                return _services[serviceType];
            }
            return null;
        }
    }
}
