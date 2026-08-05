using System;
using System.Collections.Generic;
using System.Linq;

namespace LudoGame
{
    /// <summary>
    /// Manages the overall LUDO-T simulation lifecycle.
    /// Responsible for initialization, turn sequencing, bonus roll handling, mystery cell updates, and finish ordering.
    /// Uses Dependency Inversion by receiving abstractions for randomisation, logging, section presentation, and observers.
    /// </summary>
    public class GameManager
    {
        private readonly IBoard _board;
        private readonly IDiceService _dice;
        private readonly IMoveResolver _moves;
        private readonly CoinToss _coinToss;
        private readonly MysteryCellSystem _mystery;
        private readonly IGameLogger _logger;
        private readonly IRandomSource _random;
        private readonly Dictionary<Color, IPlayer> _players;
        private readonly IGameSectionPresenter _sections;
        private readonly IReadOnlyList<IGameObserver> _observers;
        private List<Color> _playOrder;
        private int _round;
        private int _finishCount;

        /// <summary>Prevents infinite bonus-roll / six chains in one turn (simulation must terminate).</summary>
        private const int MaxRollsPerTurn = 50;
        /// <summary>Prevents runaway games if board state stalls.</summary>
        private const int MaxGameRounds = 5000;

        public GameManager(
            IRandomSource? random = null,
            IGameLogger? logger = null,
            IGameSectionPresenter? sectionPresenter = null,
            IEnumerable<IGameObserver>? observers = null)
        {
            var rng = random ?? new GameRandom();
            _random = rng;
            _logger = logger ?? new ConsoleGameLogger();
            _sections = sectionPresenter ?? new NullSectionPresenter();
            _observers = observers?.ToList() ?? new List<IGameObserver> { new PlacementTrackerObserver() };
            _board = new Board(rng);
            _dice = new DiceService(rng);
            _moves = new MovementResolver();
            _coinToss = new CoinToss(rng);
            _mystery = new MysteryCellSystem(_board, _logger, rng);
            _players = PlayerFactory.CreatePlayers();
            _playOrder = ColorHelper.ClockwisePlayOrder.ToList();
        }

        /// <summary>
        /// Initializes the simulation output and chooses the first player before starting the game loop.
        /// </summary>
        public void StartGame()
        {
            _sections.BeginSection("Game Setup — players, opening rolls, and turn order");
            foreach (var color in ColorHelper.ClockwisePlayOrder)
            {
                var p = _players[color];
                _logger.LogInitialPlayer(color, p.Pieces[0].Name, p.Pieces[1].Name, p.Pieces[2].Name, p.Pieces[3].Name);
            }

            DetermineFirstPlayer();
            _sections.WaitForContinue();
            RunGame();
        }

        private void DetermineFirstPlayer()
        {
            var rolls = new Dictionary<Color, int>();
            foreach (var color in ColorHelper.ClockwisePlayOrder)
            {
                int r = _dice.Roll();
                rolls[color] = r;
                _logger.LogOpeningRoll(color, r);
            }

            int highestRoll = rolls.Values.Max();
            var tied = ColorHelper.ClockwisePlayOrder.Where(c => rolls[c] == highestRoll).ToList();
            var first = tied[_random.Next(0, tied.Count)];
            _logger.LogFirstPlayer(first);

            int idx = _playOrder.IndexOf(first);
            _playOrder = _playOrder.Skip(idx).Concat(_playOrder.Take(idx)).ToList();
            _logger.LogRoundOrder(FormatPlayOrder(_playOrder));
        }

        private void RunGame()
        {
            while (_finishCount < 4)
            {
                if (_round >= MaxGameRounds)
                {
                    Console.WriteLine($"Simulation stopped after {MaxGameRounds} rounds (safety limit).");
                    break;
                }

                _round++;
                _sections.BeginSection($"Round {_round} — turns, mystery cell, and board status");

                foreach (var color in _playOrder)
                {
                    if (_players[color].HasWon()) continue;
                    ExecutePlayerTurn(_players[color]);
                }

                foreach (var p in _players.Values)
                    p.UpdateEffectsEndOfRound();

                if (_board.UpdateMysteryCell(_players, out int? spawned) && spawned.HasValue)
                    _logger.LogMysterySpawn(spawned.Value, 4);

                PrintRoundStatus();

                foreach (var color in _playOrder)
                    RegisterFinishIfNeeded(_players[color]);

                if (_finishCount >= 4)
                    break;

                _sections.WaitForContinue($"round {_round + 1}");
            }

            _sections.BeginSection("Final Results — placements and winner messages");
            LogFinalStandingsIfComplete();
            _sections.WaitForContinue("exit");
            Console.WriteLine("Simulation complete.");
        }

