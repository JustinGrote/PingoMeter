using System.Diagnostics;
using System.Media;
using System.Net;
using System.Runtime.Versioning;
using PingoMeter.vendor.StartupCreator;

namespace PingoMeter
{
    [SupportedOSPlatform("windows")]
    public class Setting : Form
    {
        bool loaded;
        SoundPlayer? testPlay;
        bool adjustingTimeout;
        bool adjustingStartup;
        readonly StartupCreator startupManager = new StartupViaRegistry();

        // UI Controls
        private Label? label1;
        private Label? label2;
        private Label? label3;
        private Label? label4;
        private Label? label5;
        private NumericUpDown? delay;
        private ColorDialog? colorDialog1;
        private Button? setBgColor;
        private Button? setGoodColor;
        private Button? setNormalColor;
        private Button? setBadColor;
        private Button? apply;
        private Button? reset;
        private Button? cancel;
        private Label? label12;
        private NumericUpDown? traceTimeout;
        private Label? labelTimeoutWarning;
        private Label? label7;
        private TextBox? ipAddress;
        private TabControl? tabControl1;
        private TabPage? tabPage1;
        private TabPage? tabPage2;
        private CheckBox? alarmTimeOut;
        private CheckBox? alarmConnectionLost;
        private GroupBox? groupBox1;
        private CheckBox? alarmResumed;
        private TabPage? tabPage3;
        private LinkLabel? linkLabel1;
        private Label? labelVersion;
        private Label? label9;
        private PictureBox? pictureBox1;
        private CheckBox? numbersModeCheckBox;
        private GroupBox? graphColorsGroupBox;
        private ToolTip? toolTip1;
        private Button? pingTimeoutSFXBtn;
        private GroupBox? groupBox2;
        private Label? label11;
        private Label? label10;
        private Label? label8;
        private Button? connectionResumeSFXBtn;
        private Button? connectionLostSFXBtn;
        private CheckBox? cbStartupRun;
        private CheckBox? cbOfflineCounter;

        public Setting()
        {
            InitializeComponent();
            SyncFromConfig();
            if (labelVersion != null)
                labelVersion.Text = "Version " + Program.VERSION;
            if (toolTip1 != null && numbersModeCheckBox != null)
                toolTip1.SetToolTip(numbersModeCheckBox, "Use numbers for the ping instead of a graph.");

            if (pingTimeoutSFXBtn != null)
            {
                pingTimeoutSFXBtn.Click += SelectWAV;
                pingTimeoutSFXBtn.MouseDown += (s, e) => ClearSFX(pingTimeoutSFXBtn, e);
            }
            if (connectionLostSFXBtn != null)
            {
                connectionLostSFXBtn.Click += SelectWAV;
                connectionLostSFXBtn.MouseDown += (s, e) => ClearSFX(connectionLostSFXBtn, e);
            }
            if (connectionResumeSFXBtn != null)
            {
                connectionResumeSFXBtn.Click += SelectWAV;
                connectionResumeSFXBtn.MouseDown += (s, e) => ClearSFX(connectionResumeSFXBtn, e);
            }

            if (delay != null)
                delay.ValueChanged += (s, e) => ValidateTimeoutAgainstInterval(showWarning: true);
            if (traceTimeout != null)
                traceTimeout.ValueChanged += (s, e) => ValidateTimeoutAgainstInterval(showWarning: true);
            if (cbStartupRun != null)
                cbStartupRun.CheckedChanged += CbStartupRun_CheckedChanged;

            if (Utils.IsWindows8Next())
            {
                if (cbStartupRun != null)
                {
                    cbStartupRun.Enabled = false;
                    cbStartupRun.Visible = false;
                }
                Config.RunOnStartup = false;
                if (cbStartupRun != null)
                    cbStartupRun.Checked = false;
            }

            loaded = true;
        }

        private void InitializeComponent()
        {
            // Initialize components
            toolTip1 = new ToolTip();
            colorDialog1 = new ColorDialog();

            // Initialize form
            Text = "Setting";
            ClientSize = new Size(364, 392);
            BackColor = SystemColors.Window;
            Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7F, 15F);

