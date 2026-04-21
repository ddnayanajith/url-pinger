using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UrlPingerApp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly TextBox urlTextBox;
        private readonly NumericUpDown intervalInput;
        private readonly NumericUpDown timeoutInput;
        private readonly Label statusValue;
        private readonly Label lastResultValue;
        private readonly RichTextBox logBox;
        private readonly Panel logPanel;
        private readonly Button startButton;
        private readonly Button stopButton;
        private readonly Button sendNowButton;
        private readonly Button clearButton;
        private readonly Button helpButton;
        private readonly System.Windows.Forms.Timer requestTimer;

        private bool isRunning;
        private bool requestInProgress;

        public MainForm()
        {
            Text = "URL Pinger";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(560, 420);
            MinimumSize = new Size(560, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10F);

            var urlLabel = new Label
            {
                Text = "URL",
                Location = new Point(20, 22),
                AutoSize = true
            };
            Controls.Add(urlLabel);

            urlTextBox = new TextBox
            {
                Location = new Point(20, 46),
                Size = new Size(500, 27),
                Text = "https://oneapp.hutch.lk/"
            };
            Controls.Add(urlTextBox);

            var intervalLabel = new Label
            {
                Text = "Interval (seconds)",
                Location = new Point(20, 90),
                AutoSize = true
            };
            Controls.Add(intervalLabel);

            intervalInput = new NumericUpDown
            {
                Location = new Point(20, 114),
                Size = new Size(120, 27),
                Minimum = 1,
                Maximum = 86400,
                Value = 60
            };
            intervalInput.ValueChanged += IntervalInputOnValueChanged;
            Controls.Add(intervalInput);

            var timeoutLabel = new Label
            {
                Text = "Timeout (seconds)",
                Location = new Point(170, 90),
                AutoSize = true
            };
            Controls.Add(timeoutLabel);

            timeoutInput = new NumericUpDown
            {
                Location = new Point(170, 114),
                Size = new Size(120, 27),
                Minimum = 1,
                Maximum = 300,
                Value = 10
            };
            timeoutInput.ValueChanged += TimeoutInputOnValueChanged;
            Controls.Add(timeoutInput);

            var statusTitle = new Label
            {
                Text = "Status",
                Location = new Point(20, 160),
                AutoSize = true
            };
            Controls.Add(statusTitle);

            statusValue = new Label
            {
                Text = "Stopped",
                Location = new Point(20, 184),
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 40, 40)
            };
            Controls.Add(statusValue);

            var lastResultLabel = new Label
            {
                Text = "Last result",
                Location = new Point(170, 160),
                AutoSize = true
            };
            Controls.Add(lastResultLabel);

            lastResultValue = new Label
            {
                Text = "No request yet",
                Location = new Point(170, 184),
                AutoSize = true
            };
            Controls.Add(lastResultValue);

            logPanel = new Panel
            {
                Location = new Point(20, 220),
                Size = new Size(500, 120),
                BackColor = Color.FromArgb(205, 205, 205)
            };
            Controls.Add(logPanel);

            logBox = new RichTextBox
            {
                Location = new Point(1, 1),
                Size = new Size(498, 118),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Control,
                Font = new Font("Consolas", 9.5F)
            };
            logPanel.Controls.Add(logBox);

            startButton = new RoundedButton
            {
                Text = "Start",
                Location = new Point(20, 350),
                Size = new Size(100, 32)
            };
            StyleButton(startButton, Color.FromArgb(197, 233, 213), Color.FromArgb(18, 82, 49), Color.FromArgb(132, 196, 159));
            startButton.Click += StartButtonOnClick;
            Controls.Add(startButton);

            stopButton = new RoundedButton
            {
                Text = "Stop",
                Location = new Point(130, 350),
                Size = new Size(100, 32),
                Enabled = false
            };
            StyleButton(stopButton, Color.FromArgb(245, 208, 208), Color.FromArgb(112, 35, 35), Color.FromArgb(219, 144, 144));
            stopButton.Click += StopButtonOnClick;
            Controls.Add(stopButton);

            sendNowButton = new RoundedButton
            {
                Text = "Send Now",
                Location = new Point(240, 350),
                Size = new Size(100, 32)
            };
            StyleButton(sendNowButton, Color.FromArgb(203, 222, 246), Color.FromArgb(25, 64, 112), Color.FromArgb(139, 179, 224));
            sendNowButton.Click += async (sender, args) => await RunCurlAsync(true);
            Controls.Add(sendNowButton);

            clearButton = new RoundedButton
            {
                Text = "Clear Log",
                Location = new Point(350, 350),
                Size = new Size(100, 32)
            };
            StyleButton(clearButton, Color.FromArgb(224, 228, 235), Color.FromArgb(45, 52, 63), Color.FromArgb(184, 194, 207));
            clearButton.Click += (sender, args) => logBox.Clear();
            Controls.Add(clearButton);

            helpButton = new RoundedButton
            {
                Text = "About",
                Location = new Point(460, 350),
                Size = new Size(60, 32)
            };
            StyleButton(helpButton, Color.FromArgb(218, 225, 236), Color.FromArgb(38, 47, 61), Color.FromArgb(174, 187, 204));
            helpButton.Click += HelpButtonOnClick;
            Controls.Add(helpButton);

            requestTimer = new System.Windows.Forms.Timer();
            requestTimer.Interval = IntervalToMilliseconds();
            requestTimer.Tick += async (sender, args) => await RunCurlAsync(false);

            FormClosing += (sender, args) => requestTimer.Stop();
            Shown += MainFormOnShown;

            UpdateRunningState(false);
        }

        private void MainFormOnShown(object sender, EventArgs e)
        {
            urlTextBox.SelectionLength = 0;
            startButton.Focus();
        }

        private static void StyleButton(Button button, Color background, Color foreground, Color border)
        {
            button.BackColor = background;
            button.ForeColor = foreground;
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderColor = border;
            button.FlatAppearance.BorderSize = button is RoundedButton ? 0 : 1;

            var roundedButton = button as RoundedButton;
            if (roundedButton != null)
            {
                roundedButton.BorderColor = border;
                roundedButton.BorderRadius = 5;
            }
        }

        private async void StartButtonOnClick(object sender, EventArgs e)
        {
            if (!ValidateUrl())
            {
                return;
            }

            requestTimer.Interval = IntervalToMilliseconds();
            UpdateRunningState(true);
            AddLogLine("Started with " + intervalInput.Value + "s interval");
            await RunCurlAsync(false);
            requestTimer.Start();
        }

        private void StopButtonOnClick(object sender, EventArgs e)
        {
            requestTimer.Stop();
            UpdateRunningState(false);
            AddLogLine("Stopped");
        }

        private void HelpButtonOnClick(object sender, EventArgs e)
        {
            using (var helpForm = new Form())
            {
                helpForm.Text = "About URL Pinger";
                helpForm.StartPosition = FormStartPosition.CenterParent;
                helpForm.ClientSize = new Size(650, 420);
                helpForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                helpForm.MaximizeBox = false;
                helpForm.MinimizeBox = false;
                helpForm.Font = new Font("Segoe UI", 9.5F);

                var title = new Label
                {
                    Text = "Last Result Meanings",
                    Location = new Point(20, 16),
                    Size = new Size(400, 28),
                    Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold)
                };
                helpForm.Controls.Add(title);

                var subtitle = new Label
                {
                    Text = "Use this list to understand what the app is showing after each request.",
                    Location = new Point(22, 48),
                    Size = new Size(600, 24),
                    ForeColor = Color.FromArgb(90, 90, 90)
                };
                helpForm.Controls.Add(subtitle);

                var grid = new DataGridView
                {
                    Location = new Point(20, 84),
                    Size = new Size(610, 280),
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                    ReadOnly = true,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect
                };

                grid.Columns.Add("Output", "Output");
                grid.Columns.Add("Meaning", "Meaning");
                grid.Columns[0].Width = 210;
                grid.Columns[1].Width = 360;
                grid.Columns[0].DefaultCellStyle.Font = new Font("Consolas", 9.2F, FontStyle.Bold);
                grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

                grid.Rows.Add("No request yet", "Nothing has run yet.");
                grid.Rows.Add("Running curl...", "One request is currently being sent.");
                grid.Rows.Add("OK - curl exit 0", "The curl request completed successfully.");
                grid.Rows.Add("FAIL - curl exit 7 - ...", "Curl could not connect to the server or network.");
                grid.Rows.Add("FAIL - curl exit 28 - ...", "Usually means the request timed out.");
                grid.Rows.Add("FAIL - curl exit [number] - ...", "Curl failed with that exit code and error text.");
                grid.Rows.Add("FAIL - Timed out after [seconds] seconds", "The app killed curl because it exceeded your timeout setting.");
                grid.Rows.Add("Stopped", "Appears in the log when you click Stop. It is not usually shown as Last result.");

                helpForm.Controls.Add(grid);

                var closeButton = new Button
                {
                    Text = "Close",
                    Location = new Point(530, 376),
                    Size = new Size(100, 32)
                };
                closeButton.Click += (closeSender, closeArgs) => helpForm.Close();
                helpForm.Controls.Add(closeButton);

                var creatorLabel = new LinkLabel
                {
                    Text = "Created by D D Nayanajith",
                    Location = new Point(20, 382),
                    Size = new Size(230, 22),
                    LinkColor = Color.FromArgb(65, 88, 120),
                    ActiveLinkColor = Color.FromArgb(45, 70, 105),
                    VisitedLinkColor = Color.FromArgb(65, 88, 120)
                };
                creatorLabel.Links.Add(11, 14, "https://github.com/ddnayanajith/");
                creatorLabel.LinkClicked += (linkSender, linkArgs) =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = linkArgs.Link.LinkData.ToString(),
                        UseShellExecute = true
                    });
                };
                helpForm.Controls.Add(creatorLabel);

                helpForm.ShowDialog(this);
            }
        }

        private async Task RunCurlAsync(bool force)
        {
            if (!isRunning && !force)
            {
                return;
            }

            if (requestInProgress)
            {
                return;
            }

            if (!ValidateUrl())
            {
                return;
            }

            requestInProgress = true;
            lastResultValue.Text = "Running curl...";

            try
            {
                var result = await ExecuteCurlAsync(urlTextBox.Text.Trim(), (int)timeoutInput.Value);

                if (result.ExitCode == 0)
                {
                    lastResultValue.Text = "OK - curl exit 0";
                    AddLogLine("OK  curl exit 0");
                }
                else
                {
                    var detail = string.IsNullOrWhiteSpace(result.ErrorText)
                        ? "curl exit " + result.ExitCode
                        : "curl exit " + result.ExitCode + " - " + result.ErrorText;

                    lastResultValue.Text = "FAIL - " + detail;
                    AddLogLine("FAIL  " + detail);
                }
            }
            catch (Exception ex)
            {
                var detail = FlattenException(ex);
                lastResultValue.Text = "FAIL - " + detail;
                AddLogLine("FAIL  " + detail);
            }
            finally
            {
                requestInProgress = false;
            }
        }

        private static Task<CurlResult> ExecuteCurlAsync(string url, int timeoutSeconds)
        {
            return Task.Run(() =>
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "curl.exe",
                        Arguments = "-s -L " + QuoteArgument(url),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    process.Start();

                    if (!process.WaitForExit(timeoutSeconds * 1000))
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                        }

                        return new CurlResult
                        {
                            ExitCode = -1,
                            ErrorText = "Timed out after " + timeoutSeconds + " seconds"
                        };
                    }

                    return new CurlResult
                    {
                        ExitCode = process.ExitCode,
                        ErrorText = process.StandardError.ReadToEnd().Trim()
                    };
                }
            });
        }

        private bool ValidateUrl()
        {
            Uri uri;
            var isValid = Uri.TryCreate(urlTextBox.Text.Trim(), UriKind.Absolute, out uri);
            if (isValid)
            {
                return true;
            }

            MessageBox.Show("Enter a valid absolute URL.", "URL Pinger", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void IntervalInputOnValueChanged(object sender, EventArgs e)
        {
            requestTimer.Interval = IntervalToMilliseconds();
            UpdateWindowTitle();

            if (isRunning)
            {
                AddLogLine("Interval changed to " + intervalInput.Value + "s");
            }
        }

        private void TimeoutInputOnValueChanged(object sender, EventArgs e)
        {
            if (isRunning)
            {
                AddLogLine("Timeout changed to " + timeoutInput.Value + "s");
            }
        }

        private void UpdateRunningState(bool running)
        {
            isRunning = running;
            startButton.Enabled = !running;
            stopButton.Enabled = running;
            statusValue.Text = running ? "Running" : "Stopped";
            statusValue.ForeColor = running ? Color.FromArgb(30, 120, 40) : Color.FromArgb(180, 40, 40);
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            Text = isRunning ? "URL Pinger - " + intervalInput.Value + "s" : "URL Pinger";
        }

        private void AddLogLine(string message)
        {
            var lineColor = GetLogColor(message);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  ";

            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = Color.FromArgb(95, 95, 95);
            logBox.AppendText(timestamp);
            logBox.SelectionColor = lineColor;
            logBox.AppendText(message + Environment.NewLine);
            logBox.SelectionColor = logBox.ForeColor;
            logBox.ScrollToCaret();
        }

        private static Color GetLogColor(string message)
        {
            if (message.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb(43, 125, 79);
            }

            if (message.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb(164, 73, 73);
            }

            if (message.StartsWith("Started", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("Interval", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("Timeout", StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb(67, 98, 140);
            }

            if (message.StartsWith("Stopped", StringComparison.OrdinalIgnoreCase))
            {
                return Color.FromArgb(112, 112, 112);
            }

            return Color.FromArgb(45, 45, 45);
        }

        private int IntervalToMilliseconds()
        {
            return decimal.ToInt32(intervalInput.Value) * 1000;
        }

        private static string FlattenException(Exception ex)
        {
            var text = ex.Message;
            var current = ex.InnerException;

            while (current != null)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                {
                    text += " | " + current.Message;
                }

                current = current.InnerException;
            }

            return text;
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private sealed class CurlResult
        {
            public int ExitCode { get; set; }

            public string ErrorText { get; set; }
        }

        private sealed class RoundedButton : Button
        {
            public Color BorderColor { get; set; }

            public int BorderRadius { get; set; }

            public RoundedButton()
            {
                BorderColor = Color.FromArgb(200, 200, 200);
                BorderRadius = 7;
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                UseVisualStyleBackColor = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(Parent == null ? SystemColors.Control : Parent.BackColor);

                var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = CreateRoundedRectangle(bounds, BorderRadius))
                using (var backgroundBrush = new SolidBrush(Enabled ? BackColor : Color.FromArgb(245, 245, 245)))
                using (var borderPen = new Pen(Enabled ? BorderColor : Color.FromArgb(225, 225, 225)))
                using (var textBrush = new SolidBrush(Enabled ? ForeColor : Color.FromArgb(155, 155, 155)))
                {
                    e.Graphics.FillPath(backgroundBrush, path);
                    e.Graphics.DrawPath(borderPen, path);

                    var textFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    e.Graphics.DrawString(Text, Font, textBrush, bounds, textFormat);
                }
            }

            private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
            {
                var path = new GraphicsPath();
                var diameter = radius * 2;

                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();

                return path;
            }
        }
    }
}
