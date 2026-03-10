# ChatApp - Sanlam Studios Technical Assessment

Real-time multi-user chat application built for the Sanlam Studios technical assessment.

## Solution Overview

A full-stack chat application that allows multiple anonymous users to communicate in real time. Each browser session is treated as a unique user identified by a self-chosen username. Messages persist in a relational database so users see history when they join.

The backend is the primary focus - it is built using Clean Architecture with CQRS, a SignalR hub for real-time broadcasting, EF Core for persistence, and Polly for resilience.

## Screenshots

| Landing | Chat |
|---|---|
| ![Landing page](docs/application%20landing.png) | ![Chat room](docs/global%20chat.png) |

## Technical Choices & Rationale

### Backend - ASP.NET Core (.NET 8)

#### Clean Architecture (Domain / Application / Infrastructure / API)

The solution is structured into four layers with a strict dependency rule: outer layers depend on inner ones, never the reverse.

- **Domain** - pure C# entities and repository interfaces with zero framework dependencies. `Message` lives here.
- **Application** - orchestration logic (commands, queries, handlers). Depends only on Domain abstractions, not on EF Core or any infrastructure concern.
- **Infrastructure** - EF Core, PostgreSQL, Polly. Implements the interfaces defined in Domain.
- **API** - thin HTTP/SignalR surface that delegates everything to Application via MediatR.

This means I can swap PostgreSQL for a different database, or replace the SignalR hub with a different transport, without touching business logic. It also makes the handlers easy to unit test in isolation.

#### CQRS with MediatR

CQRS (Command Query Responsibility Segregation) is the pattern of separating operations that change state (commands) from operations that read state (queries). In this project:

- `SendMessageCommand` - saves a message and returns the saved DTO
- `GetRecentMessagesQuery` - reads the last 50 messages

MediatR is a library that implements the mediator pattern in .NET. Instead of controllers calling services directly, they dispatch a request object through MediatR, which finds and invokes the correct handler. This means:

- Controllers are completely free of business logic - they just send a request and return the result
- Each handler has one job and one reason to change
- Adding a new operation means adding a new command/query class and handler, with no changes to existing code

It's a pattern more commonly seen in enterprise .NET backends. I chose it here because it naturally fits Clean Architecture and makes the intent of each operation explicit from its class name alone.

#### SignalR

SignalR is Microsoft's real-time communication library built into ASP.NET Core. It abstracts the transport layer - it tries WebSockets first and automatically falls back to Server-Sent Events or long-polling if the client doesn't support it. The server-side hub (`ChatHub`) exposes a `SendMessage` method that clients invoke, and the hub broadcasts the saved message back to all connected clients via `Clients.All.SendAsync("ReceiveMessage", dto)`.

#### PostgreSQL + EF Core

Messages are structured data with a clear schema, so a relational database is a natural fit. EF Core provides code-first migrations (the schema is defined in C# and versioned with the codebase), a clean LINQ query API, and the `DbContext` abstraction keeps the repository implementation decoupled from the domain model. The repository interface lives in Domain; the EF Core implementation lives in Infrastructure - so the domain has no knowledge of how data is persisted.

#### Polly

Polly is a .NET resilience library. I wrapped all database operations in a `WaitAndRetryAsync` policy that retries up to 3 times with exponential backoff (200ms, 400ms, 600ms). This handles transient failures - momentary DB connection drops or lock contention - without surfacing errors to the client. It's a small addition that meaningfully improves reliability under load.

#### Redis Backplane

SignalR manages connected clients in memory. If the API is scaled out to multiple instances, a client connected to instance A won't receive a message sent by a client on instance B, because instance A has no knowledge of instance B's connections. The Redis backplane solves this: all instances subscribe to a shared Redis pub/sub channel, and when any instance broadcasts a message, Redis fans it out to all other instances.

#### Global Exception Middleware

Rather than wrapping every controller action in try/catch, a single middleware catches all unhandled exceptions, logs them with the request method and path, and returns a consistent `{ "error": "..." }` JSON response with a 500 status. This prevents stack traces from leaking to clients and ensures every error follows the same response shape.

### Frontend - Next.js 16 + TypeScript

The assessment allows any frontend. I chose Next.js because it has first-class TypeScript support and I wanted type safety across the component boundaries without significant setup overhead. The frontend is intentionally minimal - its purpose is to prove the backend works end-to-end, not to demonstrate frontend depth.

#### `useChat` hook

All SignalR connection management is encapsulated in a single custom hook. On mount it fetches message history from the REST endpoint, then starts the SignalR connection and subscribes to `ReceiveMessage` broadcasts. An `active` flag guards against a subtle React StrictMode bug where the cleanup function runs between two invocations of the effect - without it, the `ReceiveMessage` handler gets re-registered on an already-running connection, causing every message to appear twice.

#### Tailwind CSS

Utility-first CSS for layout. No component library was needed for something this small, and Tailwind avoids the overhead of writing and maintaining separate stylesheet files.

## AI Tool Usage

I used **Claude** as an assistant at specific points in the project. All architectural decisions, implementation choices, and final code were mine — Claude played a supporting role in the following areas:

- **Debug sessions**: When I hit runtime errors, I used Claude to talk through root causes. For example, the duplicate message bug caused by React StrictMode's double-invocation of `useEffect`, and a `GetRecentAsync` ordering bug returning the oldest 50 messages instead of the most recent 50. In both cases I traced the issue with Claude's input, understood the cause, and applied the fix myself.
- **Frontend scaffolding**: Claude bootstrapped the initial Next.js frontend structure so I could focus my attention on the backend, which is the core of this assessment.
- **Design input**: I used Claude as a sounding board for technology choices. A good example is the real-time transport decision - Claude walked me through the trade-offs between raw WebSockets and SignalR (automatic transport fallback, built-in hub abstraction, native ASP.NET Core integration), which reinforced my decision to go with SignalR.
- **README editing**: Claude helped with grammar and phrasing in this document. The content, structure, and technical decisions described here are my own.

### Key prompts

- *"scaffold a minimal Next.js frontend that connects to a SignalR hub and displays messages"* - used to bootstrap the initial frontend structure
- *"I'm getting duplicate messages on the frontend, here's the error..."* - debug session that led to identifying the React StrictMode/`useEffect` cleanup race condition
- *"what are the trade-offs between raw WebSockets and SignalR for a .NET backend?"* - used to validate the real-time transport decision
- *"clean up the grammar and the tone in this README, don't change the content or structure"* - used for proofreading passes on this document

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL](https://www.postgresql.org/) running locally on port `5432`
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) - install once with:

