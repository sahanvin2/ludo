using System;
using System.Collections.Generic;
using System.Linq;
using GameColor = LudoGame.Color;

namespace LudoGame.Gui
{
    internal enum PieceZone
    {
        Base,
        Ring,
        HomeStraight,
        Home
    }

    internal sealed class PieceViewModel
    {
        public PieceViewModel(string name, GameColor color)
        {
            Name = name;
            Color = color;
        }

        public string Name { get; }
        public GameColor Color { get; }
        public PieceZone Zone { get; set; } = PieceZone.Base;
        public int? RingPosition { get; set; }
        public int? HomeStep { get; set; }
        public string LocationText { get; set; } = "Base";

        public PieceViewModel Clone()
        {
            return new PieceViewModel(Name, Color)
            {
                Zone = Zone,
                RingPosition = RingPosition,
                HomeStep = HomeStep,
                LocationText = LocationText
            };
        }

        public void MoveToBase()
        {
            Zone = PieceZone.Base;
            RingPosition = null;
            HomeStep = null;
            LocationText = "Base";
        }

        public void MoveToRing(int position)
        {
            Zone = PieceZone.Ring;
            RingPosition = position;
            HomeStep = null;
            LocationText = position.ToString();
        }

        public void MoveToHomeStraight(int step)
        {
            Zone = PieceZone.HomeStraight;
            RingPosition = null;
            HomeStep = step;
            LocationText = $"homepath[{step}]";
        }

        public void MoveToHome()
        {
            Zone = PieceZone.Home;
            RingPosition = null;
            HomeStep = null;
            LocationText = "Home";
        }
    }

    internal sealed class PlayerViewModel
    {
        private readonly List<PieceViewModel> _pieces = new();

        public PlayerViewModel(GameColor color)
        {
            Color = color;
        }

        public GameColor Color { get; }
        public int BoardCount { get; set; }
        public int BaseCount { get; set; }
        public int HomeCount { get; set; }
        public int? FinishPlace { get; set; }
        public IReadOnlyList<PieceViewModel> Pieces => _pieces;

        public PieceViewModel GetOrCreatePiece(string name)
        {
            var piece = _pieces.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (piece != null)
                return piece;

            piece = new PieceViewModel(name, Color);
            _pieces.Add(piece);
            return piece;
        }

        public PlayerViewModel Clone()
        {
            var clone = new PlayerViewModel(Color)
            {
                BoardCount = BoardCount,
                BaseCount = BaseCount,
                HomeCount = HomeCount,
                FinishPlace = FinishPlace
            };

            foreach (var piece in _pieces)
                clone._pieces.Add(piece.Clone());

            return clone;
        }

        public void RecalculateCounts()
        {
            BoardCount = _pieces.Count(p => p.Zone is PieceZone.Ring or PieceZone.HomeStraight);
            BaseCount = _pieces.Count(p => p.Zone == PieceZone.Base);
            HomeCount = _pieces.Count(p => p.Zone == PieceZone.Home);
        }
    }

    internal sealed class GameSnapshot
    {
        public Dictionary<GameColor, PlayerViewModel> Players { get; } = new();
        public List<string> LogLines { get; } = new();
        public int? MysteryCellPosition { get; set; }
        public int MysteryCellRoundsRemaining { get; set; }
        public string StatusText { get; set; } = "Ready to start.";
        public string CurrentSection { get; set; } = string.Empty;
        public GameColor? CurrentStatusColor { get; set; }
        public bool IsRunning { get; set; }
        public bool IsComplete { get; set; }

        public PlayerViewModel GetOrCreatePlayer(GameColor color)
        {
            if (!Players.TryGetValue(color, out var player))
            {
                player = new PlayerViewModel(color);
                Players[color] = player;
            }

            return player;
        }

        public GameSnapshot Clone()
        {
            var clone = new GameSnapshot
            {
                MysteryCellPosition = MysteryCellPosition,
                MysteryCellRoundsRemaining = MysteryCellRoundsRemaining,
                StatusText = StatusText,
                CurrentSection = CurrentSection,
                CurrentStatusColor = CurrentStatusColor,
                IsRunning = IsRunning,
                IsComplete = IsComplete
            };

            foreach (var kvp in Players)
                clone.Players[kvp.Key] = kvp.Value.Clone();

            clone.LogLines.AddRange(LogLines);
            return clone;
        }
    }
}