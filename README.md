# Tic Tac Toe

A browser-based Tic Tac Toe implementation built for the technical assignment. The React client renders the experience and the .NET Web API owns game state, validation, move history, computer decisions, and the session scoreboard.

## Tech stack

- React 19 + TypeScript + Vite
- .NET 10 Web API
- In-memory state (no database required)
- xUnit backend tests

## Features

- Two-player mode and Play Against Computer mode
- Row, column, diagonal, and draw detection
- Winning-cell highlighting
- Move history with row and column positions
- Undo one move in two-player mode, or the human/computer pair in computer mode
- Scoreboard for X wins, O wins, and draws
- Reset game without changing the scoreboard
- Reset scoreboard
- Computer priority: win, block, center, corner, any available cell
- Backend validation for wrong turns, occupied cells, invalid coordinates, and completed games

## Run locally

### Backend

```powershell
cd backend
dotnet run --urls http://localhost:5000
```

### Frontend

In a second terminal:

```powershell
cd frontend
npm install
npm run dev
```

Open the URL printed by Vite, normally `http://localhost:5173`.

The Vite development proxy forwards `/api` requests to `http://localhost:5000`.

## API summary

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | `/api/games` | Create a game with `{ "mode": "TwoPlayer" }` or `{ "mode": "Computer" }` |
| GET | `/api/games/{id}` | Retrieve the current game state |
| POST | `/api/games/{id}/moves` | Submit `{ "player": "X", "row": 0, "column": 0 }` |
| POST | `/api/games/{id}/undo` | Undo according to the selected mode |
| POST | `/api/games/{id}/reset` | Reset the current board and history |
| GET | `/api/scoreboard` | Retrieve the session scoreboard |
| POST | `/api/scoreboard/reset` | Clear the scoreboard |

Game responses include the game ID, board, current player, mode, status, winner, winning cells, move history, and scoreboard.

## Tests

```powershell
dotnet test backend.Tests/backend.Tests.csproj
cd frontend
npm run build
```

The backend tests cover row wins, scoreboard updates, two-player undo, computer-mode pair undo, and invalid moves. The production frontend build checks TypeScript and Vite integration.

## Design decisions

- The backend is the source of truth. The frontend never calculates winners or modifies the board locally.
- State is held in a singleton service because the assignment accepts in-memory storage and the app is intended to run locally.
- Undo uses Clarification Option A: it is disabled after a game is won or drawn. This means completed scoreboard results never need to be reversed.
- A computer-mode move request is atomic: the backend applies the human move and, if the game remains active, immediately applies the computer move before returning the response.

