using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MorePlayersInstaller
{
    public class InstallerForm : Form
    {
        private Label titleLabel;
        private Label statusLabel;
        private ProgressBar progressBar;
        private Button installButton;
        private TextBox gamePathTextBox;
        private Button browseButton;
        private RichTextBox logTextBox;

        private const string MELONLOADER_URL = "https://github.com/LavaGang/MelonLoader/releases/download/v0.6.6/MelonLoader.Installer.exe";
        private const string MOD_VERSION = "1.8.0-zxzinn";

        public InstallerForm()
        {
            InitializeComponents();
            DetectGamePath();
        }

        private void InitializeComponents()
        {
            // Form settings
            this.Text = "MIMESIS MorePlayers Enhanced Installer";
            this.Size = new Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Title
            titleLabel = new Label
            {
                Text = "🎮 MIMESIS MorePlayers v" + MOD_VERSION,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(560, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Game path label
            var pathLabel = new Label
            {
                Text = "Game Path:",
                Location = new Point(20, 80),
                Size = new Size(100, 25)
            };

            // Game path textbox
            gamePathTextBox = new TextBox
            {
                Location = new Point(20, 105),
                Size = new Size(460, 25),
                ReadOnly = true
            };

            // Browse button
            browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(490, 105),
                Size = new Size(80, 25)
            };
            browseButton.Click += BrowseButton_Click;

            // Status label
            statusLabel = new Label
            {
                Text = "Ready to install",
                Location = new Point(20, 145),
                Size = new Size(560, 25),
                ForeColor = Color.Blue
            };

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 175),
                Size = new Size(560, 25),
                Style = ProgressBarStyle.Continuous
            };

            // Log textbox
            logTextBox = new RichTextBox
            {
                Location = new Point(20, 210),
                Size = new Size(560, 180),
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9)
            };

            // Install button
            installButton = new Button
            {
                Text = "Install",
                Location = new Point(250, 410),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Green,
                ForeColor = Color.White
            };
            installButton.Click += InstallButton_Click;

            // Add controls
            this.Controls.AddRange(new Control[] {
                titleLabel, pathLabel, gamePathTextBox, browseButton,
                statusLabel, progressBar, logTextBox, installButton
            });
        }

        private void DetectGamePath()
        {
            Log("🔍 Searching for MIMESIS...");

            string[] possiblePaths = {
                @"C:\Program Files (x86)\Steam\steamapps\common\MIMESIS",
                @"C:\Program Files\Steam\steamapps\common\MIMESIS",
                @"D:\SteamLibrary\steamapps\common\MIMESIS",
                @"E:\SteamLibrary\steamapps\common\MIMESIS"
            };

            // Try to find Steam library folders from registry
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    var steamPath = key?.GetValue("SteamPath")?.ToString();
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        var libPath = Path.Combine(steamPath.Replace('/', '\\'), "steamapps", "common", "MIMESIS");
                        if (Directory.Exists(libPath))
                        {
                            gamePathTextBox.Text = libPath;
                            Log($"✓ Found at: {libPath}");
                            return;
                        }
                    }
                }
            }
            catch { }

            // Check common paths
            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    gamePathTextBox.Text = path;
                    Log($"✓ Found at: {path}");
                    return;
                }
            }

            Log("⚠ Could not auto-detect. Please browse manually.");
            statusLabel.Text = "Please select MIMESIS game folder";
            statusLabel.ForeColor = Color.Orange;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select MIMESIS game folder";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    gamePathTextBox.Text = dialog.SelectedPath;
                    Log($"Selected: {dialog.SelectedPath}");
                }
            }
        }

        private async void InstallButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(gamePathTextBox.Text) || !Directory.Exists(gamePathTextBox.Text))
            {
                MessageBox.Show("Please select a valid MIMESIS game folder", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            installButton.Enabled = false;
            browseButton.Enabled = false;

            try
            {
                await InstallMod();

                statusLabel.Text = "✅ Installation Complete!";
                statusLabel.ForeColor = Color.Green;

                MessageBox.Show(
                    "Installation completed successfully!\n\n" +
                    "Launch MIMESIS and create a lobby to test.\n" +
                    "Only the HOST needs this mod installed.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                Application.Exit();
            }
            catch (Exception ex)
            {
                statusLabel.Text = "❌ Installation failed";
                statusLabel.ForeColor = Color.Red;
                Log($"ERROR: {ex.Message}");

                MessageBox.Show($"Installation failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                installButton.Enabled = true;
                browseButton.Enabled = true;
            }
        }

        private async Task InstallMod()
        {
            string gamePath = gamePathTextBox.Text;
            string melonLoaderPath = Path.Combine(gamePath, "MelonLoader");
            string modsPath = Path.Combine(gamePath, "Mods");

            // Step 1: Check MelonLoader
            progressBar.Value = 10;
            Log("📦 Checking MelonLoader...");
            statusLabel.Text = "Checking MelonLoader...";

            if (!Directory.Exists(melonLoaderPath))
            {
                // Download MelonLoader
                progressBar.Value = 20;
                Log("⬇ Downloading MelonLoader...");
                statusLabel.Text = "Downloading MelonLoader...";

                string tempInstaller = Path.Combine(Path.GetTempPath(), "MelonLoader.Installer.exe");
                await DownloadFile(MELONLOADER_URL, tempInstaller);

                // Install MelonLoader
                progressBar.Value = 50;
                Log("🔧 Installing MelonLoader...");
                statusLabel.Text = "Installing MelonLoader...";

                bool deleteInstaller = true;

                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = tempInstaller,
                        Arguments = $"--path \"{gamePath}\" --automated",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                })
                {
                    process.Start();
                    await Task.Run(() => process.WaitForExit());

                    if (process.ExitCode != 0 && !Directory.Exists(melonLoaderPath))
                    {
                        // Manual installation needed
                        Log("⚠ Automated installation failed. Opening manual installer...");

                        using (var manualProcess = Process.Start(tempInstaller))
                        {
                            var result = MessageBox.Show(
                                "Please install MelonLoader manually:\n\n" +
                                "1. Select MIMESIS from the game list\n" +
                                "2. Click Install\n" +
                                "3. Click OK when done",
                                "Manual Installation Required",
                                MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Information
                            );

                            if (result == DialogResult.Cancel)
                            {
                                throw new Exception("Installation cancelled by user");
                            }

                            // Wait for manual installer to close
                            if (manualProcess != null && !manualProcess.HasExited)
                            {
                                await Task.Run(() => manualProcess.WaitForExit());
                            }
                        }

                        if (!Directory.Exists(melonLoaderPath))
                        {
                            throw new Exception("MelonLoader installation failed");
                        }
                    }
                }

                // Only delete if installer is not running
                if (deleteInstaller)
                {
                    try
                    {
                        File.Delete(tempInstaller);
                    }
                    catch
                    {
                        // Ignore if file is still in use
                    }
                }
            }
            else
            {
                Log("✓ MelonLoader already installed");
            }

            // Step 2: Install mod
            progressBar.Value = 70;
            Log("📦 Installing MorePlayers mod...");
            statusLabel.Text = "Installing mod...";

            Directory.CreateDirectory(modsPath);

            // Extract embedded DLL
            using (var stream = typeof(InstallerForm).Assembly.GetManifestResourceStream("MorePlayers.dll"))
            {
                if (stream == null)
                    throw new Exception("Mod DLL not found in installer");

                string modPath = Path.Combine(modsPath, "MorePlayers.dll");
                using (var fileStream = File.Create(modPath))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }

            progressBar.Value = 100;
            Log("✓ Mod installed successfully");
            Log($"✓ Location: {Path.Combine(modsPath, "MorePlayers.dll")}");
        }

        private async Task DownloadFile(string url, string outputPath)
        {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(outputPath))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }

        private void Log(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => Log(message)));
                return;
            }

            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            logTextBox.ScrollToCaret();
        }
    }
}
