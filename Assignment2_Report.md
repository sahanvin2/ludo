# Assignment 2 Report: Multi-Tier Client-Server Architecture and Concurrency

## 1. Architectural Design & Component Allocation

To satisfy the requirements of Assignment 2, the LUDO-T standalone application was refactored from a monolithic application into a **Multi-Tier Client-Server Architecture**. The responsibilities have been physically and logically distributed across three distinct tiers:

### A. The Database Tier (`LudoGame.Database`)
- **Role:** Persistent Storage Layer
- **Implementation:** Built using Entity Framework Core (EF Core) and SQLite. 
- **Components:** Contains the `GameDbContext` and the `GameStateRecord` entity.
- **Responsibility:** Records the chronological history of the game state as actions are processed by the server. Using SQLite ensures the database is embedded and portable, easily satisfying the requirement to connect to a relational database system without complex infrastructure setup.

### B. The Server Tier (`LudoGame.Server`)
- **Role:** Application Logic & Concurrency Management Layer
- **Implementation:** Built using ASP.NET Core Web API and SignalR.
- **Components:** 
  - `LudoHub`: The SignalR hub that handles real-time WebSocket connections from clients.
  - `GameEngineWorker`: An ASP.NET Core `BackgroundService` that sequentially processes the game simulation logic (rolling dice, moving pieces).
  - `GameCommandQueue`: A thread-safe queue implemented using `System.Threading.Channels`.
  - `GameManager` (from `LudoGame` logic project): The central game engine that enforces LUDO rules.
- **Responsibility:** Acts as the authoritative source of truth. It receives commands from clients, queues them to handle high concurrency, processes them safely in a background worker, commits state to the Database tier, and finally broadcasts the updated `GameSnapshot` back to all connected clients.

### C. The Client Tier (`LudoGame.Gui` & `LudoGame.TestClients`)
- **Role:** Presentation & Testing Layer
- **Implementation:** 
  1. `LudoGame.Gui`: A C# WinForms desktop application leveraging `Microsoft.AspNetCore.SignalR.Client` to interact with the server.
  2. `LudoGame.TestClients`: A C# Console Application that spawns multiple instances simultaneously to hammer the server with rapid-fire asynchronous requests.
- **Responsibility:** The GUI is purely responsible for rendering the `GameSnapshot` pushed by the Server. It does not execute any game logic. The Test Client demonstrates the robust nature of the server's concurrency queue. When one client makes a move (e.g. clicks "Play Next Turn"), all other connected clients immediately receive the updated state via the SignalR push mechanism.

---

## 2. Server-Side Concurrency Mechanisms

The primary challenge of a multi-tier multiplayer game server is handling simultaneous, asynchronous requests from multiple clients without corrupting the game state. 

### Mechanism 1: Thread-Safe Queuing (`System.Threading.Channels`)
If two clients rapidly request `PlayTurn` at the exact same millisecond, executing the `GameManager` logic in parallel would lead to race conditions (e.g., both clients might try to move the same piece or advance the round simultaneously).

To resolve this, the server utilizes `Channel<T>` to build a `GameCommandQueue`.
- When a client sends a request via `LudoHub`, the request is wrapped in an asynchronous task and immediately appended to the Channel.
- SignalR immediately releases the client's thread, achieving true asynchronous non-blocking I/O.
- The `Channel` acts as a thread-safe, bounded, First-In-First-Out (FIFO) buffer.

### Mechanism 2: Sequential Background Processing (`BackgroundService`)
The `GameEngineWorker` continuously reads from the `GameCommandQueue`. 
- By utilizing a single, long-running `BackgroundService` to dequeue and execute commands, the server guarantees that **only one thread mutates the game state at any given time**.
- This completely eliminates the need for complex, granular `lock` statements over game entities, avoiding potential deadlocks.
- The worker executes the engine logic, persists the state to EF Core SQLite, and triggers SignalR to broadcast the `UpdateState` event sequentially, ensuring state consistency across the entire ecosystem.

---

## 3. Conclusion
By separating the LUDO simulation into Database, Server, and Client tiers, the application successfully demonstrates modern Distributed Systems principles. The combination of ASP.NET Core SignalR for real-time duplex communication, EF Core for persistent storage, and Channels for concurrent request queuing fulfills all Assignment 2 requirements safely and efficiently.
