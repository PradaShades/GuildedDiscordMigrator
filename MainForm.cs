using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace GuildedDiscordMigrator
{
    public partial class MainForm : Form
    {
        private TextBox? guildedCookieTextBox;
        private TextBox? guildedServerIdTextBox;
        private TextBox? discordTokenTextBox;
        private Button? extractGuildedButton;
        private Button? startBotButton;
        private Button? helpButton;
        private ListBox? logListBox;
        private ProgressBar? progressBar;
        private Label? statusLabel;

        private GuildedDataExtractor? guildedExtractor;
        private DiscordBot? discordBot;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "guilded.gg/claim";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 15, 15);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            InitializeUI();
            
            this.ResumeLayout(false);
        }

        private void InitializeUI()
        {

            try
            {
                this.Icon = new Icon("guilded.ico");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load icon: {ex.Message}");
             
            }

            var guildedCookieLabel = new Label
            {
                Text = "Guilded Cookie:",
                Location = new Point(20, 50),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };
            this.Controls.Add(guildedCookieLabel);

            guildedCookieTextBox = new TextBox
            {
                Location = new Point(130, 50),
                Size = new Size(500, 20),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(guildedCookieTextBox);

            var guildedServerIdLabel = new Label
            {
                Text = "Guilded Server ID:",
                Location = new Point(20, 80),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };
            this.Controls.Add(guildedServerIdLabel);

            guildedServerIdTextBox = new TextBox
            {
                Location = new Point(130, 80),
                Size = new Size(200, 20),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(guildedServerIdTextBox);

            extractGuildedButton = new Button
            {
                Text = "Extract Guilded Server Data",
                Location = new Point(350, 80),
                Size = new Size(150, 23),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            extractGuildedButton.Click += ExtractGuildedButton_Click;
            this.Controls.Add(extractGuildedButton);

            var discordSectionLabel = new Label
            {
                Text = "Discord Server",
                Location = new Point(20, 120),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(discordSectionLabel);

            var discordTokenLabel = new Label
            {
                Text = "Discord Bot Token:",
                Location = new Point(20, 150),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };
            this.Controls.Add(discordTokenLabel);

            discordTokenTextBox = new TextBox
            {
                Location = new Point(150, 150),
                Size = new Size(480, 20),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(discordTokenTextBox);

            startBotButton = new Button
            {
                Text = "Start Discord Bot",
                Location = new Point(20, 180),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            startBotButton.Click += StartBotButton_Click;
            this.Controls.Add(startBotButton);

        
            helpButton = new Button
            {
                Text = "How to Get Credentials",
                Location = new Point(650, 50),
                Size = new Size(120, 50),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            helpButton.Click += HelpButton_Click;
            this.Controls.Add(helpButton);

            progressBar = new ProgressBar
            {
                Location = new Point(20, 220),
                Size = new Size(750, 20),
                Style = ProgressBarStyle.Continuous
            };
            this.Controls.Add(progressBar);
            
            statusLabel = new Label
            {
                Text = "Ready to migrate server",
                Location = new Point(20, 250),
                Size = new Size(750, 20),
                ForeColor = Color.White
            };
            this.Controls.Add(statusLabel);

       
            logListBox = new ListBox
            {
                Location = new Point(20, 280),
                Size = new Size(750, 280),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(logListBox);

      
            SetPlaceholderText(guildedCookieTextBox, "Paste your Guilded cookie here");
            SetPlaceholderText(guildedServerIdTextBox, "Enter Guilded Server ID");
            SetPlaceholderText(discordTokenTextBox, "Paste your Discord bot token here");
        }

        private void SetPlaceholderText(TextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.ForeColor = Color.Gray;
            
            textBox.Enter += (sender, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.ForeColor = Color.White;
                }
            };
            
            textBox.Leave += (sender, e) =>
            {
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                }
            };
        }

        private async void ExtractGuildedButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(guildedCookieTextBox!.Text) || 
                guildedCookieTextBox.Text == "Paste your Guilded cookie here" ||
                string.IsNullOrEmpty(guildedServerIdTextBox!.Text) ||
                guildedServerIdTextBox.Text == "Enter Guilded Server ID")
            {
                MessageBox.Show("Please enter Guilded Cookie and Server ID", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetUIEnabled(false);
            logListBox!.Items.Clear();

            try
            {
                guildedExtractor = new GuildedDataExtractor(guildedCookieTextBox.Text);
                var progress = new Progress<string>(LogMessage);

                var serverData = await guildedExtractor.ExtractServerDataAsync(guildedServerIdTextBox.Text, progress);

                if (serverData != null)
                {
                    LogMessage($"✅ Extracted: {serverData.Categories.Count} categories, {serverData.Channels.Count} channels, {serverData.Roles.Count} roles");
                    MessageBox.Show($"Server data extracted successfully!\n\nCategories: {serverData.Categories.Count}\nChannels: {serverData.Channels.Count}\nRoles: {serverData.Roles.Count}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("❌ Failed to extract server data");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error: {ex.Message}");
                MessageBox.Show($"Extraction failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIEnabled(true);
            }
        }

        private async void StartBotButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(discordTokenTextBox!.Text) || 
                discordTokenTextBox.Text == "Paste your Discord bot token here")
            {
                MessageBox.Show("Please enter Discord Bot Token", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (guildedExtractor?.ServerData == null)
            {
                MessageBox.Show("Please extract Guilded server data first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetUIEnabled(false);

            try
            {
                discordBot = new DiscordBot(discordTokenTextBox.Text, guildedExtractor.ServerData);
                var progress = new Progress<string>(LogMessage);

                await discordBot.StartAsync(progress);

                LogMessage("✅ Discord bot started! Use '!migrate setup' in your Discord server to migrate the structure.");
                MessageBox.Show("Discord bot is running! Go to your Discord server and type '!migrate setup' to recreate the Guilded server structure.", "Bot Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error starting bot: {ex.Message}");
                MessageBox.Show($"Failed to start bot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetUIEnabled(true);
            }
        }

        private void HelpButton_Click(object? sender, EventArgs e)
{
    var helpForm = new Form
    {
        Text = "How to Get Credentials",
        Size = new Size(700, 500),
        StartPosition = FormStartPosition.CenterParent,
        BackColor = Color.FromArgb(15, 15, 15),
        ForeColor = Color.White,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MaximizeBox = false
    };

    var helpText = new RichTextBox
    {
        Location = new Point(20, 20),
        Size = new Size(640, 400),
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        ReadOnly = true,
        ScrollBars = RichTextBoxScrollBars.Vertical,
        DetectUrls = false
    };

  
    helpText.Text = @"
GUILDED:
• Log in to Guilded in your browser. 
• Press F12 → open the Console tab.
• Type document.cookie and press Enter.
• Copy the entire cookie string.
• Go to Settings → Advanced → Enable Developer Mode.
• Right click your server and press 'Copy Server ID'.

DISCORD BOT:
• Go to Discord Developer Portal:";


    helpText.AppendText("\n• ");
    int startPos = helpText.TextLength;
    helpText.AppendText("https://discord.com/developers/applications");
    int endPos = helpText.TextLength;
    
    helpText.AppendText(@"
• Click New Application.
• Create a bot 
• Go to Bot tab
• Click reset token, then press copy token 
• Scroll Down Enable these Privileged Gateway Intents:

• Server Members Intent
• Message Content Intent

• Go to OAuth2 tab, scroll down, select bot, grant Administrator, copy the generated URL, and open it in a new tab to invite the bot to your server.
• Once the bot is in the server, click Start Bot and run '!migrate setup' in Discord

CMDS:
• !migrate setup
• !migrate status
• !migrate help
"
 );

    
    helpText.Select(startPos, endPos - startPos);
    helpText.SelectionColor = Color.Cyan;
    helpText.SelectionFont = new Font(helpText.Font, FontStyle.Underline);
    helpText.Select(0, 0); 

    var closeButton = new Button
    {
        Text = "Close",
        Location = new Point(560, 430),
        Size = new Size(100, 30),
        BackColor = Color.FromArgb(40, 40, 40),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
    };
    closeButton.Click += (s, args) => helpForm.Close();

    helpText.LinkClicked += (sender, args) => 
    {
        if (args.LinkText.StartsWith("http"))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = args.LinkText,
                UseShellExecute = true
            });
        }
    };

  
    helpText.DetectUrls = true;

    helpForm.Controls.Add(helpText);
    helpForm.Controls.Add(closeButton);
    helpForm.ShowDialog();
}
        private void LogMessage(string message)
        {
            if (logListBox != null)
            {
                logListBox.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                logListBox.TopIndex = logListBox.Items.Count - 1;
                statusLabel!.Text = message;
            }
        }

        private void SetUIEnabled(bool enabled)
        {
            guildedCookieTextBox!.Enabled = enabled;
            guildedServerIdTextBox!.Enabled = enabled;
            discordTokenTextBox!.Enabled = enabled;
            extractGuildedButton!.Enabled = enabled;
            startBotButton!.Enabled = enabled;
            helpButton!.Enabled = enabled;

            if (enabled)
            {
                progressBar!.Value = 0;
            }
        }
    }
}