# Design Patterns Used in LUDO-T Simulation

**Total patterns in this project: 9** — eight counted for the COMP63038 rubric plus the `State` pattern.
**Total for COMP63038 rubric: 8 module design patterns** (excluding the State pattern; State is present in the code but not counted here).

---

## Summary table (8 patterns)

| # | Pattern | File / class | Purpose |
|---|---------|--------------|---------|
| 1 | **Strategy** | `RedPlayer`, `GreenPlayer`, `YellowPlayer`, `BluePlayer` | Each AI chooses moves differently via `ChooseMove` |
| 2 | **Factory** | `PlayerFactory.cs` | Creates all four players in one place |
| 3 | **Facade** | `MovementEngine`, `LudoGameController` | Simple API over complex movement and game flow |
| 4 | **Adapter** | `ConsoleGameLogger` | Adapts static `GameLogger` to `IGameLogger` |
| 5 | **Template Method** | Abstract `Player` | Shared player logic; `ChooseMove` filled in by subclasses |
| 6 | **Observer** | `IGameObserver`, `PlacementTrackerObserver` | Notified when a player gets 1st–4th place |
| 7 | **Proxy** | `MovementResolver` | Same interface as movement engine; delegates to `MovementEngine` |
| 8 | **Null Object** | `NullSectionPresenter`, `NullGameObserver` | Provides safe no-op defaults for optional collaborators |

**Supporting (not counted in the 8):** Dependency Injection (`GameManager` constructor).

## Note on State

The code also uses a `PieceState` enum (`Base`, `StandardPath`, `HomeStraight`, `Home`) to manage piece lifecycle behavior, but since this pattern has not been counted for the assignment, it is documented separately rather than included in the eight-pattern total.

---

## 1. Strategy

**Where:** `LudoGame.cs` — `RedPlayer`, `GreenPlayer`, `YellowPlayer`, `BluePlayer`  
**How:** `GameManager` calls `player.ChooseMove(dice, board, players)` without knowing which AI is active.

---

## 2. Factory

**Where:** `PlayerFactory.CreatePlayers()`  
**How:** Returns `Dictionary<Color, IPlayer>` with all four player instances.

---

## 3. Facade

**Where:** `MovementEngine` (evaluate/apply/simulate), `LudoGameController` (entry to `GameManager`)  
**How:** Hides movement and startup complexity from callers.

---

## 4. State (kept — explained for your report)

**What the module teaches:** Objects change behaviour when their internal state changes.

**What we did:** Each `Piece` has `PieceState`:

- `Base` — cannot move on the ring  
- `StandardPath` — moves on ring 0–51  
- `HomeStraight` — moves on `homepath[0..4]`  
- `Home` — finished; `CanMove()` is false  

**Why enum, not many classes:** Same State *idea* as GoF, but simpler for a student project. `MovementEngine` and `Piece` methods branch on `State` instead of using `BaseState`, `PathState` classes.

**Example in code:**

```csharp
public bool CanMove() => State != PieceState.Home && Effect.BriefingRounds == 0;
```

You can tell your lecturer: *“We use the State pattern through `PieceState`; behaviour is selected by the current state value.”*

---

## 5. Adapter

**Where:** `ConsoleGameLogger` implements `IGameLogger` and forwards to `GameLogger` static methods.  
**Why:** Game logic depends on `IGameLogger`; formatting stays in `GameLogger`.

---

## 6. Template Method

**Where:** Abstract class `Player` with concrete `ChooseMove` in each colour subclass.  
**How:** Base class defines shared structure (`Pieces`, `UpdateEffectsEndOfRound`); subclasses override the variable step (`ChooseMove`).

---

## 7. Observer (added for 8th pattern set)

**Where:** `IGameObserver.cs`, `PlacementTrackerObserver`, wired in `GameManager.RegisterFinishIfNeeded`  
**How:** After `LogPlacement`, each observer receives `OnPlayerPlaced(player, place)`.  
**Why:** Decouples “someone finished” from optional tracking or future UI.

---

## 8. Proxy (added for 8th pattern set)

**Where:** `MovementResolver` in `IMoveResolver.cs`  
**How:** Implements `IMoveResolver` but every method calls `MovementEngine.Evaluate/Simulate/Apply`.  
**Why:** `GameManager` depends on `IMoveResolver`, not static methods directly — testable, controlled access.

---

## Class diagram

See **`CLASS_DIAGRAM.md`** for the full Mermaid UML diagram used in the report.

---

## Tests

See **`TEST_CASES.md`** — 24 tests (23 logic + 1 catalogue). Run `./run-tests.sh` for pass summary and test-type list in the terminal.
