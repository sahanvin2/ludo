# Unit Test Cases — LUDO-T Simulation

**Project:** `LudoGame.Tests`  
**Framework:** xUnit
**Total tests:** 27
**Run command:**

```bash
dotnet test LudoGame.Tests/LudoGame.Tests.csproj --logger "console;verbosity=detailed"
```

**Actual result:**

- `Test Run Successful.`
- `Total tests: 27`
- `Passed: 27`
- `Total time: 0.3049 Seconds`

This suite includes the catalogue test `TestSuiteSummaryTests.Test_suite_catalog_lists_all_categories_and_counts`, which prints the test class counts and types to the console.
## Test suite overview

| Test class | Count | Focus area |
|------------|-------|------------|
| `ArchitectureTests` | 2 | Factory, dependency injection |
| `GameLoggerTests` | 3 | Logger output formatting and message text |
| `GameRulesTests` | 9 | Pure rule helpers, home entry, blocks |
| `HomePathTests` | 9 | Home straight movement and formatting |
| `ApproachEntryTests` | 2 | CCW approach crossing |
| `GreenPlayerTests` | 1 | Green AI behaviour |
| `TestSuiteSummaryTests` | 1 | Prints test-type catalogue to test output |
| **Total** | **27** | |

---

## Detailed test cases table

| Test class | Test name | Focus |
|------------|-----------|-------|
| `ArchitectureTests` | `PlayerFactory_creates_all_four_players_as_IPlayer` | Factory creation and interface use |
| `ArchitectureTests` | `GameManager_accepts_injected_random_and_logger` | Dependency injection support |
| `GameLoggerTests` | `LogMysterySpawn_uses_singular_round_text_for_one_round` | Logger singular/plural message text |
| `GameLoggerTests` | `LogMysteryCellStatus_uses_singular_round_text_for_one_round` | Logger singular/plural status text |
| `GameLoggerTests` | `LogPartialMove_includes_player_color` | Partial move formatting includes colour |
| `GameRulesTests` | `DistanceFromHome_uses_clockwise_steps_for_clockwise_piece` | Distance to home calculation |
| `GameRulesTests` | `GetBlockMovementDirection_picks_farthest_from_home_when_opposite` | Block movement direction rule (T-4) |
| `GameRulesTests` | `ApplyHomeStraightSteps_zero_steps_enters_at_position_zero` | Home path entrance mapping |
| `GameRulesTests` | `CanCompleteApproachEntry_allows_zero_steps_in_home` | Approach entry with zero home steps |
| `GameRulesTests` | `StepForward_wraps_modulo_52` | Ring wrap-around movement |
| `GameRulesTests` | `CanEnterHomeStraightAfterCross_counts_ccw_second_pass_on_crossing` | CCW home entry after approach crossing |
| `GameRulesTests` | `DistanceFromHome_for_home_straight_uses_remaining_steps` | Home path remaining-distance logic |
| `GameRulesTests` | `CanEnterHomeStraight_requires_capture_while_opponents_on_ring` | Home entry capture requirement (T-7) |
| `GameRulesTests` | `CanEnterHomeStraight_waives_capture_when_no_opponent_on_ring` | Home entry when no opponent remains |
| `HomePathTests` | `StepsInHome_zero_means_entrance_tile_not_invalid` | Home path entry validation |
| `HomePathTests` | `Cw_from_8_by_5_lands_on_approach_with_zero_home_steps` | CW approach landing and home steps |
| `HomePathTests` | `ApplyHomeStraightSteps_maps_steps_to_positions_0_through_4` | Home step to path index mapping |
| `HomePathTests` | `Exact_roll_from_homepath0_requires_5` | Exact finish from homepath[0] |
| `HomePathTests` | `Exact_roll_from_homepath4_requires_1` | Exact finish from homepath[4] |
| `HomePathTests` | `FormatPieceLocation_uses_bracketed_home_path_index` | Home path location formatting |
| `HomePathTests` | `HomeStraight_piece_can_move_partially_along_home_path` | Partial home path move validity |
| `HomePathTests` | `HomeStraight_piece_cannot_move_beyond_home` | Overshoot prevention on home path |
| `HomePathTests` | `Overshoot_home_path_returns_false` | Home path overshoot validation |
| `ApproachEntryTests` | `Ccw_from_18_by_6_ring_end_is_12_but_home_entry_is_position_1` | CCW ring-to-home mapping |
| `ApproachEntryTests` | `Ccw_crossing_approach_requires_second_pass_before_entry` | CCW second-pass home entry rule |
| `GreenPlayerTests` | `ChooseMove_advances_piece_on_home_straight_when_roll_is_exact` | Green AI home-straight selection |
| `TestSuiteSummaryTests` | `Test_suite_catalog_lists_all_categories_and_counts` | Test suite catalogue reporting |