        private void RegisterFinishIfNeeded(IPlayer player)
        {
            if (!player.HasWon() || player.FinishPlace != 0)
                return;

            _finishCount++;
            player.FinishPlace = _finishCount;
            _logger.LogPlacement(player.Color, _finishCount);
            foreach (var observer in _observers)
                observer.OnPlayerPlaced(player, _finishCount);

            string reason = _finishCount switch
            {
                1 => "next rounds (1st place decided)",
                4 => "final results",
                _ => $"next rounds ({OrdinalPlace(_finishCount)} place decided)"
            };
            _sections.WaitForContinue(reason);
        }

        private static string OrdinalPlace(int place) => place switch
        {
            2 => "2nd",
            3 => "3rd",
            4 => "4th",
            _ => $"{place}th"
        };

        private void LogFinalStandingsIfComplete()
        {
            if (_finishCount < 4)
                return;

            var standings = _players.Values
                .Where(p => p.FinishPlace > 0)
                .Select(p => (p.Color, p.FinishPlace))
                .ToList();
            _logger.LogFinalStandings(standings);
        }

        /// <summary>
        /// Executes a single player's turn, including dice rolls, bonus rolls, briefing logic, and blockade resolution.
        /// The loop also protects against runaway-turns via a maximum roll count.
        /// </summary>
        private void ExecutePlayerTurn(IPlayer player)
        {
            int consecutiveSixes = 0;
            int rollsThisTurn = 0;

            while (true)
            {
                if (++rollsThisTurn > MaxRollsPerTurn)
                    return;

                int diceValue = _dice.Roll();
                _logger.LogDiceRoll(player.Color, diceValue);

                if (diceValue == 6)
                {
                    consecutiveSixes++;
                    if (consecutiveSixes >= 3)
                    {
                        if (HasBlockade(player))
                            BreakBlockade(player);
                        _logger.LogConsecutiveSixesIgnored(player.Color);
                        return;
                    }
                }
                else
                {
                    consecutiveSixes = 0;
                }

                foreach (var briefingPiece in player.Pieces.Where(p => p.Effect.BriefingRounds > 0).ToList())
                {
                    if (_mystery.HandleBriefingRoll(briefingPiece, player.Color, diceValue))
                        goto NextRoll;
                }

                var result = TryExecuteChosenMove(player, diceValue);
                if (result is MoveResult.Moved or MoveResult.CaptureBonus)
                    RegisterFinishIfNeeded(player);

                if (result == MoveResult.IgnoredTurn)
                    return;

                if (player.HasWon())
                    return;

                if (result == MoveResult.CaptureBonus)
                    goto NextRoll;

                if (diceValue == 6 && result == MoveResult.Moved)
                    goto NextRoll;

                return;

                NextRoll: ;
            }
        }

        /// <summary>
        /// Attempts to execute the player's chosen piece move.
        /// If the chosen move fails, the manager searches for an alternative move or applies no-move logic.
        /// </summary>
        private MoveResult TryExecuteChosenMove(IPlayer player, int diceValue)
        {
            var tried = new HashSet<Piece>();
            (Piece piece, bool fromBase)? choice = player.ChooseMove(diceValue, _board, _players);

            while (true)
            {
                if (choice == null)
                    return MoveResult.NoMove;

                var (piece, fromBase) = choice.Value;
                if (tried.Contains(piece))
                    return MoveResult.NoMove;
                tried.Add(piece);

                MoveResult result = fromBase
                    ? MoveFromBase(player, piece)
                    : _moves.Apply(player, piece, diceValue, _board, _players, _mystery, _logger);

                if (result != MoveResult.NoMove)
                    return result;

                if (TryFindAlternative(player, diceValue, piece, tried, out var alt))
                {
                    choice = alt;
                    continue;
                }

                if (!GameRules.HasAnyMovablePiece(player, diceValue, _board, _players))
                    return MoveResult.NoMove;

                var outcome = _moves.Evaluate(player, piece, diceValue, _board, _players);
                if (outcome.NeedsAlternative)
                {
                    _logger.LogNoAlternativeMove(player.Color);
                    return MoveResult.IgnoredTurn;
                }

                return MoveResult.NoMove;
            }
        }

