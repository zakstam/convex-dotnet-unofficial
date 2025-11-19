// Mutation to place a mark on the board with validation

import { mutation } from "../_generated/server";
import { v } from "convex/values";

// Helper function to check for a winner
function checkWinner(board: string[]): "X" | "O" | null {
  const winPatterns = [
    [0, 1, 2], [3, 4, 5], [6, 7, 8], // Rows
    [0, 3, 6], [1, 4, 7], [2, 5, 8], // Columns
    [0, 4, 8], [2, 4, 6]             // Diagonals
  ];

  for (const pattern of winPatterns) {
    const [a, b, c] = pattern;
    if (board[a] && board[a] === board[b] && board[a] === board[c]) {
      return board[a] as "X" | "O";
    }
  }

  return null;
}

export default mutation({
  args: {
    gameId: v.id("games"),
    username: v.string(),
    position: v.number(), // 0-8
  },
  handler: async (ctx, { gameId, username, position }) => {
    if (!username || !username.trim()) {
      throw new Error("Username cannot be empty");
    }

    const game = await ctx.db.get(gameId);

    if (!game) {
      throw new Error("Game not found");
    }

    // Validation
    if (game.status !== "playing") {
      throw new Error("Game not in progress");
    }

    const player = game.playerX === username.trim() ? "X" :
                   game.playerO === username.trim() ? "O" : null;

    if (!player) {
      throw new Error("Not a player in this game");
    }

    if (game.currentTurn !== player) {
      throw new Error("Not your turn");
    }

    if (position < 0 || position > 8) {
      throw new Error("Invalid position");
    }

    if (game.board[position] !== "") {
      throw new Error("Position already occupied");
    }

    // Make move
    const newBoard = [...game.board];
    newBoard[position] = player;

    // Check for winner
    const winner = checkWinner(newBoard);
    const isDraw = !winner && newBoard.every(cell => cell !== "");

    const updates: any = {
      board: newBoard,
      currentTurn: player === "X" ? "O" : "X",
    };

    if (winner || isDraw) {
      updates.status = "finished";
      updates.winner = winner || "draw";
      updates.finishedAt = Date.now();
    }

    await ctx.db.patch(gameId, updates);

    // Optional: Record move history
    await ctx.db.insert("moves", {
      gameId,
      player,
      position,
      timestamp: Date.now(),
    });

    return { success: true, winner: updates.winner || null };
  },
});