```bash
dotnet tool install --global dotnet-ef
```

> **Redis is optional.** The app runs fine as a single instance without it. See step 3 for how to enable it.

## Setup & Running Locally

### 1. Clone the repository

```bash
git clone https://github.com/abelmasingita/sanlam-chat.git
cd sanlam-chat
```

### 2. Create the database

Connect to your local PostgreSQL instance and create the database:

```sql
CREATE DATABASE chatapp;
```

### 3. Configure the API

Create `src/ChatApp.Api/appsettings.Development.json` (this file is gitignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=chatapp;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

To enable the Redis backplane (optional - only needed when running multiple API instances), add the Redis connection string to the same file:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=chatapp;Username=postgres;Password=YOUR_PASSWORD",
    "Redis": "localhost:6379"
  }
}
```

If you have Docker, you can spin up Redis with:

```bash
docker run -d -p 6379:6379 redis
```

### 4. Run database migrations

From the repository root:

```bash
dotnet ef database update --project src/ChatApp.Infrastructure --startup-project src/ChatApp.Api
```

This applies all pending migrations and creates the schema in the `chatapp` database.

### 5. Start the API

```bash
cd src/ChatApp.Api
dotnet run
```

API runs on `http://localhost:5111`. Swagger UI is available at `http://localhost:5111/swagger`.

### 6. Configure the frontend

Create `src/ChatApp.Web/.env.local`:

```
NEXT_PUBLIC_API_URL=http://localhost:5111
```

### 7. Start the frontend

```bash
cd src/ChatApp.Web
npm install
npm run dev
```

Frontend runs on `http://localhost:3000`.

### 8. Open the app

Navigate to `http://localhost:3000`, enter a username, and start chatting. Open a second browser tab with a different username to test real-time broadcasting.

## Diagrams

Technical diagrams are in the `/docs` folder:

- [`component-diagram.drawio.pdf`](docs/component-diagram.drawio.pdf) - Backend component breakdown
- [`erd-diagram.drawio.pdf`](docs/erd-diagram.drawio.pdf) - Database schema
- [`sequence-diagram.drawio.pdf`](docs/sequence-diagram.drawio.pdf) - Message flow from client send to broadcast
- [`chat-architecture.drawio.pdf`](docs/chat-architecture.drawio.pdf) - Full system architecture overview

## Assumptions & Trade-offs

| Assumption / Trade-off | Reasoning |
|---|---|
| Anonymous authentication (username input only) | The requirement explicitly allows it. Adding JWT/OAuth would be significant scope for minimal assessment value. |
| Single global chat room | The requirement describes a single shared chat space. Room/channel support would require schema changes and was out of scope. |
| Last 50 messages shown on join | A reasonable default. No pagination is implemented - acceptable for a demo, not for production. |
| Redis backplane disabled by default | Requires a running Redis instance. Disabled for ease of local setup; the wiring is in place to enable with one line. |
| No backend input validation | Frontend trims and blocks empty messages. Production would require server-side validation (length limits, sanitisation). |

## Known Limitations

- No authentication - any username can be claimed by any user
- No message pagination - only the 50 most recent messages load on join
- Single room - all users share one global conversation
- No user presence - no online/offline indicators
- No typing indicators
- No message editing or deletion

## What I Would Do Differently in Production

- **Authentication**: JWT-based auth with refresh tokens, or OAuth via an identity provider
- **Authorisation**: Scope SignalR connections to authenticated users; sign messages with the server-verified identity rather than a client-supplied username
- **Rooms / channels**: Add a `Room` entity, scope hub groups per room, and require users to join a room before broadcasting
- **Pagination**: Cursor-based pagination on `GET /api/messages` with infinite scroll on the frontend
- **Input validation**: `FluentValidation` pipeline behaviour in MediatR for all commands
- **Rate limiting**: Per-connection message rate limiting on the hub to prevent flooding
- **Redis backplane**: Enable for horizontal scaling; use managed Redis (e.g. Azure Cache for Redis)
- **Health checks**: `AddHealthChecks()` with DB and Redis probes, exposed at `/health`
- **Observability**: Structured logging (Serilog - OpenTelemetry), distributed tracing, metrics
- **Testing**: Unit tests for handlers, integration tests for the hub using `WebApplicationFactory`
- **CI/CD**: GitHub Actions pipeline for build, test, and containerised deployment
- **Secrets management**: Azure Key Vault / AWS Secrets Manager instead of environment files
