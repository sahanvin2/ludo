using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using GameColor = LudoGame.Color;

namespace LudoGame.Gui
{
    internal sealed class BoardCanvas : Control
    {
        private readonly Dictionary<GameColor, System.Drawing.Color> _colors = new()
        {
            [GameColor.Red] = System.Drawing.Color.FromArgb(220, 65, 79),
            [GameColor.Green] = System.Drawing.Color.FromArgb(62, 168, 90),
            [GameColor.Yellow] = System.Drawing.Color.FromArgb(240, 188, 72),
            [GameColor.Blue] = System.Drawing.Color.FromArgb(74, 124, 246)
        };

        public BoardCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = System.Drawing.Color.FromArgb(20, 22, 28);
        }

        public GameSnapshot? Snapshot { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            using var backgroundBrush = new LinearGradientBrush(ClientRectangle,
                System.Drawing.Color.FromArgb(26, 31, 42),
                System.Drawing.Color.FromArgb(15, 17, 24),
                LinearGradientMode.ForwardDiagonal);
            g.FillRectangle(backgroundBrush, ClientRectangle);

            var boardArea = GetBoardArea();
            var center = new PointF(boardArea.Left + boardArea.Width / 2f, boardArea.Top + boardArea.Height / 2f);
            float rx = boardArea.Width / 2f - 22;
            float ry = boardArea.Height / 2f - 22;
            const int cellSize = 30;

            DrawCenterHub(g, center);

            var cellCenters = new PointF[GameConstants.BoardSize];
            for (int position = 0; position < GameConstants.BoardSize; position++)
            {
                double angle = -Math.PI / 2 + (2 * Math.PI * position / GameConstants.BoardSize);
                cellCenters[position] = new PointF(
                    center.X + (float)(rx * Math.Cos(angle)),
                    center.Y + (float)(ry * Math.Sin(angle)));
            }

            DrawRing(g, cellCenters, cellSize);
            DrawSpecialCells(g, cellCenters, cellSize);
            DrawMysteryCell(g, cellCenters, cellSize);
            DrawPieces(g, cellCenters, cellSize);
            DrawTitle(g);
        }

        private void DrawTitle(Graphics g)
        {
            using var brush = new SolidBrush(System.Drawing.Color.FromArgb(180, 205, 220));
            using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            var text = Snapshot?.CurrentSection;
            if (string.IsNullOrWhiteSpace(text))
                text = "Live board view";

            g.DrawString(text, font, brush, 18f, 12f);
        }

        private RectangleF GetBoardArea()
        {
            int padding = 28;
            return new RectangleF(padding, padding, Math.Max(10, Width - padding * 2), Math.Max(10, Height - padding * 2));
        }

        private static void DrawCenterHub(Graphics g, PointF center)
        {
            var hub = new RectangleF(center.X - 75, center.Y - 75, 150, 150);
            using var fill = new LinearGradientBrush(hub, System.Drawing.Color.FromArgb(42, 48, 64), System.Drawing.Color.FromArgb(22, 24, 32), LinearGradientMode.ForwardDiagonal);
            using var pen = new Pen(System.Drawing.Color.FromArgb(88, 96, 120), 2f);
            g.FillEllipse(fill, hub);
            g.DrawEllipse(pen, hub);

            using var titleBrush = new SolidBrush(System.Drawing.Color.WhiteSmoke);
            using var subtitleBrush = new SolidBrush(System.Drawing.Color.FromArgb(170, 180, 200));
            using var titleFont = new Font("Segoe UI Semibold", 16f, FontStyle.Bold);
            using var subtitleFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            var titleFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("LUDO-T", titleFont, titleBrush, hub, titleFormat);
            var subtitleRect = new RectangleF(hub.Left + 15, hub.Top + 88, hub.Width - 30, 30);
            g.DrawString("Gui board", subtitleFont, subtitleBrush, subtitleRect, titleFormat);
        }

        private void DrawRing(Graphics g, PointF[] centers, int cellSize)
        {
            for (int position = 0; position < centers.Length; position++)
            {
                bool special = IsSpecialPosition(position);
                bool approach = IsApproachPosition(position);
                var rect = new RectangleF(centers[position].X - cellSize / 2f, centers[position].Y - cellSize / 2f, cellSize, cellSize);

                using var fill = new SolidBrush(special
                    ? System.Drawing.Color.FromArgb(52, 58, 80)
                    : approach
                        ? System.Drawing.Color.FromArgb(42, 50, 64)
                        : System.Drawing.Color.FromArgb(34, 38, 48));
                using var pen = new Pen(special ? System.Drawing.Color.FromArgb(150, 160, 200) : System.Drawing.Color.FromArgb(70, 78, 95), special ? 2.5f : 1.4f);

                g.FillEllipse(fill, rect);
                g.DrawEllipse(pen, rect);

                if (special)
                {
                    using var badgeBrush = new SolidBrush(System.Drawing.Color.FromArgb(220, 230, 245));
                    using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                    string label = position switch
                    {
                        9 => "A",
                        27 => "B",
                        46 => "G",
                        _ => position.ToString()
                    };
                    var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(label, font, badgeBrush, rect, format);
                }
            }
        }

        private void DrawSpecialCells(Graphics g, PointF[] centers, int cellSize)
        {
            foreach (var (color, position) in GameConstants.XPositions)
                DrawAccentCell(g, centers[position], cellSize, _colors[color], "X");

            foreach (var (color, position) in GameConstants.ApproachCells)
                DrawAccentCell(g, centers[position], cellSize, _colors[color], "A");
        }

