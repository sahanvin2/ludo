# LUDO-T Simulation (Take Home Assignment)

This project is a complete simulation of the **LUDO-T** game described in the assignment. It runs without any user input and prints the game progress to the console until all four players finish.

## Where the code is located

All core source files are at the project root:
- `Program.cs`
- `LudoGameController.cs`
- `GameManager.cs` is inside `LudoGameController.cs`.
- `LudoGame.cs` contains game domain types and all player AI classes.
- `GameRules.cs` contains pure rule helper methods.
- `MovementEngine.cs` contains move evaluation and application logic.
- `PlayerFactory.cs` creates the four player objects.
- `IGameContracts.cs` defines the main interfaces for board, player, logger, and random source.
- `IMoveResolver.cs` defines the move resolver interface and implementation.
- `IDiceService.cs` defines the dice service interface and implementation.
- `GameLogger.cs` defines all console message templates.
- `ConsoleGameLogger.cs` implements `IGameLogger` for runtime output.

Unit tests are in `LudoGame.Tests/`.

## GUI version

A Windows Forms GUI lives in `LudoGame.Gui/`. It reuses the existing game engine and shows the simulation on a live dashboard with a board view, player cards, and a scrolling event log.

To run it on Windows with the .NET 8 SDK installed:

```bash
dotnet run --project LudoGame.Gui/LudoGame.Gui.csproj
```

If you have the repo-local SDK checked out in the project folder, you can also run it with:

```bash
.\dotnet\dotnet.exe run --project .\LudoGame.Gui\LudoGame.Gui.csproj
```

If the GUI is already open, close it before rebuilding so the output files are not locked.

Quick run steps:

```bash
cd "d:\Test 2\Wenura\CleanCodin1-main\CleanCodin1-main"
.\Run-Game.bat
```

Build and test commands:

```bash
.\dotnet\dotnet.exe build .\LudoGame.Gui\LudoGame.Gui.csproj
.\dotnet\dotnet.exe test .\LudoGame.Tests\LudoGame.Tests.csproj --logger "console;verbosity=detailed"
```

## What each file does

### `Program.cs`
- Program entry point.
- Creates `LudoGameController` and starts the game.
- Simple because the project is a simulation, not an interactive app.

### `LudoGameController.cs`
- Contains `GameManager` and `LudoGameController`.
- `LudoGameController` is a thin facade.
- `GameManager` is the simulation engine that controls:
  - game initialization
  - first-player selection
  - round execution
  - turn sequencing
  - finishing order
  - round-end logging

Why this exists:
- separates the game orchestration from the rest of the logic.
- keeps the program entry simple and testable.

### `PlayerFactory.cs`
- Static factory that constructs the four player objects:
  - `RedPlayer`
  - `GreenPlayer`
  - `YellowPlayer`
  - `BluePlayer`
- Returns a `Dictionary<Color, IPlayer>`.

Why this exists:
- centralizes player creation in one place.
- makes the `GameManager` constructor cleaner.

### `IGameContracts.cs`
- Defines the game contracts and interfaces used by the simulation.
- Includes:
  - `IRandomSource`
  - `IBoard`
  - `IPlayer`
  - `IGameLogger`

Why this exists:
- decouples implementation details.
- enables dependency injection and easier unit testing.
- allows future replacement of the board or logger.

### `IDiceService.cs`
- Defines `IDiceService`.
- Implements `DiceService`.

Why this exists:
- abstracts dice rolling behind an interface.
- makes tests deterministic when needed.

### `IMoveResolver.cs`
- Defines `IMoveResolver`.
- Implements `MovementResolver` which delegates to `MovementEngine`.

Why this exists:
- keeps the game manager from depending directly on a static rule engine.
- improves testability and separation of concerns.

### `LudoGame.cs`
This file contains most of the game domain and the player AI classes.

#### Domain types stored here
- `Color` enum: Red, Yellow, Green, Blue.
- `PieceState` enum: `Base`, `StandardPath`, `HomeStraight`, `Home`.
- `Direction` enum: `Clockwise`, `CounterClockwise`.
- `SpecialLocation` enum: `Alpha`, `Beta`, `Gamma`, `Base`, `X`, `Approach`.

#### `GameConstants`
- Encodes the board size and special cell positions.
- Contains the standard ring size `0..51`, approach cell mapping, and X entry positions.

Why this exists:
- keeps all board constants in one location.
- makes the cell indexing explicit and easier to verify against the assignment.

#### `PieceEffect`
- Records timed status effects:
  - energised rounds
  - sick rounds
  - briefing rounds
  - consecutive threes counter

Why this exists:
- isolates LUDO-T mystery effects from the piece state.
- allows clean expiration of effects.