---

## 6. TestSuiteSummaryTests (1 test)

### 6.1 `Test_suite_catalog_lists_all_categories_and_counts`
- **Type:** Documentation / catalogue
- **Verifies:** Writes a table of all test classes and types to the test runner output (for screenshots).
- **Run with:** `dotnet test --logger "console;verbosity=normal"` to see the table in the terminal.

---

## 1. ArchitectureTests (2 tests)

### 1.1 `PlayerFactory_creates_all_four_players_as_IPlayer`
- **File:** `LudoGame.Tests/ArchitectureTests.cs`
- **Verifies:** `PlayerFactory.CreatePlayers()` returns exactly 4 players (Red, Green, Yellow, Blue), each implementing `IPlayer` with the correct colour.
- **Pattern tested:** Factory

### 1.2 `GameManager_accepts_injected_random_and_logger`
- **File:** `LudoGame.Tests/ArchitectureTests.cs`
- **Verifies:** `GameManager` can be constructed with injected `TestRandom` and `TestLogger` without throwing; logger starts empty.
- **Pattern tested:** Dependency Injection

---

## 2. GameLoggerTests (3 tests)

### 2.1 `LogMysterySpawn_uses_singular_round_text_for_one_round`
- **Verifies:** `GameLogger.LogMysterySpawn` prints "round" singular when `rounds == 1`.

### 2.2 `LogMysteryCellStatus_uses_singular_round_text_for_one_round`
- **Verifies:** `GameLogger.LogMysteryCellStatus` prints "round" singular when `rounds == 1`.

### 2.3 `LogPartialMove_includes_player_color`
- **Verifies:** `GameLogger.LogPartialMove` includes the player colour in the partial-block message.

---

## 3. GameRulesTests (9 tests)

### 2.1 `DistanceFromHome_uses_clockwise_steps_for_clockwise_piece`
- **Verifies:** For a clockwise piece on the ring, `GameRules.DistanceFromHome` equals steps to the approach cell modulo 52.

### 2.2 `GetBlockMovementDirection_picks_farthest_from_home_when_opposite`
- **Verifies:** When a block contains pieces moving in opposite directions, `GetBlockMovementDirection` returns a valid direction (T-4 rule).

### 2.3 `ApplyHomeStraightSteps_zero_steps_enters_at_position_zero`
- **Verifies:** `ApplyHomeStraightSteps(piece, 0)` sets state to `HomeStraight` and position index `0`.

### 2.4 `CanCompleteApproachEntry_allows_zero_steps_in_home`
- **Verifies:** Landing on approach with zero steps into home is a valid entry when capture requirements are met.

### 2.5 `StepForward_wraps_modulo_52`
- **Verifies:** Stepping forward from cell 51 clockwise wraps to cell 0.

### 2.6 `CanEnterHomeStraightAfterCross_counts_ccw_second_pass_on_crossing`
- **Verifies:** CCW piece with `ApproachPassCount == 1` can enter home **after** crossing approach on the current move (`CanEnterHomeStraightAfterCross`), but not before (`CanEnterHomeStraight`).

