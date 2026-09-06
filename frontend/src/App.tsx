import { useEffect, useState } from 'react'
import './App.css'

type Mode = 'TwoPlayer' | 'Computer'
type GameStatus = 'InProgress' | 'Won' | 'Draw'
type Move = { number: number; player: string; row: number; column: number }
type Scoreboard = { xWins: number; oWins: number; draws: number }
type GameState = {
  gameId: string; board: (string | null)[]; currentPlayer: string; mode: Mode; status: GameStatus
  winner: string | null; winningCells: number[]; moveHistory: Move[]; scoreboard: Scoreboard
}

const api = async (path: string, options?: RequestInit) => {
  const response = await fetch(`/api${path}`, { headers: { 'Content-Type': 'application/json' }, ...options })
  const body = await response.json()
  if (!response.ok) throw new Error(body.message ?? 'The request could not be completed.')
  return body
}

function App() {
  const [mode, setMode] = useState<Mode>('TwoPlayer')
  const [game, setGame] = useState<GameState | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const startGame = async (nextMode = mode) => {
    setBusy(true); setError('')
    try { setGame(await api('/games', { method: 'POST', body: JSON.stringify({ mode: nextMode }) })) }
    catch (caught) { setError(caught instanceof Error ? caught.message : 'Unable to start the game.') }
    finally { setBusy(false) }
  }

  useEffect(() => { void startGame() }, [])

  const action = async (path: string, options?: RequestInit) => {
    if (!game) return
    setBusy(true); setError('')
    try { setGame(await api(path, options)) }
    catch (caught) { setError(caught instanceof Error ? caught.message : 'The request could not be completed.') }
    finally { setBusy(false) }
  }

  const resetScoreboard = async () => {
    setBusy(true); setError('')
    try {
      const scoreboard: Scoreboard = await api('/scoreboard/reset', { method: 'POST' })
      setGame(current => current ? { ...current, scoreboard } : current)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The scoreboard could not be reset.')
    } finally { setBusy(false) }
  }

  const play = (index: number) => {
    if (!game || game.board[index] || game.status !== 'InProgress' || busy) return
    void action(`/games/${game.gameId}/moves`, { method: 'POST', body: JSON.stringify({ player: game.currentPlayer, row: Math.floor(index / 3), column: index % 3 }) })
  }

  const status = game?.status === 'Won' ? `Player ${game.winner} wins` : game?.status === 'Draw' ? 'Draw game' : `Player ${game?.currentPlayer ?? 'X'}'s turn`

  return (
    <main className="app-shell">
      <header className="topbar"><div><p className="eyebrow">LOCAL ARCADE / 001</p><h1>Tic Tac Toe</h1></div><span className="live-dot">● API connected</span></header>
      <section className="intro"><div><p className="eyebrow">A small game of perfect focus</p><h2>Make your mark.</h2></div><div className="mode-picker"><span>Game mode</span><button className={mode === 'TwoPlayer' ? 'selected' : ''} onClick={() => { setMode('TwoPlayer'); void startGame('TwoPlayer') }}>Two players</button><button className={mode === 'Computer' ? 'selected' : ''} onClick={() => { setMode('Computer'); void startGame('Computer') }}>Vs computer</button></div></section>
      {error && <div className="alert" role="alert">{error}</div>}
      <section className="layout">
        <div className="play-area">
          <div className="status-line"><span className={game?.status === 'InProgress' ? 'turn-badge' : 'turn-badge complete'}>{status}</span><span>{game?.moveHistory.length ?? 0} / 9 moves</span></div>
          <div className="board" aria-label="Tic Tac Toe board">{game?.board.map((cell, index) => <button key={index} className={`cell ${cell === 'X' ? 'x' : cell === 'O' ? 'o' : ''} ${game.winningCells.includes(index) ? 'winner' : ''}`} onClick={() => play(index)} aria-label={`Row ${Math.floor(index / 3) + 1}, Column ${(index % 3) + 1}${cell ? `, ${cell}` : ''}`}>{cell}</button>)}</div>
          <div className="controls"><button className="primary" disabled={busy || !game} onClick={() => void action(`/games/${game?.gameId}/reset`, { method: 'POST' })}>Reset game</button><button disabled={busy || !game?.moveHistory.length || game.status !== 'InProgress'} onClick={() => void action(`/games/${game?.gameId}/undo`, { method: 'POST' })}>Undo last move</button></div>
        </div>
        <aside className="side-panel"><section><div className="section-heading"><h3>Scoreboard</h3><button className="text-button" disabled={busy || !game} onClick={() => void resetScoreboard()}>Reset</button></div><div className="scores"><div><strong>{game?.scoreboard.xWins ?? 0}</strong><span>X wins</span></div><div><strong>{game?.scoreboard.oWins ?? 0}</strong><span>O wins</span></div><div><strong>{game?.scoreboard.draws ?? 0}</strong><span>Draws</span></div></div></section><section className="history"><div className="section-heading"><h3>Move history</h3><span>{game?.moveHistory.length ?? 0}</span></div>{game?.moveHistory.length ? <ol>{game.moveHistory.map(move => <li key={`${move.number}-${move.row}-${move.column}`}><span className={`mini-mark ${move.player.toLowerCase()}`}>{move.player}</span><span>Move {move.number}</span><span>R{move.row + 1}, C{move.column + 1}</span></li>)}</ol> : <p className="empty">Your moves will appear here.</p>}</section></aside>
      </section>
    </main>
  )
}

export default App
