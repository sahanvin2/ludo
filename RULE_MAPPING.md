# LUDO-T Rule Mapping

This document maps assignment rules and design criteria to the exact code locations and tests in the repository.

## Rule implementations

### T-4: Block movement direction
- Implementation: `GameRules.GetBlockMovementDirection` — `GameRules.cs:92`
- Used by:
  - `MovementEngine.Evaluate` — `MovementEngine.cs:92`
  - `MovementEngine.ApplyBlockMove` — `MovementEngine.cs:262`
- Test: `GetBlockMovementDirection_picks_farthest_from_home_when_opposite` — `LudoGame.Tests/GameRulesTests.cs:23`

### T-6: Partial move before opponent block
- Implementation: `GameRules.TryPartialMoveBeforeBlock` — `GameRules.cs:247`
- Supporting helper: `GameRules.TryMoveCumulative` — `GameRules.cs:260`
- Used by:
  - `MovementEngine.Evaluate` — `MovementEngine.cs:120`
  - `MovementEngine.ApplyStandardLanding` — `MovementEngine.cs:209`
- Test coverage: logic known from home path and block tests in `LudoGame.Tests/GameRulesTests.cs` and `LudoGame.Tests/HomePathTests.cs`

### T-7: Capture required before home entry
- Implementation: `GameRules.CanEnterHomeStraight` — `GameRules.cs:216`
- CCW two-pass extension: `GameRules.CanEnterHomeStraightAfterCross` — `GameRules.cs:228`
- Used by:
  - `MovementEngine.Evaluate` when crossing approach — `MovementEngine.cs:120`
  - `MovementEngine.Apply` when applying a move across approach — `MovementEngine.cs:176`
- Tests: 
  - `CanEnterHomeStraightAfterCross_counts_ccw_second_pass_on_crossing` — `LudoGame.Tests/GameRulesTests.cs:70`
  - `CanEnterHomeStraight_requires_capture_while_opponents_on_ring` — `LudoGame.Tests/GameRulesTests.cs:95`
  - `CanEnterHomeStraight_waives_capture_when_no_opponent_on_ring` — `LudoGame.Tests/GameRulesTests.cs:119`

### Home-straight mapping and exact finish
- Implementation: `GameRules.ApplyHomeStraightSteps` — `GameRules.cs:126`
- Home straight move execution: `MovementEngine.ApplyHomeStraight` — `MovementEngine.cs:291`
- Exact-finish prevention: `MovementEngine.Evaluate` rejects overshoot in home straight — `MovementEngine.cs:70`
- Tests:
  - `ApplyHomeStraightSteps_zero_steps_enters_at_position_zero` — `LudoGame.Tests/GameRulesTests.cs:42`
  - `ApplyHomeStraightSteps_maps_steps_to_positions_0_through_4` — `LudoGame.Tests/HomePathTests.cs:32`
  - `Exact_roll_from_homepath0_requires_5` — `LudoGame.Tests/HomePathTests.cs:47`
  - `HomeStraight_piece_cannot_move_beyond_home` — `LudoGame.Tests/HomePathTests.cs:98`

### Capture detection and resolution
- Implementation: `GameRules.FindCapturableAt` — `GameRules.cs:278`
- Execution in normal landing: `MovementEngine.ApplyStandardLanding` — `MovementEngine.cs:221`
- Execution in block capture: `MovementEngine.ApplyBlockMove` — `MovementEngine.cs:262`

### Mystery cell spawning and effects
- Mystery cell lifecycle: `Board.UpdateMysteryCell` — `LudoGame.cs:160`
- Mystery landing handling: `MysteryCellSystem.HandleLanding` — `LudoGame.cs:247`
- Teleport effect application: `MysteryCellSystem.ApplyTeleportEffect` — `LudoGame.cs:265`
- Game loop integration: `LudoGameController` calls `UpdateMysteryCell` — `LudoGameController.cs:112`

### Movement modifiers from mystery effects
- Implementation: `GameRules.GetModifiedMovement` — `GameRules.cs:26`
- Used by: `MovementEngine.Evaluate` and `MovementEngine.Apply` — `MovementEngine.cs:83`, `MovementEngine.cs:159`

## Design patterns and architectural criteria

### Facade
- `MovementEngine` provides a unified interface for move evaluation and execution.
- Key location: `MovementEngine.cs:33`
- Reason: AI and game execution both depend on the same rule engine.

### Template Method / Strategy
- Abstract base class: `Player` — `LudoGame.cs:307`
- Concrete players:
  - `RedPlayer` — `LudoGame.cs:333`
  - `GreenPlayer` — `LudoGame.cs:396`
  - `YellowPlayer` — `LudoGame.cs:468`
  - `BluePlayer` — `LudoGame.cs:523`
- Reason: shared behavior in `Player`, individual move strategy in subclasses.

### Factory
- `PlayerFactory.CreatePlayers()` — `PlayerFactory.cs:12`
- Reason: centralises creation of AI players and supports testable dependency injection.

### Adapter
- `ConsoleGameLogger` implements `IGameLogger` by delegating to `GameLogger`.
- Key location: `ConsoleGameLogger.cs:1`
- Reason: adapts static logging helpers to the game logging interface.

### Observer
- `IGameObserver` / `PlacementTrackerObserver` — `IGameObserver.cs:1`
- `GameManager` / `LudoGameController` notify observers when a player places.
- Reason: decouples placement tracking from core game flow.

### Proxy
- `MovementResolver` implements `IMoveResolver` and forwards calls to `MovementEngine`.
- Key location: `IMoveResolver.cs:1`
- Reason: preserves an interface boundary while reusing the shared movement engine.

### Null Object
- `NullSectionPresenter` — `GameSectionPresenter.cs:1`
- `NullGameObserver` — `IGameObserver.cs:14`
- Reason: provides safe no-op defaults for optional collaborators in tests and non-interactive runs.

## Tests that validate rules

- `LudoGame.Tests/GameRulesTests.cs` — core rule validation.
- `LudoGame.Tests/HomePathTests.cs` — home path, approach crossing, and exact-finish rules.
- `LudoGame.Tests/ApproachEntryTests.cs` — approach crossing and CCW entry rules.
- `LudoGame.Tests/GreenPlayerTests.cs` — AI decision behavior on home straight.

## Notes

- The codebase already includes strong in-code comments and unit tests for the main assignment rules.
- This file is a quick reference for where each rule is implemented and where its behavior is tested.
