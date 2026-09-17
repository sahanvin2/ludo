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
        // Colors matching authentic Ludo
        private static readonly System.Drawing.Color RedColor    = System.Drawing.Color.FromArgb(229, 37, 33);
        private static readonly System.Drawing.Color GreenColor  = System.Drawing.Color.FromArgb(0, 184, 43);
        private static readonly System.Drawing.Color YellowColor = System.Drawing.Color.FromArgb(245, 166, 35);
        private static readonly System.Drawing.Color BlueColor   = System.Drawing.Color.FromArgb(0, 128, 255);

        private readonly Dictionary<GameColor, System.Drawing.Color> _colors = new()
        {
            [GameColor.Red]    = RedColor,
            [GameColor.Green]  = GreenColor,
            [GameColor.Yellow] = YellowColor,
            [GameColor.Blue]   = BlueColor
        };

        // 52 continuous track coordinates around the authentic 15x15 Ludo board in pure CLOCKWISE order
        // Yellow Approach = 0, Yellow Start = 1
        // Blue Approach = 13, Blue Start = 14
        // Red Approach = 26, Red Start = 27
        // Green Approach = 39, Green Start = 40
        private static readonly (int r, int c)[] PathCoords = new (int, int)[52]
        {
            // [0] Yellow Approach (end of East arm, before home)
            (7, 14),
            // [1..12] East arm bottom -> South arm right -> South end
            (8, 14), (8, 13), (8, 12), (8, 11), (8, 10), (8, 9),
            (9, 8),  (10, 8), (11, 8), (12, 8), (13, 8), (14, 8),
            // [13] Blue Approach (end of South arm, before home)
            (14, 7),
            // [14..25] South end -> South arm left -> West arm bottom -> West end
            (14, 6), (13, 6), (12, 6), (11, 6), (10, 6), (9, 6),
            (8, 5),  (8, 4),  (8, 3),  (8, 2),  (8, 1),  (8, 0),
            // [26] Red Approach (end of West arm, before home)
            (7, 0),
            // [27..38] West end -> West arm top -> North arm left -> North end
            (6, 0),  (6, 1),  (6, 2),  (6, 3),  (6, 4),  (6, 5),
            (5, 6),  (4, 6),  (3, 6),  (2, 6),  (1, 6),  (0, 6),
            // [39] Green Approach (end of North arm, before home)
            (0, 7),
            // [40..51] North end -> North arm right -> East arm top
            (0, 8),  (1, 8),  (2, 8),  (3, 8),  (4, 8),  (5, 8),
            (6, 9),  (6, 10), (6, 11), (6, 12), (6, 13), (6, 14)
        };

        // Home straight cells (5 per color) leading straight into each player's center home triangle
        private static readonly Dictionary<GameColor, (int r, int c)[]> HomeStraights = new()
        {
            [GameColor.Red]    = new[] { (7, 1),  (7, 2),  (7, 3),  (7, 4),  (7, 5) },
            [GameColor.Green]  = new[] { (1, 7),  (2, 7),  (3, 7),  (4, 7),  (5, 7) },
            [GameColor.Yellow] = new[] { (7, 13), (7, 12), (7, 11), (7, 10), (7, 9) },
            [GameColor.Blue]   = new[] { (13, 7), (12, 7), (11, 7), (10, 7), (9, 7) }
        };

        // Star positions on the board matching classic Ludo (Start squares + Safe cells)
        private static readonly HashSet<(int r, int c)> StarCells = new()
        {
            (6, 1), (1, 8), (8, 13), (13, 6),
            (2, 6), (6, 12), (12, 8), (8, 2)
        };

        public BoardCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = System.Drawing.Color.FromArgb(14, 16, 22);
        }

        public GameSnapshot? Snapshot { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(BackColor);

            float boardSize = Math.Min(Width, Height) - 24;
            if (boardSize < 150) return;

            float cellSize = boardSize / 15f;
            float startX = (Width - boardSize) / 2f;
            float startY = (Height - boardSize) / 2f;

            // Draw outer board shadow & base card
            var boardRect = new RectangleF(startX - 4, startY - 4, boardSize + 8, boardSize + 8);
            using (var shadowBrush = new SolidBrush(System.Drawing.Color.FromArgb(60, 0, 0, 0)))
                g.FillRoundedRectangle(shadowBrush, new RectangleF(boardRect.X + 4, boardRect.Y + 6, boardRect.Width, boardRect.Height), 16);

            using (var boardBg = new SolidBrush(System.Drawing.Color.FromArgb(240, 242, 245)))
                g.FillRoundedRectangle(boardBg, boardRect, 14);

            // 1. Draw 4 Bases (6x6 each in authentic clockwise order: Red -> Green -> Yellow -> Blue)
            DrawBase(g, startX, startY, cellSize, 0, 0, RedColor, GameColor.Red);        // Top-Left: Red
            DrawBase(g, startX, startY, cellSize, 0, 9, GreenColor, GameColor.Green);    // Top-Right: Green
            DrawBase(g, startX, startY, cellSize, 9, 9, YellowColor, GameColor.Yellow);  // Bottom-Right: Yellow
            DrawBase(g, startX, startY, cellSize, 9, 0, BlueColor, GameColor.Blue);      // Bottom-Left: Blue

            // 2. Draw Track Cells (White & Colored Home Straights)
            DrawTracks(g, startX, startY, cellSize);

            // 3. Draw Center Home Triangles
            DrawCenterHome(g, startX, startY, cellSize);

            // 4. Draw Mystery Cell if active
            DrawMysteryCell(g, startX, startY, cellSize);

            // 5. Draw Pieces
            DrawAllPieces(g, startX, startY, cellSize);

            // 6. Outer board border
            using var borderPen = new Pen(System.Drawing.Color.FromArgb(30, 35, 45), 2f);
            g.DrawRoundedRectangle(borderPen, boardRect, 14);
        }

        private void DrawBase(Graphics g, float startX, float startY, float cs, int rowStart, int colStart, System.Drawing.Color color, GameColor gameColor)
        {
            var baseRect = new RectangleF(startX + colStart * cs, startY + rowStart * cs, 6 * cs, 6 * cs);
            using var fill = new SolidBrush(color);
            g.FillRectangle(fill, baseRect);

            // Inner white rounded card
            float pad = cs * 0.75f;
            var innerRect = new RectangleF(baseRect.X + pad, baseRect.Y + pad, baseRect.Width - pad * 2, baseRect.Height - pad * 2);
            using var whiteBrush = new SolidBrush(System.Drawing.Color.White);
            using var borderPen = new Pen(System.Drawing.Color.FromArgb(200, color), 2f);
            g.FillRoundedRectangle(whiteBrush, innerRect, 18);
            g.DrawRoundedRectangle(borderPen, innerRect, 18);

            // 4 Dice circles inside the white card
            var spots = GetBaseSpotCenters(startX, startY, cs, gameColor);
            float spotRadius = cs * 0.48f;
            foreach (var spot in spots)
            {
                var spotRect = new RectangleF(spot.X - spotRadius, spot.Y - spotRadius, spotRadius * 2, spotRadius * 2);
                using var spotBrush = new SolidBrush(color);
                using var spotShadow = new SolidBrush(System.Drawing.Color.FromArgb(40, 0, 0, 0));
                g.FillEllipse(spotShadow, new RectangleF(spotRect.X + 1.5f, spotRect.Y + 2f, spotRect.Width, spotRect.Height));
                g.FillEllipse(spotBrush, spotRect);
                using var innerSpotPen = new Pen(System.Drawing.Color.FromArgb(240, 255, 255, 255), 1.5f);
                g.DrawEllipse(innerSpotPen, spotRect);
            }
        }

        private static PointF[] GetBaseSpotCenters(float startX, float startY, float cs, GameColor color)
        {
            float r0 = color is GameColor.Red or GameColor.Green ? 0 : 9;
            float c0 = color is GameColor.Red or GameColor.Blue ? 0 : 9;

            float x1 = startX + (c0 + 1.7f) * cs;
            float x2 = startX + (c0 + 4.3f) * cs;
            float y1 = startY + (r0 + 1.7f) * cs;
            float y2 = startY + (r0 + 4.3f) * cs;

            return new[]
            {
                new PointF(x1, y1),
                new PointF(x2, y1),
                new PointF(x1, y2),
                new PointF(x2, y2)
            };
        }

        private void DrawTracks(Graphics g, float startX, float startY, float cs)
        {
            using var cellPen = new Pen(System.Drawing.Color.FromArgb(190, 195, 205), 1.2f);
            using var whiteFill = new SolidBrush(System.Drawing.Color.White);

            // Draw all 52 path cells
            for (int i = 0; i < PathCoords.Length; i++)
            {
                var (r, c) = PathCoords[i];
                var rect = new RectangleF(startX + c * cs, startY + r * cs, cs, cs);

                // Starting square coloring for each player's X
                System.Drawing.Color? startColor = null;
                if (i == GameConstants.XPositions[GameColor.Red]) startColor = RedColor;
                else if (i == GameConstants.XPositions[GameColor.Green]) startColor = GreenColor;
                else if (i == GameConstants.XPositions[GameColor.Yellow]) startColor = YellowColor;
                else if (i == GameConstants.XPositions[GameColor.Blue]) startColor = BlueColor;

                if (startColor.HasValue)
                {
                    using var startFill = new SolidBrush(startColor.Value);
                    g.FillRectangle(startFill, rect);
                }
                else
                {
                    g.FillRectangle(whiteFill, rect);
                }

                g.DrawRectangle(cellPen, rect.X, rect.Y, rect.Width, rect.Height);

                // Draw star on designated safe/start cells
                if (StarCells.Contains((r, c)) || startColor.HasValue)
                {
                    var starColor = startColor.HasValue ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(140, 145, 160);
                    DrawStar(g, rect, starColor);
                }
            }

            // Draw Home Straights (5 colored cells each)
            foreach (var (color, coords) in HomeStraights)
            {
                using var fill = new SolidBrush(_colors[color]);
                foreach (var (r, c) in coords)
                {
                    var rect = new RectangleF(startX + c * cs, startY + r * cs, cs, cs);
                    g.FillRectangle(fill, rect);
                    g.DrawRectangle(cellPen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        private static void DrawCenterHome(Graphics g, float startX, float startY, float cs)
        {
            float x6 = startX + 6 * cs;
            float y6 = startY + 6 * cs;
            float x9 = startX + 9 * cs;
            float y9 = startY + 9 * cs;
            float cx = startX + 7.5f * cs;
            float cy = startY + 7.5f * cs;

            var pTopLeft     = new PointF(x6, y6);
            var pTopRight    = new PointF(x9, y6);
            var pBottomRight = new PointF(x9, y9);
            var pBottomLeft  = new PointF(x6, y9);
            var pCenter      = new PointF(cx, cy);

            using var redFill    = new SolidBrush(RedColor);
            using var greenFill  = new SolidBrush(GreenColor);
            using var yellowFill = new SolidBrush(YellowColor);
            using var blueFill   = new SolidBrush(BlueColor);
            using var borderPen  = new Pen(System.Drawing.Color.FromArgb(40, 45, 55), 2f);

            // Left triangle = Red (West)
            g.FillPolygon(redFill, new[] { pTopLeft, pCenter, pBottomLeft });
            // Top triangle = Green (North)
            g.FillPolygon(greenFill, new[] { pTopLeft, pTopRight, pCenter });
            // Right triangle = Yellow (East)
            g.FillPolygon(yellowFill, new[] { pTopRight, pBottomRight, pCenter });
            // Bottom triangle = Blue (South)
            g.FillPolygon(blueFill, new[] { pBottomLeft, pCenter, pBottomRight });

            g.DrawPolygon(borderPen, new[] { pTopLeft, pTopRight, pBottomRight, pBottomLeft });
            g.DrawLine(borderPen, pTopLeft, pCenter);
            g.DrawLine(borderPen, pTopRight, pCenter);
            g.DrawLine(borderPen, pBottomRight, pCenter);
            g.DrawLine(borderPen, pBottomLeft, pCenter);
        }

        private static void DrawStar(Graphics g, RectangleF rect, System.Drawing.Color color)
        {
            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;
            float rOuter = rect.Width * 0.36f;
            float rInner = rOuter * 0.44f;
            var points = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double angle = -Math.PI / 2 + (i * Math.PI / 5);
                float r = i % 2 == 0 ? rOuter : rInner;
                points[i] = new PointF(cx + (float)(r * Math.Cos(angle)), cy + (float)(r * Math.Sin(angle)));
            }

            using var pen = new Pen(color, 2f);
            using var fill = new SolidBrush(System.Drawing.Color.FromArgb(40, color));
            g.FillPolygon(fill, points);
            g.DrawPolygon(pen, points);
        }

        private void DrawMysteryCell(Graphics g, float startX, float startY, float cs)
        {
            var snapshot = Snapshot;
            if (snapshot?.MysteryCellPosition is not int pos || pos < 0 || pos >= PathCoords.Length)
                return;

            var (r, c) = PathCoords[pos];
            var rect = new RectangleF(startX + c * cs + 1, startY + r * cs + 1, cs - 2, cs - 2);

            using var glowBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 147, 51, 234));
            using var glowPen = new Pen(System.Drawing.Color.FromArgb(255, 234, 179), 2.5f);
            g.FillRoundedRectangle(glowBrush, rect, 6);
            g.DrawRoundedRectangle(glowPen, rect, 6);

            using var font = new Font("Segoe UI Black", cs * 0.48f, FontStyle.Bold);
            using var brush = new SolidBrush(System.Drawing.Color.White);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("?", font, brush, rect, format);
        }

        private void DrawAllPieces(Graphics g, float startX, float startY, float cs)
        {
            var snapshot = Snapshot;
            if (snapshot == null) return;

            // 1. Draw Base Pieces
            foreach (var (color, player) in snapshot.Players)
            {
                var spots = GetBaseSpotCenters(startX, startY, cs, color);
                var basePieces = player.Pieces.Where(p => p.Zone == PieceZone.Base).ToList();
                for (int i = 0; i < basePieces.Count && i < spots.Length; i++)
                {
                    DrawToken(g, spots[i], cs * 0.38f, basePieces[i]);
                }
            }

            // 2. Draw Ring Pieces (grouped by cell position)
            var ringGroups = snapshot.Players.Values
                .SelectMany(p => p.Pieces)
                .Where(p => p.Zone == PieceZone.Ring && p.RingPosition.HasValue && p.RingPosition.Value >= 0 && p.RingPosition.Value < PathCoords.Length)
                .GroupBy(p => p.RingPosition!.Value);

            foreach (var group in ringGroups)
            {
                var (r, c) = PathCoords[group.Key];
                float cellCenterX = startX + (c + 0.5f) * cs;
                float cellCenterY = startY + (r + 0.5f) * cs;
                var pieces = group.ToList();
                var offsets = GetClusterOffsets(pieces.Count, cs * 0.22f);

                for (int i = 0; i < pieces.Count; i++)
                {
                    var pt = new PointF(cellCenterX + offsets[i].X, cellCenterY + offsets[i].Y);
                    DrawToken(g, pt, cs * (pieces.Count > 1 ? 0.32f : 0.40f), pieces[i]);
                }
            }

            // 3. Draw Home Straight Pieces
            var homeStraightPieces = snapshot.Players.Values
                .SelectMany(p => p.Pieces)
                .Where(p => p.Zone == PieceZone.HomeStraight);

            foreach (var piece in homeStraightPieces)
            {
                if (HomeStraights.TryGetValue(piece.Color, out var coords))
                {
                    int step = Math.Clamp(piece.HomeStep ?? 0, 0, coords.Length - 1);
                    var (r, c) = coords[step];
                    var pt = new PointF(startX + (c + 0.5f) * cs, startY + (r + 0.5f) * cs);
                    DrawToken(g, pt, cs * 0.38f, piece);
                }
            }

            // 4. Draw Finished Home Pieces in Center Triangles
            var finishedPieces = snapshot.Players.Values
                .SelectMany(p => p.Pieces)
                .Where(p => p.Zone == PieceZone.Home)
                .GroupBy(p => p.Color);

            foreach (var group in finishedPieces)
            {
                var homeCenter = GetHomeCenter(startX, startY, cs, group.Key);
                var pieces = group.ToList();
                var offsets = GetClusterOffsets(pieces.Count, cs * 0.24f);
                for (int i = 0; i < pieces.Count; i++)
                {
                    var pt = new PointF(homeCenter.X + offsets[i].X, homeCenter.Y + offsets[i].Y);
                    DrawToken(g, pt, cs * 0.32f, pieces[i]);
                }
            }
        }

        private static PointF GetHomeCenter(float startX, float startY, float cs, GameColor color)
        {
            float cx = startX + 7.5f * cs;
            float cy = startY + 7.5f * cs;
            return color switch
            {
                GameColor.Red    => new PointF(cx - cs * 0.8f, cy),
                GameColor.Green  => new PointF(cx, cy - cs * 0.8f),
                GameColor.Yellow => new PointF(cx + cs * 0.8f, cy),
                GameColor.Blue   => new PointF(cx, cy + cs * 0.8f),
                _ => new PointF(cx, cy)
            };
        }

        private void DrawToken(Graphics g, PointF center, float radius, PieceViewModel piece)
        {
            var rect = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            var color = _colors[piece.Color];

            var highlight = System.Drawing.Color.FromArgb(Math.Min(255, color.R + 45), Math.Min(255, color.G + 45), Math.Min(255, color.B + 45));
            var shadow = System.Drawing.Color.FromArgb(Math.Max(0, color.R - 45), Math.Max(0, color.G - 45), Math.Max(0, color.B - 45));

            // Drop shadow
            using var shadowBrush = new SolidBrush(System.Drawing.Color.FromArgb(120, 0, 0, 0));
            g.FillEllipse(shadowBrush, new RectangleF(rect.X + 2f, rect.Y + 2.5f, rect.Width, rect.Height));

            // Token body
            using var tokenFill = new LinearGradientBrush(rect, highlight, shadow, LinearGradientMode.ForwardDiagonal);
            using var borderPen = new Pen(System.Drawing.Color.White, 1.8f);
            g.FillEllipse(tokenFill, rect);
            g.DrawEllipse(borderPen, rect);

            // Label (e.g. R1, G2, B3)
            float fontSize = Math.Max(6.5f, radius * 0.72f);
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
            using var textBrush = new SolidBrush(System.Drawing.Color.White);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(piece.Name, font, textBrush, rect, format);
        }

        private static PointF[] GetClusterOffsets(int count, float spread)
        {
            return count switch
            {
                1 => new[] { PointF.Empty },
                2 => new[] { new PointF(-spread, 0), new PointF(spread, 0) },
                3 => new[] { new PointF(-spread, -spread * 0.7f), new PointF(spread, -spread * 0.7f), new PointF(0, spread * 0.8f) },
                _ => new[] { new PointF(-spread, -spread), new PointF(spread, -spread), new PointF(-spread, spread), new PointF(spread, spread) }
            };
        }
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