#### `Piece`
- Represents a single game piece.
- Stores:
  - colour and name
  - current state
  - position on the ring or in home straight
  - movement direction and original direction at X
  - capture history
  - active effects

Methods include:
- `ResetToBase()` to fully reset state after capture.
- state helpers like `IsAtBase()`, `CanMove()`, `NeedsCaptureForHome()`.

Why this exists:
- centralizes piece state and behavior.
- ensures captured pieces are completely reset, matching T-9.

#### `Board` / `IBoard`
- Implements the standard 52-cell ring and the mystery cell lifecycle.
- Provides:
  - position calculation for clockwise/counterclockwise moves
  - special cell lookups for `X`, `Approach`, `Alpha`, `Beta`, `Gamma`
  - mystery cell spawning rules and lifetime tracking
  - own-block detection at a ring position

Why this exists:
- keeps board path logic out of the player AI and game manager.
- supports the assignment requirement that mystery cells cannot appear on occupied squares and cannot repeat consecutively.

#### `Dice`
- Simple wrapper around `IRandomSource`.
- Returns values `1..6`.

Why this exists:
- separates dice semantics from raw random number generation.

#### `CoinToss`
- Decides movement direction when a piece enters the board from base.

Why this exists:
- implements T-1 cleanly and keeps direction logic isolated.

#### `MysteryCellSystem`
- Handles landing on mystery cells and teleport effects.
- Teleports pieces to one of:
  - `Alpha`, `Beta`, `Gamma`, `Base`, `X`, or `Approach`.
- Applies T-12/T-13/T-14 effects only when teleportation occurs.
- Handles Beta briefing behavior and three consecutive threes.

Why this exists:
- isolates mystery-cell rules from general movement.
- avoids mixing teleport/effect logic with path movement.

#### `Player` base class and AI classes
- `Player` stores shared piece lists and common helper methods.
- Provides `ChooseMove(...)` abstract method for each AI.
- Four concrete classes implement the exact assignment behaviors:
  - `RedPlayer`
  - `GreenPlayer`
  - `YellowPlayer`
  - `BluePlayer`

Why this exists:
- the Strategy pattern models player-specific behavior cleanly.
- it avoids duplicating shared logic.

### `GameRules.cs`
- Provides pure helper methods for:
  - path generation
  - approach crossing detection
  - home entry and capture requirements
  - block and opponent-block checks
  - partial movement before a block

Why this exists:
- keeps rule computations side-effect free.
- makes `MovementEngine` easier to read and verify.

### `MovementEngine.cs`
- The single “source of truth” for move evaluation and execution.
- `Evaluate(...)` returns a `MoveOutcome` describing whether a move is valid, blocked, or captures.
- `Apply(...)` performs the move with side effects and logging.
- Handles:
  - standard path movement
  - home straight movement
  - block movement using T-4 rules
  - capture and capture bonus (T-2)
  - partial movement before a block
  - home entry restrictions from T-7

Why this exists:
- one consistent place for rule enforcement.
- avoids duplicated movement logic in multiple classes.

### `GameLogger.cs`
- Formats every required console message.
- Implements the exact messages required by the assignment.
- Includes messages for:
  - initial player line-up
  - opening rolls and first player
  - dice rolls
  - starting-point moves
  - path moves and home moves
  - detailed move output showing both dice roll and actual moved units
  - blocked movement
  - partial block movement
  - captures
  - mystery cell spawn and landing
  - teleport destinations and effects
  - briefing / energized / sick rules
  - final standings

Why this exists:
- separates output text from game logic.
- allows the game flow to remain clean while preserving required reporting.
- makes progress tracing precise by showing both the roll and the effective move distance.

### `ConsoleGameLogger.cs`
- Implements `IGameLogger` using `GameLogger` static methods.
- Enables runtime logging through the shared interface.

Why this exists:
- keeps the logger implementation separate from its interface.
- allows future replacement with another logger if needed.

## Why these structures were chosen

- `GameManager` is responsible for flow, not rules. This improves readability and maintainability.
- `MovementEngine` is responsible for move correctness. This avoids scattered rule checks.
- `GameRules` is responsible for math and rule predicates. This makes the logic easier to test and reason about.
- `Player` subclasses encode behavior differences. This matches the assignment’s four AI behaviours.
- Interfaces (`IBoard`, `IPlayer`, `IGameLogger`, `IRandomSource`) provide loose coupling and easier testing.
- `Piece` holds all state for a piece, including effects and direction, so captures and mystery teleports reset correctly.
- `MysteryCellSystem` keeps the teleport effects separate from game progression.

## Design patterns and justifications

