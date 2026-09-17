using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LudoGame;
using Microsoft.AspNetCore.SignalR.Client;
using GameColor = LudoGame.Color;

namespace LudoGame.Gui
{
    internal sealed class MainForm : Form
    {
        private readonly SnapshotGameLogger _logger = new();
        private readonly Dictionary<GameColor, Label> _playerSummary = new();
        private readonly Dictionary<GameColor, Label> _playerPieces = new();
        private readonly Dictionary<GameColor, Label> _playerPlacements = new();
        private readonly Dictionary<GameColor, Panel> _playerAccents = new();
        private readonly RichTextBox _logBox = new();
        private readonly Label _statusLabel = new();
        private readonly Label _sectionLabel = new();
        private readonly BoardCanvas _boardCanvas = new();
        private readonly TrackBar _speedSlider = new();
        private readonly Button _startButton = new();
        private readonly Button _resetButton = new();
        private readonly Label _mysteryLabel = new();

        private GameSnapshot _lastSnapshot = new();
        private int _lastLogLineCount;
        private bool _gameRunning;
        private HubConnection? _hubConnection;

        public MainForm()
        {
            Text = "LUDO-T Arena";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 780);
            MinimumSize = new Size(960, 600);
            BackColor = System.Drawing.Color.FromArgb(12, 14, 20);
            Font = new Font("Segoe UI", 9f);

            InitializeLayout();

            _logger.SnapshotChanged += OnSnapshotChanged;
            _logger.DelayMilliseconds = 12;

