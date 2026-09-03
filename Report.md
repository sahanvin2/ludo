# LUDO-T Simulation Report

## 1. Structures used to represent the Board and Pieces
- **Piece**: Represented as a `Piece` class containing properties such as `Id` (e.g., R1, B2), `Color` (Enum), `State` (Base, StandardPath, HomeStraight, Home), `Position` (integer index), `Direction` (Clockwise / Counter-Clockwise), `CapturedPiecesCount`, and `StatusEffect`.
- **Board**: Represented primarily as a static utility class `Board` that maintains constants for cell lengths (52 standard cells, 5 home straight cells) and mapping logic (`GetApproachCell`, `GetStartCellX`). It operates on an integer-based coordinate system wrapping via modulo arithmetic for the standard path.
- **GameEngine**: Maintains lists of `Player` objects and tracks the `MysteryCellManager`.
- **MysteryCellManager**: Tracks the location, rounds remaining, and teleportation logic for mystery cells.

## 2. Justification for the used structures
- **Integer Grid System**: Mapping the board to a 0-51 circular integer index simplifies movement and distance calculations. Rather than maintaining a complex graph of interconnected Node objects, using modulo arithmetic (`(pos + move) % 52`) is computationally cheaper and less prone to memory leaks.
- **State Properties on Piece**: Instead of storing Pieces inside arrays of Cells, each `Piece` tracks its own position and state. This allows quick filtering via LINQ (e.g., `AllPieces.Where(p => p.Position == 5)`) which is efficient enough for 16 total pieces and drastically simplifies tracking teleportation and movement.
- **Enums for Status and States**: Enums ensure compile-time safety and prevent illegal states (e.g., a piece being in "Base" and "Home" simultaneously).

## 3. Design Principles (SOLID, Design Patterns, OOP)
- **Single Responsibility Principle (SRP)**: 
  - `GameEngine` is solely responsible for determining valid moves and applying them.
  - `Player` classes are solely responsible for deciding *which* move to take.
  - `MysteryCellManager` isolates the logic for spawning and teleporting pieces away from the main game loop.
- **Open/Closed Principle (OCP)** & **Strategy Pattern**: The `Player` base class is abstract. The specific behaviors for Red, Green, Yellow, and Blue players are implemented in derived classes. If a new player behavior is needed, we can create a new subclass without modifying existing code.
- **Encapsulation**: Object states like `Piece.Id` and `Player.Color` are read-only (`{ get; }`) after initialization, preventing accidental mutation during gameplay. The `GameEngine` uses private methods for complex internal tasks (like block checking) while exposing only what's necessary (`GetPossibleMoves`, `ExecuteMove`).

## 4. Efficiency of the Program
- **Time Complexity**: The game logic operates with $O(N)$ operations where $N$ is the total number of pieces (16). Checking for blocks, finding valid moves, and executing captures iterate over small arrays (size 16), which is instantaneous.
- **Space Complexity**: Memory footprint is minimal ($O(1)$ constant size). We only allocate 16 `Piece` objects, 4 `Player` objects, and 1 `GameEngine`. There are no dynamic board arrays resizing during runtime.
- **Justification**: For a simulation of this size, clarity and bug-free logic are more important than micro-optimizations. However, by avoiding a 2D grid structure in favor of a 1D circular index and object-based tracking, we saved significant memory and eliminated the overhead of iterating over empty cells.
