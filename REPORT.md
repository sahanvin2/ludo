# LUDO-T Simulation — Design Report

## 0. Class diagram

A full **class diagram** (Mermaid UML) is in **`CLASS_DIAGRAM.md`**. Paste it into your report PDF or export as an image from VS Code / [mermaid.live](https://mermaid.live).

**Core structure:** `Program` → `LudoGameController` → `GameManager` orchestrates `IBoard`, `IPlayer` (four AI strategies), `IMoveResolver` (proxy to `MovementEngine`), `IGameLogger`, `IGameSectionPresenter`, and `IGameObserver`.

---

## 1. Data structures

### Board
- **52-cell ring** represented as integers `0–51` (yellow approach = 0).
- **Constants** in `GameConstants`: approach cells, X positions, Alpha/Beta/Gamma, five home-straight cells (`homepath[0]`–`homepath[4]`); exact roll to finish when index + dice = 5.
- **`Board`** implements `IBoard`: mystery cell position/timer, spawn logic, `CalculateNewPosition` with modulo wrap.

### Pieces
- **`Piece`**: colour, name, `PieceState` (Base, StandardPath, HomeStraight, Home), ring `Position`, `MovementDirection`, `DirectionAtX` (T-5), `ApproachPassCount`, `Captures`, `HomeStraightPosition`, `PieceEffect` (energized/sick/briefing rounds).

### Players
- **`Player`** abstract base; **`RedPlayer`**, **`GreenPlayer`**, **`YellowPlayer`**, **`BluePlayer`** implement strategy via `ChooseMove`.
- **`Dictionary<Color, IPlayer>`** for lookup during movement and capture, enabled through `PlayerFactory.CreatePlayers()`.

### Blocks
- No separate block class: **two or more same-colour pieces on one cell** form a block; detected via `GetPiecesAtPosition` / `GetOwnBlock`.

## 2. Justification

| Choice | Why |
|--------|-----|
| Integer ring positions | Matches spec numbering; O(1) indexing; easy modulo movement |
| Enum `PieceState` | Clear lifecycle; avoids invalid combined states |
| Per-piece direction | Required for LUDO-T bidirectional rules and T-5 |
| Strategy subclasses | Each AI behaviour isolated; open for extension |
| `MovementEngine` | One rule source for simulation and execution — reduces drift |
| `IRandomSource` | Testable, seeded runs for debugging |

## 3. SOLID and patterns

- **Single responsibility**: `GameLogger` (output), `MovementEngine` (rules), `GameManager` (flow), `MysteryCellSystem` (mystery), `GameSectionPresenter` (output pacing).
- **Open/closed**: New player = new `Player` subclass without changing engine.
- **Liskov**: All players usable through `IPlayer` / `Player`.
- **Interface segregation**: `IBoard`, `IPlayer`, `IGameLogger`, `IRandomSource`, `IDiceService`, `IMoveResolver`, `IGameSectionPresenter`.
- **Dependency inversion**: `GameManager` accepts `IRandomSource`, `IGameLogger`, and `IGameSectionPresenter` through its constructor.

### Actual test execution

The test suite was executed with:

```bash
dotnet test LudoGame.Tests/LudoGame.Tests.csproj --logger "console;verbosity=detailed"
```

Result:

- `Test Run Successful.`
- `Total tests: 27`
- `Passed: 27`
- `Total time: 0.3049 Seconds`

This includes the catalogue test `TestSuiteSummaryTests.Test_suite_catalog_lists_all_categories_and_counts`, which prints the test class counts in the terminal.

**Eight design patterns (module GoF-style — see `DESIGN_PATTERNS.md`)**

| # | Pattern | Where in code |
|---|---------|----------------|
| 1 | **Strategy** | `RedPlayer`, `GreenPlayer`, `YellowPlayer`, `BluePlayer` → `ChooseMove` |
| 2 | **Factory** | `PlayerFactory.CreatePlayers()` |
| 3 | **Facade** | `MovementEngine`, `LudoGameController` |
| 4 | **State** | `Piece` + `PieceState` enum (see note below) |
| 5 | **Adapter** | `ConsoleGameLogger` → static `GameLogger` |
| 6 | **Template Method** | Abstract `Player`; subclasses override `ChooseMove` |
| 7 | **Observer** | `IGameObserver`, `PlacementTrackerObserver` in `IGameObserver.cs` |
| 8 | **Proxy** | `MovementResolver` delegates to `MovementEngine` |

**Also used (supporting):** Null Object (`NullSectionPresenter`, `NullGameObserver`), Dependency Injection (`GameManager` constructor).

### Design pattern evidence

The following evidence shows where each of the eight counted patterns appears in the code:

- **Strategy**: `RedPlayer`, `GreenPlayer`, `YellowPlayer`, `BluePlayer` in `LudoGame.cs`; all implement `ChooseMove` with color-specific decision logic.
- **Factory**: `PlayerFactory.CreatePlayers()` in `PlayerFactory.cs`; centralizes construction and returns `Dictionary<Color, IPlayer>`.
- **Facade**: `MovementEngine` in `MovementEngine.cs` and `LudoGameController` in `LudoGameController.cs`; both hide complexity behind a simpler API.
- **Adapter**: `ConsoleGameLogger` in `ConsoleGameLogger.cs`; adapts static `GameLogger` methods to `IGameLogger`.
- **Template Method**: `Player` in `LudoGame.cs`; defines shared behaviour and requires subclasses to implement `ChooseMove`.
- **Observer**: `IGameObserver` and `PlacementTrackerObserver` in `IGameObserver.cs`; game completion events are pushed to observers.
- **Proxy**: `MovementResolver` in `IMoveResolver.cs`; forwards evaluation and application calls to `MovementEngine`.
- **Null Object**: `NullSectionPresenter` in `GameSectionPresenter.cs` and `NullGameObserver` in `IGameObserver.cs`; provide no-op defaults for optional collaborators.

### State pattern — short explanation for the report

The **State** pattern lets an object behave differently depending on its current state. Here, each `Piece` has a `PieceState`: `Base`, `StandardPath`, `HomeStraight`, or `Home`. Methods such as `CanMove()`, `IsAtBase()`, and branches in `MovementEngine` depend on that state. This is the same idea as the GoF State pattern, implemented with an **enum** instead of separate state classes (simpler for this assignment). Example: a piece in `Home` cannot move; a piece in `Briefing` is handled via `PieceEffect`.

### Observer pattern — what was added

When a player finishes (1st–4th place), `GameManager` notifies all `IGameObserver` subscribers. `PlacementTrackerObserver` records placements without changing game rules. Tests use no observers by default.

### Proxy pattern — what it does

`MovementResolver` implements `IMoveResolver` but forwards every call to the static `MovementEngine`. Callers depend on the interface; the real work stays in one place — classic **Proxy** (control + delegation).

## 4. Efficiency

- **Time**: O(52) per move worst case (path steps); O(players × pieces) per turn for AI scans — trivial at 4×4 scale.
- **Space**: O(players × pieces + board constants) — fixed small footprint.
- **Bottleneck**: repeated `Simulate` in AI — acceptable for assignment; could cache moves per dice roll if needed.

## 5. Algorithm and Game Logic

The simulation’s core idea is to treat movement as a single “rules engine” and then let everything else (player strategies, turn progression, and logging) build on top of it.

### Turn flow (GameManager)

`GameManager.StartGame()` initializes the four players and the board and then runs a loop of rounds until all four players have finished. Each player’s turn is driven by dice rolls:

1. The turn begins with a dice roll (`IDiceService` / `DiceService` via `GameRandom`).
2. On a roll of `6`, the player gets bonus rolls, but the simulation caps the number of rolls per turn (`MaxRollsPerTurn`) to guarantee termination.
3. Before choosing a move, the manager checks “briefing” turns for pieces with `PieceEffect.BriefingRounds > 0`. If a piece is in briefing mode and the dice value matches the briefing rule, `MysteryCellSystem.HandleBriefingRoll` may force the piece back to base (escape).
4. The AI then selects a candidate move via `IPlayer.ChooseMove(diceValue, board, players)`.
5. If the chosen move can’t legally be executed (for example, the piece is blocked by an opponent block in a way that requires choosing another piece), `GameManager.TryExecuteChosenMove` searches for alternatives and may ignore the turn when no alternatives exist.

This structure keeps the AI logic lightweight: AI proposes, while `MovementEngine` and `GameRules` decide legality and effects.

### Movement logic (MovementEngine + GameRules)

Movement is handled by `MovementEngine`, which provides two key operations:

- `Evaluate(...)`: computes whether a move is legal and what high-level outcome it would produce (moved, capture bonus, needs alternative, and whether it creates a block).
- `Apply(...)`: performs the state mutation for the selected move, updating the piece and applying side-effects (home entry, captures, and mystery-cell teleport).

Both evaluation and execution share the same rule inputs through `GameRules` helpers, avoiding “AI drift” where the AI thinks something is legal but execution rejects it.

#### Standard path movement and approach crossing

For pieces on the standard ring, movement size is computed by `GameRules.GetModifiedMovement`, which applies LUDO-T effects:

- **Energized** doubles movement.
- **Sick** halves movement (with a minimum of 1 when positive).

The engine then enumerates the ring cells the piece would traverse using `GameRules.GetCellsAlongPath(start, steps, direction)` and finds the player’s approach cell (`GameConstants.ApproachCells[color]`).

When the path crosses (or lands on) the approach, the engine uses `GameRules.CanCompleteApproachEntry` to decide whether the piece may enter the home straight. The “two-pass” style condition for home entry is modeled using `Piece.ApproachPassCount` and the direction-specific rule in `GameRules.CanEnterHomeStraight` / `CanEnterHomeStraightAfterCross`. If home entry is not allowed, the move continues as a normal standard-path landing (the piece stays on the ring at the path’s final cell).

#### Home straight movement

When a piece is already on the home straight, `MovementEngine.ApplyHomeStraight` checks whether `HomeStraightPosition + diceValue` overshoots the finishing step. Landing exactly at the finishing step transitions the piece to `PieceState.Home` and records the final home movement in logs.

### Capturing logic

Capturing happens whenever a moving piece lands on a cell that contains a valid opponent piece.

For normal (non-block) movement:

- `GameRules.FindCapturableAt(target, attackerColor, players)` returns an opponent piece only when exactly one opponent piece is at `target` (if there are two or more opponents, the cell is considered unavailable for capture).
- `MovementEngine.ApplyStandardLanding` then:
  - moves the attacker to `target`,
  - resets the captured piece to base (`cap.ResetToBase()`),
  - increments `piece.Captures` (used by Yellow-player / home-entry constraints),
  - returns `MoveResult.CaptureBonus`, which grants the current player additional dice rolls in `GameManager`.

For block moves (when a player has two or more pieces on the same cell):

- `MovementEngine.Evaluate` treats block ownership as a different movement mode. It uses `board.GetOwnBlock` to detect the block and computes a shared “divided” movement (`diceValue / block.Count`).
- If the resulting landing cell contains an opponent block of matching size, `MovementEngine.ApplyBlockMove` captures the entire opponent block, resets those pieces to base, increments the attacker block pieces’ `Captures`, and returns `CaptureBonus`.

This gives a consistent notion of “block captures” while still preserving the normal single-piece capture rule.

### Blockades (own blocks and opponent blocks)

Blocks are represented implicitly: if two or more same-color pieces occupy the same ring index, they form a blockade.

There are two blockade-related behaviors:

1. **Movement onto/into an opponent blockade is prevented.**
   - In `MovementEngine.Evaluate`, landing on a cell that satisfies `GameRules.IsOpponentBlock(...)` triggers `NeedsAlternative`.
   - In `MovementEngine.ApplyStandardLanding`, when `target` is an opponent block, the engine attempts a partial advance via `GameRules.TryPartialMoveBeforeBlock`, which moves the piece up to (but not onto) the blocking cell.
2. **Moves made from within your own blockade are handled as a block move.**
   - When a piece that is part of a block is selected, `MovementEngine.Apply` delegates to `ApplyBlockMove`.
   - The direction of block movement uses `GameRules.GetBlockMovementDirection(block)`, which implements the “T-4: farthest-from-home direction when directions differ” rule if the block contains pieces moving in different directions.

If the block move would still land on an opponent block, `MovementEngine.ApplyBlockMove` distinguishes between:

- capturing (when opponent block size matches), and
- being unable to move (when opponent block size differs).

### Mystery cells (Board + MysteryCellSystem)

Mystery cells are modeled as a single special ring index plus a countdown timer:

- `Board.UpdateMysteryCell(players, out spawnedAt)` tracks when mystery-cell spawning begins and when to respawn.
  - The first spawn occurs only after at least two rounds have elapsed while there is at least one piece on the standard path.
  - After spawn, the cell remains for `MysteryCellRoundsRemaining = 4` rounds.
  - Respawns avoid reusing the immediately previous location and choose among currently unoccupied ring indices.

Whenever a piece lands on the mystery cell, `MovementEngine.ApplyStandardLanding` calls `MysteryCellSystem.HandleLanding(piece, playerColor)`.

`MysteryCellSystem` then:

- selects a random destination among the set `{Alpha, Beta, Gamma, Base, X, Approach}` via `_random`,
- teleports the piece to the appropriate destination:
  - `Base` resets the piece,
  - `Alpha`, `Beta`, `Gamma` move it to the corresponding special ring indices,
  - `X` teleports to the player’s X entry index,
  - `Approach` teleports to the player’s approach cell.

Additionally, LUDO-T mystery effects are applied after teleport:

- **Alpha** energizes or sickens the piece for 4 rounds (random choice).
- **Beta** forces `BriefingRounds = 4`, preventing movement until briefing resolves.
- **Gamma** behaves differently based on movement direction:
  - if moving clockwise, it flips direction to counter-clockwise,
  - if moving counter-clockwise, it teleports to Beta and applies briefing.

The briefing interaction with dice is then handled by `MysteryCellSystem.HandleBriefingRoll`, tying mystery cells directly into turn progression.

### Player strategies (AI heuristics per color)

Each player is a `Player` subclass implementing `ChooseMove`. The strategies do not attempt full game-theoretic search; instead, they evaluate candidate moves using `MovementEngine.Simulate(...)` and apply color-specific heuristics.

Common patterns across all AIs:

- Candidate moves are filtered to pieces that can move (`piece.CanMove()`), plus special handling for dice `6` to enter from base.
- For pieces already on the board, the AI uses `MovementEngine.Simulate(this, piece, diceValue, board, players)` to test whether the move is executable and to read high-level consequences (capture bonus, whether a move creates a block).

#### RedPlayer

RedPlayer plays a capture-first style:

- If any piece can capture (simulation shows `CaptureTarget != null`), it chooses the capture that results in the captured piece being closer to home (`GameRules.DistanceCapturedToHome`).
- If no capture is possible and the dice is `6` while the player has no standard-path pieces, it tries to bring a piece out of base, but only when the move would not enable an immediate capture-by-opponents condition (`MovementEngine.CanCaptureWithRollOnPath`).
- Otherwise it prefers standard-path moves that do not create blocks (it ranks moves via simulation and selects non-block-creating moves first).
- If no standard-path move is viable, it advances the home-straight piece furthest along.

#### GreenPlayer

GreenPlayer balances board control and progress:

- On `6`, it moves a piece from base only if doing so would not create a blockade that hurts its own future options (`WouldCreateBlockWithSix` uses simulation for `CreatesBlock`).
- It then tries to progress in the home straight first (furthest `HomeStraightPosition`).
- If not possible, it prefers standard-path moves from pieces that are not currently in its own block (it filters to cells where `board.GetOwnBlock(...).Count < 2`), ranking by decreasing proximity to home.
- If it can only move by extending its own blocks, it selects a block move.
- Finally, if all movement options are dominated by blockade constraints, it attempts a “block break” move by simulating moves of all pieces in block groups.

#### YellowPlayer

YellowPlayer is forced to care about captures because entering the home straight can require at least one capture while opponents remain on the ring:

- On `6`, it brings a piece out of base immediately if available.
- For pieces with `Captures < 1`, YellowPlayer looks specifically for moves that yield a capture (simulation must show `CaptureTarget != null`).
- Otherwise, it moves the piece that minimizes distance to home.

This aligns with `GameRules.CanEnterHomeStraight(...)`, which blocks home entry for pieces that have not captured yet when opponents are still on the standard path.

#### BluePlayer

BluePlayer uses a cyclic priority over its four pieces and tries to avoid harmful mystery-cell interactions:

- It maintains an internal piece-number pointer (`_nextPieceNumber`) and selects candidates in a repeating order.
- On `6`, it selects a base piece using that same cyclic order.
- For non-standard-path pieces (not on the ring), it prefers any piece that can move under simulation.
- For standard-path pieces, it ranks candidates that can move and then uses a mystery-cell check based on direction:
  - when moving clockwise, it avoids moves whose target is the mystery cell,
  - when moving counter-clockwise, it allows the move only if the target is the mystery cell.

This direction-dependent preference is encoded directly in `BluePlayer.ChooseMove` through `board.IsMysteryCell(target)` checks.

## 6. Build and test

```bash
dotnet build
dotnet test LudoGame.Tests/LudoGame.Tests.csproj
dotnet run                  # Interactive: press any key between output sections
dotnet run -- --no-pause      # Continuous output (no pauses)
```

### Unit tests (24 total)

All tests live in `LudoGame.Tests/`. A full list with descriptions is in **`TEST_CASES.md`**.

| Test class | Tests | Test type | What is verified |
|------------|-------|-----------|------------------|
| `ArchitectureTests` | 2 | Architecture | Factory creates 4 players; DI for random/logger |
| `GameRulesTests` | 9 | Unit | Ring math, blocks (T-4), home entry (T-7), CCW approach |
| `HomePathTests` | 9 | Unit | Home straight steps, exact finish (Rule 10), `MovementEngine.Evaluate` |
| `ApproachEntryTests` | 2 | Unit | CCW approach crossing and second-pass rule (T-1) |
| `GreenPlayerTests` | 1 | Behaviour | Green AI home-straight priority |
| `TestSuiteSummaryTests` | 1 | Catalogue | Prints test-type table to test output |

**Expected:** `Passed: 24, Failed: 0`

**Run with summary after tests:**

```bash
chmod +x run-tests.sh
./run-tests.sh
```

### Screenshots for report (COMP63038 steps 2 and 4)

Insert these images into your Word/PDF report:

1. **After step 2 / step 4 — test results**  
   Screenshot the terminal showing: `Passed!  - Failed: 0, Passed: 24`  
   Optional: include lines from `Test_suite_catalog_lists_all_categories_and_counts` showing the test-type table.

   `![Test results — 24 passed](screenshots/test-results-24-passed.png)`

2. **Class diagram**  
   Export from `CLASS_DIAGRAM.md` via mermaid.live or VS Code preview.  
   `![Class diagram](screenshots/class-diagram.png)`

*(Create a `screenshots/` folder and save your captures there.)*

### Interactive output sections

When running `dotnet run`, the simulation prints output in sections separated by **“Press ENTER to continue”**:

1. **Game Setup** — player intro, opening rolls, turn order → **ENTER**
2. **Each round** — turns, mystery cell, board status → **ENTER** (skipped once all 4 players finish)
3. **Pause on each placement** — when a player wins or gets 2nd/3rd/4th, output stops until **ENTER**
4. **Final Results** — standings → **ENTER** → program exits

Implemented via `IGameSectionPresenter` / `InteractiveSectionPresenter` in `GameSectionPresenter.cs`.

## 7. Challenges and Solutions

Several LUDO-T rules interact in non-obvious ways, so the main development challenge was preventing subtle rule mismatches between AI evaluation and the actual move execution.

### Keeping AI and execution consistent

Solution: make `MovementEngine` the single source of truth. AI calls `MovementEngine.Simulate(...)` to evaluate legality and high-level outcomes, while the game loop executes moves via `MovementEngine.Apply(...)`. Because both evaluation and mutation run on top of the same `GameRules` helpers, it’s much harder for the AI to propose an illegal move.

### Home-entry constraints and “approach passes”

Challenge: in LUDO-T, entering the home straight depends on both direction and prior passes, and it is further constrained by the “capture required before home straight while opponents remain” rule.

Solution: model this state with:

- `Piece.ApproachPassCount` that increments based on direction when passing the approach,
- `GameRules.CanEnterHomeStraight(...)` and `GameRules.CanEnterHomeStraightAfterCross(...)` which apply the capture requirement and direction-based pass count,
- special handling in `MovementEngine.Evaluate` / `Apply` around whether the move is crossing the approach on this turn (`countingThisCross`).

### Blockades: mixing directions and partial movement

Challenge: blocks change movement semantics (block move vs normal step-by-step landing), and when blocked by an opponent block, the correct behavior can be a partial advance rather than “no move”.

Solution: represent blocks implicitly (two or more pieces on one index) and centralize blockade logic in:

- `GameRules.IsOpponentBlock(...)` and `GameRules.TryPartialMoveBeforeBlock(...)` for preventing landing while allowing partial movement,
- `MovementEngine.ApplyBlockMove(...)` for block moves, including T-4’s “choose direction based on farthest-from-home” when directions differ within a block.

### Mystery cells and special effects

Challenge: mystery cells require correct timing (spawn after enough activity), correct persistence (round countdown), and effect application that depends on landing destination and sometimes movement direction.

Solution: split responsibilities:

- `Board.UpdateMysteryCell(...)` handles spawn/persistence and avoids illegal respawns.
- `MysteryCellSystem.HandleLanding(...)` handles teleport selection and applies Alpha/Beta/Gamma rules and briefing behavior.

This separation makes it easier to reason about “when” the mystery cell exists versus “what” it does.

### Termination and runaway simulation safety

Challenge: bonus-roll chains (e.g., repeatedly rolling 6) can create very long simulations if not bounded.

Solution: enforce hard caps in `GameManager` (`MaxRollsPerTurn` and `MaxGameRounds`) so the simulation always terminates even under adversarial random sequences.
