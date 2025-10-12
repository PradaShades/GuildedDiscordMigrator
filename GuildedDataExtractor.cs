using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Windows.Forms;
using System.Text;

namespace GuildedDiscordMigrator
{
    public class GuildedDataExtractor
    {
        private readonly string _guildedCookie;
        private readonly HttpClient _httpClient;

        public ServerData? ServerData { get; private set; }

        public GuildedDataExtractor(string guildedCookie)
        {
            _guildedCookie = guildedCookie;
            _httpClient = new HttpClient();
            SetupHttpClient();
        }

        private void SetupHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Origin", "https://www.guilded.gg");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.guilded.gg/");
            
            _httpClient.DefaultRequestHeaders.Add("Cookie", _guildedCookie);
        }

        public async Task<ServerData?> ExtractServerDataAsync(string serverId, IProgress<string>? progress = null)
        {
            try
            {
                progress?.Report("Starting Guilded server extraction...");

                progress?.Report("Testing authentication...");
                var userInfo = await GetUserInfo();
                if (userInfo == null)
                {
                    MessageBox.Show("❌ Authentication failed. Please check your cookie.\n\nMake sure you copied the ENTIRE cookie string from your browser's Developer Tools.", 
                        "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                progress?.Report($"✅ Authenticated as: {userInfo.Name}");

                progress?.Report("Fetching server information...");
                var serverInfo = await GetServerInfoWithRoles(serverId);
                if (serverInfo == null)
                {
                    MessageBox.Show($"❌ Failed to fetch server information for ID: {serverId}\n\nPlease ensure:\n• Server ID is correct\n• You have permission to access this server\n• The server exists", 
                        "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                progress?.Report($"✅ Server found: {serverInfo.Team.Name}");

                progress?.Report("Fetching channels and categories...");
                var fullChannelData = await GetFullChannelData(serverId);
                
                if (fullChannelData == null)
                {
                    progress?.Report("❌ Failed to fetch channel data");
                    return null;
                }

                
                var categories = new List<GuildedChannel>();
                foreach (var category in fullChannelData.Categories)
                {
                    var sanitizedName = SanitizeUnicodeName(category.Name);
                    categories.Add(new GuildedChannel
                    {
                        Id = category.Id.ToString(),
                        Name = sanitizedName,
                        Type = "category",
                        Topic = "",
                        ParentId = null,
                        ChannelCategoryId = null
                    });
                }

             
                var channels = new List<GuildedChannel>();
                foreach (var channel in fullChannelData.Channels)
                {
                    var sanitizedName = SanitizeUnicodeName(channel.Name);
                    var sanitizedDescription = SanitizeUnicodeName(channel.Description ?? "");
                    
                    var guildedChannel = new GuildedChannel
                    {
                        Id = channel.Id,
                        Name = sanitizedName,
                        Type = channel.ContentType ?? "chat", 
                        Topic = sanitizedDescription,
                        ParentId = channel.ChannelCategoryId?.ToString(),
                        ChannelCategoryId = channel.ChannelCategoryId?.ToString()
                    };
                    channels.Add(guildedChannel);
                }

                progress?.Report($"Found {categories.Count} categories and {channels.Count} channels");

                progress?.Report("Fetching roles...");
                var roles = ExtractRolesFromServerInfo(serverInfo);
                
                foreach (var role in roles)
                {
                    role.Name = SanitizeUnicodeName(role.Name);
                }
                
                progress?.Report($"Found {roles?.Count ?? 0} roles");

                progress?.Report($"✅ Extracted: {categories.Count} categories, {channels.Count} channels, {roles?.Count ?? 0} roles");

                ServerData = new ServerData
                {
                    ServerName = SanitizeUnicodeName(serverInfo.Team.Name),
                    Categories = categories,
                    Channels = channels,
                    Roles = roles ?? new List<GuildedRole>()
                };

                return ServerData;
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ Extraction error: {ex.Message}");
                MessageBox.Show($"Extraction failed: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private string SanitizeUnicodeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unnamed";

            try
            {
               
                var normalized = name.Normalize(NormalizationForm.FormC);
                
                
                var sanitized = new string(normalized
                    .Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t')
                    .ToArray());

                if (string.IsNullOrWhiteSpace(sanitized))
                    return "unnamed";

                return sanitized.Trim();
            }
            catch (Exception)
            {
               
                try
                {
                    var basicSanitized = new string(name
                        .Where(c => char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsWhiteSpace(c))
                        .ToArray());

                    return string.IsNullOrWhiteSpace(basicSanitized) ? "unnamed" : basicSanitized.Trim();
                }
                catch (Exception)
                {
                    return "unnamed";
                }
            }
        }

        private async Task<GuildedUserInfo?> GetUserInfo()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://www.guilded.gg/api/me");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<GuildedUserInfo>(json);
                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting user info: {ex.Message}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
        }

        private async Task<GuildedServerInfoWithRoles?> GetServerInfoWithRoles(string serverId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://www.guilded.gg/api/teams/{serverId}/info");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<GuildedServerInfoWithRoles>(json);
                }
                else
                {
                    var response2 = await _httpClient.GetAsync($"https://www.guilded.gg/api/teams/{serverId}");
                    if (response2.IsSuccessStatusCode)
                    {
                        var json = await response2.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<GuildedServerInfoWithRoles>(json);
                    }
                    
                    MessageBox.Show($"Failed to fetch server info. Status: {response.StatusCode}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching server info: {ex.Message}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
        }

        private List<GuildedRole> ExtractRolesFromServerInfo(GuildedServerInfoWithRoles serverInfo)
        {
            var roles = new List<GuildedRole>();
            
            if (serverInfo?.Team?.RolesById != null)
            {
                foreach (var roleEntry in serverInfo.Team.RolesById)
                {
                    if (roleEntry.Key == "baseRole") continue;
                    
                    var role = roleEntry.Value;
                    roles.Add(new GuildedRole
                    {
                        Id = role.Id,
                        Name = role.Name,
                        Color = role.Color
                    });
                }
            }

            return roles;
        }

        private async Task<FullChannelData?> GetFullChannelData(string serverId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://www.guilded.gg/api/teams/{serverId}/channels?excludeBadgedContent=true");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };
                    
                    return JsonConvert.DeserializeObject<FullChannelData>(json, settings);
                }
                else
                {
                    MessageBox.Show($"Failed to fetch channel data. Status: {response.StatusCode}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching channel data: {ex.Message}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
        }
    }

    public class GuildedServerInfoWithRoles
    {
        [JsonProperty("team")] public GuildedTeam Team { get; set; } = new();
    }

    public class GuildedTeam
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("about")] public string About { get; set; } = "";
        [JsonProperty("rolesById")] public Dictionary<string, GuildedRoleDetail> RolesById { get; set; } = new();
    }

    public class GuildedRoleDetail
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("color")] public string Color { get; set; } = "";
        [JsonProperty("priority")] public int Priority { get; set; }
        [JsonProperty("isBase")] public bool IsBase { get; set; }
    }

    public class FullChannelData
    {
        [JsonProperty("channels")] public List<ChannelData> Channels { get; set; } = new();
        [JsonProperty("categories")] public List<CategoryData> Categories { get; set; } = new();
    }

    public class ChannelData
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("type")] public string Type { get; set; } = "";
        [JsonProperty("description")] public string? Description { get; set; }
        [JsonProperty("contentType")] public string? ContentType { get; set; }
        [JsonProperty("channelCategoryId")] public int? ChannelCategoryId { get; set; }
        [JsonProperty("groupId")] public string? GroupId { get; set; }
    }

    public class CategoryData
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("priority")] public int? Priority { get; set; } 
    }

    public class ServerData
    {
        public string ServerName { get; set; } = "";
        public List<GuildedChannel> Categories { get; set; } = new();
        public List<GuildedChannel> Channels { get; set; } = new();
        public List<GuildedRole> Roles { get; set; } = new();
    }

    public class GuildedUserInfo
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("name")] public string Name { get; set; } = "";
    }

    public class GuildedChannel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Topic { get; set; } = "";
        public string? ParentId { get; set; }
        public string? ChannelCategoryId { get; set; }
    }

    public class GuildedRole
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("color")] public string Color { get; set; } = "";
    }
}