        private static void DrawAccentCell(Graphics g, PointF center, int cellSize, System.Drawing.Color accent, string text)
        {
            var rect = new RectangleF(center.X - cellSize / 2f, center.Y - cellSize / 2f, cellSize, cellSize);
            using var pen = new Pen(accent, 2f);
            using var fill = new SolidBrush(System.Drawing.Color.FromArgb(60, accent.R, accent.G, accent.B));
            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var brush = new SolidBrush(System.Drawing.Color.WhiteSmoke);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.FillEllipse(fill, rect);
            g.DrawEllipse(pen, rect);
            g.DrawString(text, font, brush, rect, format);
        }

        private void DrawMysteryCell(Graphics g, PointF[] centers, int cellSize)
        {
            var snapshot = Snapshot;
            if (snapshot?.MysteryCellPosition is not int pos)
                return;

            var center = centers[pos];
            var rect = new RectangleF(center.X - cellSize / 2f - 2, center.Y - cellSize / 2f - 2, cellSize + 4, cellSize + 4);
            using var fill = new SolidBrush(System.Drawing.Color.FromArgb(80, 178, 95, 214));
            using var pen = new Pen(System.Drawing.Color.FromArgb(190, 118, 225), 2.4f);
            using var font = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var brush = new SolidBrush(System.Drawing.Color.WhiteSmoke);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.FillEllipse(fill, rect);
            g.DrawEllipse(pen, rect);
            g.DrawString("?", font, brush, rect, format);
        }

        private void DrawPieces(Graphics g, PointF[] centers, int cellSize)
        {
            var snapshot = Snapshot;
            if (snapshot == null)
                return;

            var ringPieces = snapshot.Players.Values
                .SelectMany(player => player.Pieces)
                .Where(piece => piece.Zone == PieceZone.Ring && piece.RingPosition.HasValue)
                .GroupBy(piece => piece.RingPosition!.Value)
                .ToList();

            foreach (var group in ringPieces)
            {
                var baseCenter = centers[group.Key];
                var offsets = GetOffsets(group.Count()).ToArray();
                int index = 0;
                foreach (var piece in group)
                {
                    var offset = offsets[index++];
                    DrawPieceMarker(g, new PointF(baseCenter.X + offset.X, baseCenter.Y + offset.Y), piece);
                }
            }

            var homePieces = snapshot.Players.Values
                .SelectMany(player => player.Pieces)
                .Where(piece => piece.Zone is PieceZone.HomeStraight or PieceZone.Home)
                .ToList();

            if (homePieces.Count > 0)
                DrawHomeStatus(g, homePieces);
        }

        private void DrawPieceMarker(Graphics g, PointF center, PieceViewModel piece)
        {
            const float markerSize = 20f;
            var rect = new RectangleF(center.X - markerSize / 2f, center.Y - markerSize / 2f, markerSize, markerSize);
            var color = _colors[piece.Color];
            using var fill = new SolidBrush(color);
            using var border = new Pen(System.Drawing.Color.WhiteSmoke, 1.2f);
            using var font = new Font("Segoe UI", 7.4f, FontStyle.Bold);
            using var textBrush = new SolidBrush(System.Drawing.Color.White);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            // Drop shadow
            var shadowRect = new RectangleF(rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
            using var shadowBrush = new SolidBrush(System.Drawing.Color.FromArgb(90, 0, 0, 0));
            g.FillEllipse(shadowBrush, shadowRect);

            // Piece body
            g.FillEllipse(fill, rect);
            g.DrawEllipse(border, rect);
            g.DrawString(piece.Name, font, textBrush, rect, format);
        }

        private void DrawHomeStatus(Graphics g, IReadOnlyCollection<PieceViewModel> pieces)
        {
            var rect = new RectangleF(16, Height - 110, 220, 90);
            using var fill = new SolidBrush(System.Drawing.Color.FromArgb(145, 28, 31, 40));
            using var pen = new Pen(System.Drawing.Color.FromArgb(90, 110, 140), 1.2f);
            using var titleFont = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            using var bodyFont = new Font("Segoe UI", 8f, FontStyle.Regular);
            using var brush = new SolidBrush(System.Drawing.Color.WhiteSmoke);
            using var mutedBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 215, 230));

            g.FillRoundedRectangle(fill, rect, 14);
            g.DrawRoundedRectangle(pen, rect, 14);
            g.DrawString($"Home pieces: {pieces.Count}", titleFont, brush, rect.Left + 12, rect.Top + 10);
            var detail = string.Join("   ", pieces.Take(6).Select(piece => piece.Name));
            g.DrawString(detail, bodyFont, mutedBrush, new RectangleF(rect.Left + 12, rect.Top + 36, rect.Width - 24, rect.Height - 42));
        }

        private static IEnumerable<PointF> GetOffsets(int count)
        {
            return count switch
            {
                1 => new[] { new PointF(0, 0) },
                2 => new[] { new PointF(-8, 0), new PointF(8, 0) },
                3 => new[] { new PointF(-8, -6), new PointF(8, -6), new PointF(0, 8) },
                _ => new[] { new PointF(-8, -6), new PointF(8, -6), new PointF(-8, 8), new PointF(8, 8) }
            };
        }

        private static bool IsSpecialPosition(int position) => position is 9 or 27 or 46;

        private static bool IsApproachPosition(int position) =>
            GameConstants.ApproachCells.Values.Contains(position);
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF rect, float radius)
        {
            using var path = CreateRoundedRectangle(rect, radius);
            graphics.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF rect, float radius)
        {
            using var path = CreateRoundedRectangle(rect, radius);
            graphics.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectangle(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;
            var arc = new RectangleF(rect.Location, new SizeF(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}