            ApplySnapshot(_logger.GetSnapshot());
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                BringToFront();
                Activate();
            }
            catch { }
        }

        private void InitializeLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = BackColor,
                Padding = new Padding(16)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildBody(), 0, 1);
            Controls.Add(root);
        }

        private Control BuildHeader()
        {
            var headerGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = System.Drawing.Color.FromArgb(23, 27, 37)
            };
            headerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360f));
            headerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 510f));

            headerGrid.Paint += (_, e) =>
            {
                using var pen = new Pen(System.Drawing.Color.FromArgb(58, 66, 84), 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, headerGrid.Width - 1, headerGrid.Height - 1);
            };

            // Title Box
            var titleBox = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12, 10, 0, 0) };
            var title = new Label
            {
                AutoSize = true,
                Text = "LUDO-T Arena",
                ForeColor = System.Drawing.Color.WhiteSmoke,
                Font = new Font("Segoe UI Semibold", 21f, FontStyle.Bold)
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Text = "Live simulation dashboard built on the existing C# game engine.",
                ForeColor = System.Drawing.Color.FromArgb(190, 200, 220),
                Margin = new Padding(3, -2, 0, 0)
            };
            titleBox.Controls.Add(title);
            titleBox.Controls.Add(subtitle);

            // Status Box
            var statusBox = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(0, 18, 0, 0) };
            statusBox.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            statusBox.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            _statusLabel.AutoSize = true;
            _statusLabel.ForeColor = System.Drawing.Color.FromArgb(224, 232, 244);
            _statusLabel.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);

            _sectionLabel.AutoSize = true;
            _sectionLabel.ForeColor = System.Drawing.Color.FromArgb(180, 194, 215);

            statusBox.Controls.Add(_statusLabel, 0, 0);
            statusBox.Controls.Add(_sectionLabel, 0, 1);

            // Controls Box
            var controlsBox = new Panel { Dock = DockStyle.Fill };
            var speedCaption = new Label
            {
                AutoSize = true,
                Text = "Speed",
                ForeColor = System.Drawing.Color.FromArgb(200, 210, 228),
                Location = new Point(0, 14)
            };

            _speedSlider.Minimum = 0;
            _speedSlider.Maximum = 90;
            _speedSlider.Value = 12;
            _speedSlider.TickFrequency = 10;
            _speedSlider.SmallChange = 3;
            _speedSlider.LargeChange = 12;
            _speedSlider.Width = 190;
            _speedSlider.Location = new Point(-6, 38);
            _speedSlider.Scroll += (_, _) => _logger.DelayMilliseconds = _speedSlider.Value;

            _startButton.Text = "Start simulation";
            _startButton.BackColor = System.Drawing.Color.FromArgb(46, 107, 255);
            _startButton.ForeColor = System.Drawing.Color.White;
            _startButton.FlatStyle = FlatStyle.Flat;
            _startButton.FlatAppearance.BorderSize = 0;
            _startButton.Width = 140;
            _startButton.Height = 36;
            _startButton.Location = new Point(200, 14);
            _startButton.Click += async (_, _) => await StartGameAsync();

            _resetButton.Text = "Reset view";
            _resetButton.BackColor = System.Drawing.Color.FromArgb(54, 61, 78);
            _resetButton.ForeColor = System.Drawing.Color.White;
            _resetButton.FlatStyle = FlatStyle.Flat;
            _resetButton.FlatAppearance.BorderSize = 0;
            _resetButton.Width = 110;
            _resetButton.Height = 36;
            _resetButton.Location = new Point(350, 14);
            _resetButton.Click += (_, _) => ResetView();

            _mysteryLabel.AutoSize = true;
            _mysteryLabel.ForeColor = System.Drawing.Color.FromArgb(185, 174, 255);
            _mysteryLabel.Location = new Point(200, 56);
            _mysteryLabel.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            controlsBox.Controls.Add(speedCaption);
            controlsBox.Controls.Add(_speedSlider);
            controlsBox.Controls.Add(_startButton);
            controlsBox.Controls.Add(_resetButton);
            controlsBox.Controls.Add(_mysteryLabel);

            headerGrid.Controls.Add(titleBox, 0, 0);
            headerGrid.Controls.Add(statusBox, 1, 0);
            headerGrid.Controls.Add(controlsBox, 2, 0);

            return headerGrid;
        }

        private Control BuildBody()
        {
            var split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Vertical;
            split.BackColor = BackColor;
            split.Width = 1200;
            split.Panel1MinSize = 300;
            split.Panel2MinSize = 280;

            try
            {
                int desired = (int)(split.Width * 0.62f);
                int max = Math.Max(split.Panel1MinSize, split.Width - split.Panel2MinSize);
                split.SplitterDistance = Math.Clamp(desired, split.Panel1MinSize, max);
            }
            catch { }

            split.Panel1.Padding = new Padding(0, 0, 10, 0);
            split.Panel2.Padding = new Padding(10, 0, 0, 0);

            _boardCanvas.Dock = DockStyle.Fill;
            split.Panel1.Controls.Add(_boardCanvas);

            split.Panel2.Controls.Add(BuildSidePanel());
            return split;
        }

        private Control BuildSidePanel()
        {
            var side = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = BackColor
            };
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 285f));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

            side.Controls.Add(BuildPlayerGrid(), 0, 0);
            side.Controls.Add(BuildLogBox(), 0, 1);
            side.Controls.Add(BuildMysteryStrip(), 0, 2);
            return side;
        }

        private Control BuildPlayerGrid()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = BackColor
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            grid.Controls.Add(CreatePlayerCard(GameColor.Red), 0, 0);
            grid.Controls.Add(CreatePlayerCard(GameColor.Green), 1, 0);
            grid.Controls.Add(CreatePlayerCard(GameColor.Yellow), 0, 1);
            grid.Controls.Add(CreatePlayerCard(GameColor.Blue), 1, 1);
            return grid;
        }

        private Control CreatePlayerCard(GameColor color)
        {
            var accent = GetUiColor(color);
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6),
                Padding = new Padding(12),
                BackColor = System.Drawing.Color.FromArgb(24, 28, 38)
            };

            var title = new Label
            {
                AutoSize = true,
                Text = color.ToString(),
                ForeColor = accent,
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                Location = new Point(14, 10)
            };

            var accentBar = new Panel
            {
                BackColor = accent,
                Width = 4,
                Dock = DockStyle.Left
            };

            var summary = new Label
            {
                AutoSize = false,
                ForeColor = System.Drawing.Color.FromArgb(220, 228, 242),
                Location = new Point(14, 42),
                Size = new Size(170, 56),
                Text = "Board: 0\nBase: 4\nHome: 0"
            };

            var placement = new Label
            {
                AutoSize = false,
                ForeColor = System.Drawing.Color.FromArgb(185, 195, 210),
                Location = new Point(14, 100),
                Size = new Size(170, 26),
                Text = "Placement: pending"
            };

            var pieces = new Label
            {
                AutoSize = false,
                ForeColor = System.Drawing.Color.FromArgb(165, 175, 190),
                Location = new Point(14, 126),
                Size = new Size(200, 96),
                Text = string.Empty
            };

            card.Controls.Add(title);
            card.Controls.Add(summary);
            card.Controls.Add(placement);
            card.Controls.Add(pieces);
            card.Controls.Add(accentBar);

            _playerSummary[color] = summary;
            _playerPlacements[color] = placement;
            _playerPieces[color] = pieces;
            _playerAccents[color] = accentBar;
            return card;
        }

        private Control BuildLogBox()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 10),
                BackColor = BackColor
            };

            var caption = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "Live log",
                ForeColor = System.Drawing.Color.WhiteSmoke,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
            };

            _logBox.Dock = DockStyle.Fill;
            _logBox.ReadOnly = true;
            _logBox.BorderStyle = BorderStyle.None;
            _logBox.BackColor = System.Drawing.Color.FromArgb(18, 21, 28);
            _logBox.ForeColor = System.Drawing.Color.FromArgb(228, 234, 244);
            _logBox.Font = new Font("Cascadia Mono", 9f);

            panel.Controls.Add(_logBox);
            panel.Controls.Add(caption);
            return panel;
        }

        private Control BuildMysteryStrip()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(24, 28, 38),
                Padding = new Padding(12)
            };

            var label = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = System.Drawing.Color.FromArgb(207, 196, 255),
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Mystery cell: waiting for the first spawn"
            };

            panel.Controls.Add(label);
            return panel;
        }

        private async Task StartGameAsync()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                // If already connected, we just trigger PlayTurn
                await _hubConnection.InvokeAsync("PlayTurn");
                return;
            }

            if (_gameRunning)
                return;

            _gameRunning = true;
            _startButton.Enabled = false;
            _resetButton.Enabled = false;
            _logger.Reset();
            _logger.MarkRunning("Connecting to Server...");

            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5000/ludohub")
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<GameSnapshot>("UpdateState", OnSnapshotChanged);
                
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("StartGame");
                
                // Change button purpose to Play Next Turn
                _startButton.Text = "Play Next Turn";
            }
            catch (Exception ex)
            {
                _logger.MarkComplete($"Failed to connect to server: {ex.Message}");
                _gameRunning = false;
            }
            finally
            {
                if (!IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _startButton.Enabled = true;
                        _resetButton.Enabled = true;
                    }));
                }
            }
        }

        private void ResetView()
        {
            _logger.Reset();
        }

        private void OnSnapshotChanged(GameSnapshot snapshot)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => ApplySnapshot(snapshot)));
                return;
            }

            ApplySnapshot(snapshot);
        }

        private void ApplySnapshot(GameSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            _boardCanvas.Snapshot = snapshot;
            _boardCanvas.Invalidate();

            _statusLabel.Text = snapshot.StatusText;
            _sectionLabel.Text = snapshot.CurrentSection;
            _mysteryLabel.Text = snapshot.MysteryCellPosition.HasValue
                ? $"Mystery cell: {snapshot.MysteryCellPosition.Value} ({snapshot.MysteryCellRoundsRemaining} rounds left)"
                : "Mystery cell: waiting for the first spawn";

            foreach (var (color, player) in snapshot.Players)
            {
                if (_playerSummary.TryGetValue(color, out var summary))
                    summary.Text = $"Board: {player.BoardCount}\nBase: {player.BaseCount}\nHome: {player.HomeCount}";

                if (_playerPlacements.TryGetValue(color, out var placement))
                    placement.Text = player.FinishPlace.HasValue ? $"Placement: {player.FinishPlace.Value}" : "Placement: pending";

                if (_playerPieces.TryGetValue(color, out var pieces))
                {
                    pieces.Text = string.Join(Environment.NewLine,
                        player.Pieces.Select(piece => $"{piece.Name}: {piece.LocationText}"));
                }
            }

            if (snapshot.LogLines.Count > _lastLogLineCount)
            {
                for (int i = _lastLogLineCount; i < snapshot.LogLines.Count; i++)
                {
                    _logBox.AppendText(snapshot.LogLines[i] + Environment.NewLine);
                }

                _lastLogLineCount = snapshot.LogLines.Count;
                _logBox.SelectionStart = _logBox.TextLength;
                _logBox.ScrollToCaret();
            }
            else if (snapshot.LogLines.Count < _lastLogLineCount)
            {
                _logBox.Clear();
                foreach (var line in snapshot.LogLines)
                {
                    _logBox.AppendText(line + Environment.NewLine);
                }
                _lastLogLineCount = snapshot.LogLines.Count;
                _logBox.SelectionStart = _logBox.TextLength;
                _logBox.ScrollToCaret();
            }
            else if (snapshot.LogLines.Count > 0 && _lastLogLineCount == snapshot.LogLines.Count && snapshot.LogLines.Count % 500 == 0)
            {
                // Edge case where truncation happened perfectly. 
                _logBox.Text = string.Join(Environment.NewLine, snapshot.LogLines) + Environment.NewLine;
                _logBox.SelectionStart = _logBox.TextLength;
                _logBox.ScrollToCaret();
            }
        }

        private System.Drawing.Color GetUiColor(GameColor color) => color switch
        {
            GameColor.Red => System.Drawing.Color.FromArgb(255, 75, 92),
            GameColor.Green => System.Drawing.Color.FromArgb(52, 211, 153),
            GameColor.Yellow => System.Drawing.Color.FromArgb(251, 191, 36),
            GameColor.Blue => System.Drawing.Color.FromArgb(96, 165, 250),
            _ => System.Drawing.Color.WhiteSmoke
        };
    }
}