            // Create tab control
            tabControl1 = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(339, 335),
                Padding = new Point(30, 3),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Create Basic tab
            tabPage1 = new TabPage
            {
                Text = "Basic",
                BackColor = SystemColors.Control,
                Padding = new Padding(3)
            };

            label1 = new Label
            {
                Text = "Ping Interval (ms):",
                Location = new Point(6, 9),
                AutoSize = true
            };

            delay = new NumericUpDown
            {
                Location = new Point(144, 7),
                Size = new Size(82, 22),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9F),
                Minimum = 50,
                Maximum = 60000,
                Increment = 250,
                Value = 1000,
                TextAlign = HorizontalAlignment.Center,
                ThousandsSeparator = true
            };

            label12 = new Label
            {
                Text = "Timeout (ms):",
                Location = new Point(6, 36),
                AutoSize = true
            };

            traceTimeout = new NumericUpDown
            {
                Location = new Point(144, 34),
                Size = new Size(82, 22),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9F),
                Minimum = 100,
                Maximum = 10000,
                Increment = 100,
                Value = 1000,
                TextAlign = HorizontalAlignment.Center,
                ThousandsSeparator = true
            };

            labelTimeoutWarning = new Label
            {
                Text = "Timeout cannot exceed interval.",
                Location = new Point(6, 62),
                AutoSize = true,
                ForeColor = Color.Firebrick,
                Visible = false
            };

            cbStartupRun = new CheckBox
            {
                Text = "Run on Windows startup",
                Location = new Point(6, 82),
                AutoSize = true
            };

            numbersModeCheckBox = new CheckBox
            {
                Text = "Numbers mode (ping from 0 to 99)",
                Location = new Point(6, 105),
                AutoSize = true
            };
            numbersModeCheckBox.CheckedChanged += numbersModeCheckBox_CheckedChanged;

            // Graph colors group box
            graphColorsGroupBox = new GroupBox
            {
                Text = "Graph colors",
                Location = new Point(9, 138),
                Size = new Size(316, 150),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            label2 = new Label
            {
                Text = "Background color:",
                Location = new Point(17, 29),
                AutoSize = true
            };

            setBgColor = new Button
            {
                BackColor = Color.FromArgb(70, 0, 0),
                Location = new Point(130, 25),
                Size = new Size(23, 23),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            setBgColor.Click += SetBgColor_Click;

            label3 = new Label
            {
                Text = "Good ping color:",
                Location = new Point(17, 58),
                AutoSize = true
            };

            setGoodColor = new Button
            {
                BackColor = Color.FromArgb(120, 180, 0),
                Location = new Point(130, 54),
                Size = new Size(23, 23),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            setGoodColor.Click += SetGoodColor_Click;

            label4 = new Label
            {
                Text = "Normal ping color:",
                Location = new Point(17, 87),
                AutoSize = true
            };

            setNormalColor = new Button
            {
                BackColor = Color.FromArgb(255, 180, 0),
                Location = new Point(130, 83),
                Size = new Size(23, 23),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            setNormalColor.Click += SetNormalColor_Click;

            label5 = new Label
            {
                Text = "Bad ping color:",
                Location = new Point(17, 116),
                AutoSize = true
            };

            setBadColor = new Button
            {
                BackColor = Color.Red,
                Location = new Point(130, 112),
                Size = new Size(23, 23),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            setBadColor.Click += SetBadColor_Click;

            graphColorsGroupBox.Controls.AddRange(new Control[] { label2, setBgColor, label3, setGoodColor, label4, setNormalColor, label5, setBadColor });
            tabPage1.Controls.AddRange(new Control[] { graphColorsGroupBox, numbersModeCheckBox, cbStartupRun, labelTimeoutWarning, traceTimeout, delay, label12, label1 });

            // Create Advanced tab
            tabPage2 = new TabPage
            {
                Text = "Advanced",
                BackColor = SystemColors.Control,
                Padding = new Padding(3)
            };

            label7 = new Label
            {
                Text = "Target IP Address:",
                Location = new Point(7, 7),
                AutoSize = true
            };

            ipAddress = new TextBox
            {
                Text = "8.8.8.8",
                Location = new Point(145, 6),
                Size = new Size(172, 22),
                Font = new Font("Consolas", 9F)
            };

            groupBox1 = new GroupBox
            {
                Text = "Balloon tip alarm when:",
                Location = new Point(6, 34),
                Size = new Size(311, 93)
            };

            alarmTimeOut = new CheckBox
            {
                Text = "Ping timeout",
                Location = new Point(6, 20),
                AutoSize = true
            };

            alarmConnectionLost = new CheckBox
            {
                Text = "Connection lost",
                Location = new Point(6, 45),
                AutoSize = true
            };

            alarmResumed = new CheckBox
            {
                Text = "Connection resume",
                Location = new Point(6, 70),
                AutoSize = true
            };

            groupBox1.Controls.AddRange(new Control[] { alarmResumed, alarmConnectionLost, alarmTimeOut });

            groupBox2 = new GroupBox
            {
                Text = "Alarm sound when (right click to clear):",
                Location = new Point(6, 133),
                Size = new Size(311, 116)
            };

            label8 = new Label
            {
                Text = "Ping timeout:",
                Location = new Point(6, 26),
                AutoSize = true
            };

            pingTimeoutSFXBtn = new Button
            {
                Text = "none",
                Location = new Point(128, 23),
                Size = new Size(177, 23),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, 204)
            };

            label10 = new Label
            {
                Text = "Connection lost:",
                Location = new Point(6, 55),
                AutoSize = true
            };

            connectionLostSFXBtn = new Button
            {
                Text = "none",
                Location = new Point(128, 52),
                Size = new Size(177, 23),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, 204)
            };

            label11 = new Label
            {
                Text = "Connection resume:",
                Location = new Point(6, 84),
                AutoSize = true
            };

            connectionResumeSFXBtn = new Button
            {
                Text = "none",
                Location = new Point(128, 81),
                Size = new Size(177, 23),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, 204)
            };

            groupBox2.Controls.AddRange(new Control[] { label11, connectionResumeSFXBtn, label10, connectionLostSFXBtn, label8, pingTimeoutSFXBtn });

            cbOfflineCounter = new CheckBox
            {
                Text = "Offline time counter",
                Location = new Point(6, 255),
                AutoSize = true
            };

            tabPage2.Controls.AddRange(new Control[] { cbOfflineCounter, groupBox2, groupBox1, ipAddress, label7 });

            // Create About tab
            tabPage3 = new TabPage
            {
                Text = "About",
                BackColor = SystemColors.Control
            };

            pictureBox1 = new PictureBox
            {
                Location = new Point(8, 8),
                Size = new Size(64, 64)
            };

            label9 = new Label
            {
                Text = "PingoMeter",
                Location = new Point(78, 8),
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, 204)
            };

            labelVersion = new Label
            {
                Text = "Version x.x.x",
                Location = new Point(78, 27),
                AutoSize = true
            };

            linkLabel1 = new LinkLabel
            {
                Text = "Source code on GitHub",
                Location = new Point(78, 46),
                AutoSize = true
            };
            linkLabel1.LinkClicked += LinkLabel1_LinkClicked;

            tabPage3.Controls.AddRange(new Control[] { linkLabel1, labelVersion, label9, pictureBox1 });

            // Add tabs to tab control
            tabControl1.Controls.AddRange(new Control[] { tabPage1, tabPage2, tabPage3 });

            // Create bottom buttons
            apply = new Button
            {
                Text = "Apply",
                Location = new Point(12, 353),
                Size = new Size(75, 26),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                DialogResult = DialogResult.OK
            };
            apply.Click += Apply_Click;

            cancel = new Button
            {
                Text = "Cancel",
                Location = new Point(93, 353),
                Size = new Size(75, 26),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            cancel.Click += Cancel_Click;

            reset = new Button
            {
                Text = "Reset",
                Location = new Point(276, 353),
                Size = new Size(75, 26),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            reset.Click += Reset_Click;

            // Add controls to form
            Controls.AddRange(new Control[] { reset, cancel, apply, tabControl1 });
        }

        private void SyncToConfig(IPAddress address)
        {
            Config.SetAll(
                interval: delay != null ? (int)delay.Value : Config.Interval,
                timeout: traceTimeout != null ? (int)traceTimeout.Value : Config.Timeout,
                bgColor: setBgColor?.BackColor ?? Color.FromArgb(70, 0, 0),
                goodColor: setGoodColor?.BackColor ?? Color.FromArgb(120, 180, 0),
                normalColor: setNormalColor?.BackColor ?? Color.FromArgb(255, 180, 0),
                badColor: setBadColor?.BackColor ?? Color.Red,
                runOnStartup: cbStartupRun?.Checked ?? false,
                address: address,
                alarmConnectionLost: alarmConnectionLost?.Checked ?? false,
                alarmTimeOut: alarmTimeOut?.Checked ?? false,
                alarmResumed: alarmResumed?.Checked ?? false,
                useNumbers: numbersModeCheckBox?.Checked ?? false,
                _SFXConnectionLost: (toolTip1 != null && connectionLostSFXBtn != null) ? toolTip1.GetToolTip(connectionLostSFXBtn) ?? Config.NONE_SFX : Config.NONE_SFX,
                _SFXTimeOut: (toolTip1 != null && pingTimeoutSFXBtn != null) ? toolTip1.GetToolTip(pingTimeoutSFXBtn) ?? Config.NONE_SFX : Config.NONE_SFX,
                _SFXResumed: (toolTip1 != null && connectionResumeSFXBtn != null) ? toolTip1.GetToolTip(connectionResumeSFXBtn) ?? Config.NONE_SFX : Config.NONE_SFX,
                offlineCounter: cbOfflineCounter?.Checked ?? false);
        }

        private void SyncFromConfig()
        {
            if (delay != null)
                delay.Value = Config.Interval;
            if (traceTimeout != null)
                traceTimeout.Value = Config.Timeout;
            ValidateTimeoutAgainstInterval(showWarning: false);
            if (Config.BgColor != null && setBgColor != null)
                setBgColor.BackColor = Config.BgColor.Color;
            if (Config.GoodColor != null && setGoodColor != null)
                setGoodColor.BackColor = Config.GoodColor.Color;
            if (Config.NormalColor != null && setNormalColor != null)
                setNormalColor.BackColor = Config.NormalColor.Color;
            if (Config.BadColor != null && setBadColor != null)
                setBadColor.BackColor = Config.BadColor.Color;

            if (alarmTimeOut != null)
                alarmTimeOut.Checked = Config.AlarmTimeOut;
            if (alarmConnectionLost != null)
                alarmConnectionLost.Checked = Config.AlarmConnectionLost;
            if (alarmResumed != null)
                alarmResumed.Checked = Config.AlarmResumed;
            if (numbersModeCheckBox != null)
                numbersModeCheckBox.Checked = Config.UseNumbers;
            if (cbStartupRun != null)
                cbStartupRun.Checked = Config.RunOnStartup;
            if (cbOfflineCounter != null)
                cbOfflineCounter.Checked = Config.OfflineCounter;

            //isStartUp.Checked = Config.s_runOnStartup;

            if (Config.TheIPAddress != null && ipAddress != null)
                ipAddress.Text = Config.TheIPAddress.ToString();

            SetSoundInfoForButtom(pingTimeoutSFXBtn, Config.SFXTimeOut);
            SetSoundInfoForButtom(connectionLostSFXBtn, Config.SFXConnectionLost);
            SetSoundInfoForButtom(connectionResumeSFXBtn, Config.SFXResumed);
        }

        private void ClearSFX(Button button, MouseEventArgs mouseEvent)
        {
            if (mouseEvent.Button == MouseButtons.Right)
                SetSoundInfoForButtom(button, null);
        }

        private void SetSoundInfoForButtom(Button? button, string? pathToFile)
        {
            if (button == null || toolTip1 == null)
                return;

            if (string.IsNullOrWhiteSpace(pathToFile) || pathToFile == Config.NONE_SFX || !File.Exists(pathToFile))
            {
                button.Text = Config.NONE_SFX;
                toolTip1.SetToolTip(button, Config.NONE_SFX);
            }
            else
            {
                button.Text = Path.GetFileNameWithoutExtension(pathToFile);
                toolTip1.SetToolTip(button, pathToFile);

                if (loaded)
                {
                    if (testPlay == null)
                        testPlay = new SoundPlayer();

                    if (testPlay.SoundLocation != pathToFile)
                    {
                        testPlay.SoundLocation = pathToFile;
                        try
                        {
                            testPlay.Load();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message + "\n\nFile: " + pathToFile, "Load sound error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    if (testPlay.IsLoadCompleted)
                        testPlay.Play();
                }
            }
        }

        private void SelectWAV(object? senderAsButton, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                DefaultExt = "",
                InitialDirectory = @"C:\Windows\Media\",

                // Filter string you provided is not valid. The filter string must contain a description of the filter,
                // followed by the vertical bar (|) and the filter pattern. The strings for different filtering options
                // must also be separated by the vertical bar.
                // Example: "Text files (*.txt)|*.txt|All files (*.*)|*.*"
                Filter = "WAV file (*.wav)|*.wav",
                Multiselect = false,
                Title = "Select .wav file",
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SetSoundInfoForButtom(senderAsButton as Button, dialog.FileName);
            }

            dialog.Dispose();
        }

        private void SetBgColor_Click(object? sender, EventArgs e)
        {
            if (colorDialog1 != null && colorDialog1.ShowDialog() == DialogResult.OK && setBgColor != null)
            {
                setBgColor.BackColor = colorDialog1.Color;
            }
        }

        private void SetGoodColor_Click(object? sender, EventArgs e)
        {
            if (colorDialog1 != null && colorDialog1.ShowDialog() == DialogResult.OK && setGoodColor != null)
            {
                setGoodColor.BackColor = colorDialog1.Color;
            }
        }

        private void SetNormalColor_Click(object? sender, EventArgs e)
        {
            if (colorDialog1 != null && colorDialog1.ShowDialog() == DialogResult.OK && setNormalColor != null)
            {
                setNormalColor.BackColor = colorDialog1.Color;
            }
        }

        private void SetBadColor_Click(object? sender, EventArgs e)
        {
            if (colorDialog1 != null && colorDialog1.ShowDialog() == DialogResult.OK && setBadColor != null)
            {
                setBadColor.BackColor = colorDialog1.Color;
            }
        }

        private void ValidateTimeoutAgainstInterval(bool showWarning)
        {
            if (adjustingTimeout || traceTimeout == null || delay == null || labelTimeoutWarning == null)
                return;

            if (traceTimeout.Value > delay.Value)
            {
                adjustingTimeout = true;
                traceTimeout.Value = delay.Value;
                adjustingTimeout = false;

                if (showWarning)
                    labelTimeoutWarning.Visible = true;
                return;
            }

            if (showWarning)
                labelTimeoutWarning.Visible = false;
        }

        private void CbStartupRun_CheckedChanged(object? sender, EventArgs e)
        {
            if (!loaded || adjustingStartup || cbStartupRun == null)
                return;

            bool success = cbStartupRun.Checked
                ? startupManager.RunOnStartup()
                : startupManager.RemoveFromStartup();

            if (success)
                return;

            adjustingStartup = true;
            cbStartupRun.Checked = !cbStartupRun.Checked;
            adjustingStartup = false;
            MessageBox.Show("Unable to update Windows startup settings.", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void Apply_Click(object? sender, EventArgs e)
        {
            // check ip address
            if (ipAddress == null || !IPAddress.TryParse(ipAddress.Text, out IPAddress? address) || address == null)
            {
                MessageBox.Show("IP Address is invalid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SyncToConfig(address);
            Config.Save();
            Close();
        }

        private void Cancel_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void Reset_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Reset all settings to default?",
                "Reset all?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)
                == DialogResult.Yes)
            {
                Config.Reset();
                SyncFromConfig();
            }
        }

        private void LinkLabel1_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/JustinGrote/PingoMeter");
        }

        private void numbersModeCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (graphColorsGroupBox != null && numbersModeCheckBox != null)
                graphColorsGroupBox.Visible = !numbersModeCheckBox.Checked;
        }
    }
}