        /// <summary>
        /// Attempts to find an alternative movable piece when the chosen piece cannot execute its move.
        /// </summary>
        private bool TryFindAlternative(IPlayer player, int diceValue, Piece blocked, HashSet<Piece> tried, out (Piece piece, bool fromBase)? choice)
        {
            foreach (var piece in player.Pieces)
            {
                if (tried.Contains(piece) || piece == blocked) continue;

                if (piece.IsAtBase() && diceValue == 6)
                {
                    if (player is RedPlayer && player.StandardPathCount() > 0)
                        continue;
                    choice = (piece, true);
                    return true;
                }

                if (!piece.IsAtBase() && !piece.IsAtHome() &&
                    MovementEngine.Simulate(player, piece, diceValue, _board, _players).CanMove)
                {
                    choice = (piece, false);
                    return true;
                }
            }

            choice = null;
            return false;
        }

        /// <summary>
        /// Moves a piece from the base onto the starting point when a 6 is rolled.
        /// Respects player-specific restrictions such as RedPlayer's requirement when pieces are already on the board.
        /// </summary>
        private MoveResult MoveFromBase(IPlayer player, Piece piece)
        {
            if (player.StandardPathCount() > 0 && player is RedPlayer)
                return MoveResult.NoMove;

            piece.State = PieceState.StandardPath;
            piece.Position = GameConstants.XPositions[player.Color];
            piece.MovementDirection = _coinToss.Toss();
            piece.DirectionAtX = piece.MovementDirection;
            piece.ApproachPassCount = 0;
            _logger.LogStartingPointMove(player.Color, piece.Name);
            LogPlayerPieceStatus(player);
            return MoveResult.Moved;
        }

        private void LogPlayerPieceStatus(IPlayer player) =>
            _logger.LogPieceStatus(
                player.Color,
                player.GetPiecesOnBoard().Count,
                player.GetPiecesAtBase().Count,
                player.GetPiecesAtHome().Count);

        private bool HasBlockade(IPlayer player) =>
            player.Pieces.Where(p => p.IsInStandardPath()).GroupBy(p => p.Position).Any(g => g.Count() >= 2);

        /// <summary>
        /// Breaks a player's blockade by moving additional pieces forward when three consecutive sixes force a forced unblock.
        /// This preserves piece directions and logs the resulting detailed movement.
        /// </summary>
        private void BreakBlockade(IPlayer player)
        {
            var groups = player.Pieces.Where(p => p.IsInStandardPath()).GroupBy(p => p.Position).Where(g => g.Count() >= 2);
            foreach (var grp in groups)
            {
                var list = grp.ToList();
                var keeper = list[0];
                keeper.MovementDirection = keeper.DirectionAtX;

                for (int i = 1; i < list.Count; i++)
                {
                    var p = list[i];
                    int old = p.Position;
                    var dir = p.DirectionAtX;
                    int? landing = GameRules.TryMoveCumulative(old, 6, dir, player.Color, _players);
                    if (!landing.HasValue) continue;

                    p.Position = landing.Value;
                    p.MovementDirection = dir;
                    int stepsMoved = 0;
                    int pos = old;
                    while (pos != landing.Value)
                    {
                        pos = GameRules.StepForward(pos, dir);
                        stepsMoved++;
                    }
                    _logger.LogMoveDetailed(player.Color, p.Name, old, landing.Value, 6, stepsMoved, dir);
                }
            }
        }

        private static string FormatPlayOrder(IReadOnlyList<Color> order)
        {
            var names = order.Select(c => c.ToLowerString()).ToList();
            return names.Count switch
            {
                0 => string.Empty,
                1 => names[0],
                _ => string.Join(", ", names.Take(names.Count - 1)) + ", and " + names[^1]
            };
        }

        private void PrintRoundStatus()
        {
            foreach (var color in ColorHelper.ClockwisePlayOrder)
            {
                var player = _players[color];
                LogPlayerPieceStatus(player);
                _logger.LogRoundStatusHeader(player.Color);
                foreach (var piece in player.Pieces)
                {
                    _logger.LogPieceLocation(piece.Name, GameRules.FormatPieceLocation(piece, player.Color));
                }
            }
            if (_board.MysteryCellPosition.HasValue)
                _logger.LogMysteryCellStatus(_board.MysteryCellPosition.Value, _board.MysteryCellRoundsRemaining);
        }
    }

    public class LudoGameController
    {
        private readonly GameManager _game;

        public LudoGameController(bool interactiveSections = true)
            : this(
                logger: null,
                sectionPresenter: interactiveSections ? new InteractiveSectionPresenter() : new NullSectionPresenter(),
                observers: new IGameObserver[] { new PlacementTrackerObserver() })
        {
        }

        public LudoGameController(
            IGameLogger? logger = null,
            IGameSectionPresenter? sectionPresenter = null,
            IEnumerable<IGameObserver>? observers = null)
        {
            _game = new GameManager(
                logger: logger,
                sectionPresenter: sectionPresenter,
                observers: observers);
        }

        public void StartGame() => _game.StartGame();
    }
}