- Total patterns used: 9, including the `State` pattern.
- **Factory**: `PlayerFactory` centrally builds the players.
- **Strategy**: `Player` subclasses each implement `ChooseMove(...)` differently.
- **Dependency Injection / Inversion of Control**: `GameManager` receives its random source and logger through interfaces.
- **Facade**: `MovementEngine` provides a unified move API for `GameManager` and the player AIs.
- **Adapter**: `ConsoleGameLogger` adapts `GameLogger` to `IGameLogger`.
- **Template Method**: `Player` defines shared behaviour and allows subclasses to customise `ChooseMove`.
- **Observer**: `IGameObserver` and `PlacementTrackerObserver` record placements without changing game flow.
- **Proxy**: `MovementResolver` forwards calls to `MovementEngine` through `IMoveResolver`.
- **Null Object**: `NullSectionPresenter` and `NullGameObserver` provide safe defaults.
- **State**: `PieceState` governs piece lifecycle and move eligibility.
- The source contains inline comments in `GameManager`, `GameRules`, `PlayerFactory`, `ConsoleGameLogger`, `IGameObserver`, `GameSectionPresenter`, and `MovementEngine` describing the pattern and rule usage.

## Code comment locations

The following files contain explicit pattern or SOLID commentary tied to the implementation:
- `LudoGameController.cs` — Dependency Inversion and game orchestration responsibilities.
- `MovementEngine.cs` — Facade pattern and rule engine centralisation.
- `GameRules.cs` — assignment rule comments for T-4, T-6, T-7, and home-path logic.
- `PlayerFactory.cs` — Factory pattern comment.
- `ConsoleGameLogger.cs` — Adapter pattern comment.
- `IGameObserver.cs` — Observer and Null Object pattern comments.
- `GameSectionPresenter.cs` — Strategy pattern comment for presentation.
- `IGameContracts.cs` — Interface Segregation and Single Responsibility comments.
- `IMoveResolver.cs` — Proxy pattern comment.
- `LudoGame.Tests/TestSuiteSummaryTests.cs` — catalogue output for actual test run reporting.

## Assignment alignment

This README maps directly to the assignment requirements:
- game structure and board representation: `GameConstants`, `Board`, `Piece`, `PieceState`
- player behaviour: `RedPlayer`, `GreenPlayer`, `YellowPlayer`, `BluePlayer`
- console output: `GameLogger` and `ConsoleGameLogger` produce detailed progress logs that include dice rolls and actual moved units for every move, matching the assignment reporting requirements.
- rules T-1 through T-15: `MovementEngine`, `GameRules`, `MysteryCellSystem`
- output formatting: `GameLogger`
- simulation-only execution: `Program.cs` → `LudoGameController` → `GameManager`

## How to compare with your report

If your report includes the following sections, they should match this project:
1. Structures used: board, pieces, players, rules engine, logger.
2. Justification: why separation into game manager, rule helpers, and player AI.
3. Design principles: SOLID, interface-based architecture, separation of concerns.
4. Efficiency: the game uses deterministic rule checks and avoids repeated expensive calculations.

## How to run

From the project root:

```bash
dotnet build
dotnet run                  # Step through output: press ENTER between sections
dotnet run -- --no-pause    # Print full log without pauses (no waiting)
```

To run tests:

```bash
dotnet test LudoGame.Tests/LudoGame.Tests.csproj
```

Actual test run example:

```bash
dotnet test LudoGame.Tests/LudoGame.Tests.csproj --logger "console;verbosity=detailed"
```

Actual result from this project:

- `Test Run Successful.`
- `Total tests: 27`
- `Passed: 27`
- `Total time: 0.3049 Seconds`

The detailed output also includes a catalogue test that prints a table of test classes and counts.

## Documentation files

| File | Contents |
|------|----------|
| `DESIGN_PATTERNS.md` | All 8 design patterns, where they are used in code, and why |
| `CLASS_DIAGRAM.md` | Mermaid class diagram for the report |
| `TEST_CASES.md` | Full list of all 27 unit tests with descriptions |
| `run-tests.sh` | Runs tests and prints test-type summary in terminal |
| `REPORT.md` | Design report for submission (structures, SOLID, algorithm, challenges) |

## Notes

- The numbered ring squares are represented by integers `0..51`.
- Home path cells are displayed as `redhomepath[0]` through `redhomepath[4]` and similarly for other colours.
- Captured pieces are fully reset to base, including direction and effects, matching the assignment.
- `GameManager` enforces `MaxRollsPerTurn` and `MaxGameRounds` to prevent runaway simulations.
- `GameRules` provides `GetModifiedMovement` (energized/sick modifiers) and `CanEnterHomeStraight` / `CanEnterHomeStraightAfterCross` to implement T-7 home-entry constraints.