### 2.7 `DistanceFromHome_for_home_straight_uses_remaining_steps`
- **Verifies:** Piece at `homepath[2]` has distance 3 to finish (5 − 2).

### 2.8 `CanEnterHomeStraight_requires_capture_while_opponents_on_ring`
- **Verifies:** T-7 — piece with `Captures == 0` cannot enter home while an opponent is on the ring; after `Captures = 1`, entry is allowed.

### 2.9 `CanEnterHomeStraight_waives_capture_when_no_opponent_on_ring`
- **Verifies:** T-7 exception — capture not required when no opponent remains on the standard path.

---

## 3. HomePathTests (9 tests)

### 3.1 `StepsInHome_zero_means_entrance_tile_not_invalid`
- **Verifies:** Approach crossing with 0 steps into home is valid; piece enters at `homepath[0]` with ring position cleared.

### 3.2 `Cw_from_8_by_5_lands_on_approach_with_zero_home_steps`
- **Verifies:** Blue piece at 8 moving 5 CW lands on approach (13) with 0 home steps.

### 3.3 `ApplyHomeStraightSteps_maps_steps_to_positions_0_through_4`
- **Verifies:** Steps 1–4 map to home path indices 1–4 and pass `IsValidHomePathPosition`.

### 3.4 `Exact_roll_from_homepath0_requires_5`
- **Verifies:** Rule 10 — from `homepath[0]`, only a roll of 5 finishes (0 + 5 = 5).

### 3.5 `Exact_roll_from_homepath4_requires_1`
- **Verifies:** Rule 10 — from `homepath[4]`, only a roll of 1 finishes.

### 3.6 `FormatPieceLocation_uses_bracketed_home_path_index`
- **Verifies:** Log format `bluehomepath[0]` for home straight display.

### 3.7 `HomeStraight_piece_can_move_partially_along_home_path`
- **Verifies:** `MovementEngine.Evaluate` allows move from home path index 1 by 2 steps.

### 3.8 `HomeStraight_piece_cannot_move_beyond_home`
- **Verifies:** `MovementEngine.Evaluate` returns `NoMove` when roll would overshoot home from index 4.

### 3.9 `Overshoot_home_path_returns_false`
- **Verifies:** `ApplyHomeStraightSteps(piece, 6)` returns false (invalid overshoot).

---

## 4. ApproachEntryTests (2 tests)

### 4.1 `Ccw_from_18_by_6_ring_end_is_12_but_home_entry_is_position_1`
- **Verifies:** CCW move from 18 by 6: ring end is 12, approach crossed at index 4, 1 step into home → `homepath[1]`.

### 4.2 `Ccw_crossing_approach_requires_second_pass_before_entry`
- **Verifies:** CCW piece with one approach pass can enter after cross but not before (T-1).

---

## 5. GreenPlayerTests (1 test)

### 5.1 `ChooseMove_advances_piece_on_home_straight_when_roll_is_exact`
- **Verifies:** Green player selects the piece on home path index 4 when roll is 1 (exact finish move).
- **Assignment ref:** §2.1.2 Green — prioritise home straight.

---

## Coverage gaps (not yet tested)

These areas are implemented in code but have **no dedicated unit test** yet:

| Area | Suggested future test |
|------|------------------------|
| `MysteryCellSystem` | Spawn timing, teleport, Alpha/Beta/Gamma effects |
| `RedPlayer` / `YellowPlayer` / `BluePlayer` | Capture-first, cyclic, mystery preference |
| Block capture (T-8) | Equal-size block vs block |
| T-6 blockade break | Third consecutive six with blockade |
| `MovementEngine.Apply` | Full move execution with logging |
| Interactive sections | `InteractiveSectionPresenter` (manual demo only) |

---


