using System;
using System.Collections.Generic;
using ChessLogic.Enums;
using ChessLogic.Pieces;
using Constants;
using Models;
using UnityEngine;

namespace ChessLogic
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private float deathSize = 1.5f;
        [SerializeField] private float deathSpacing = 0.3f;
        [SerializeField] private BoardStats boardStats;

        private ChessPieceManager _chessPieceManager;
        private SpecialMove _specialMove;
        private List<Vector2Int[]> _moveList = new();
        private ChessPiece[,] _chessPieces;
        private List<ChessPiece> _deadWithes = new();
        private List<ChessPiece> _deadBlacks = new();
        private List<Vector2Int> _availableMoves = new();

        private void Awake()
        {
            _chessPieceManager = gameObject.GetComponent<ChessPieceManager>();
        }

        private void CheckMate(int team)
        {
            // DisplayVictory(team);
        }

        private void DisplayVictory(int winningTeam)
        {
            //victoryScreen.SetActive(true);
            //victoryScreen.transform.GetChild(winningTeam).gameObject
            //  .SetActive(true);
        }

// special moves
        private void ProcessSpecialMove()
        {
            if (_specialMove == SpecialMove.EnPassant)
            {
                var newMove = _moveList[_moveList.Count - 1];
                var myPawn = _chessPieces[newMove[1].x,
                    newMove[1].y];
                var targetPawnPosition = _moveList[_moveList.Count - 2];
                var enemyPawn = _chessPieces[targetPawnPosition[1].x,
                    targetPawnPosition[1].y];

                if (myPawn.currentX == enemyPawn.currentX)
                {
                    if (myPawn.currentY == enemyPawn.currentY - 1 ||
                        myPawn.currentY == enemyPawn.currentY + 1)
                    {
                        if (enemyPawn.team == 0)
                        {
                            _deadWithes.Add(enemyPawn);
                            enemyPawn.SetScale(Vector3.one * deathSize);
                            enemyPawn.SetPosition(
                                new Vector3(8 * boardStats.tileSize, boardStats.yOffset,
                                    -1 * boardStats.tileSize) -
                                boardStats.bounds +
                                new Vector3(boardStats.tileSize / 2, 0, boardStats.tileSize / 2) +
                                (Vector3.forward * deathSpacing) *
                                _deadWithes.Count);
                        }
                        else
                        {
                            _deadBlacks.Add(enemyPawn);
                            enemyPawn.SetScale(Vector3.one * deathSize);
                            enemyPawn.SetPosition(
                                new Vector3(8 * boardStats.tileSize, boardStats.yOffset,
                                    -1 * boardStats.tileSize) -
                                boardStats.bounds +
                                new Vector3(boardStats.tileSize / 2, 0, boardStats.tileSize / 2) +
                                (Vector3.forward * deathSpacing) *
                                _deadBlacks.Count);
                        }

                        _chessPieces[enemyPawn.currentX, enemyPawn.currentY] =
                            null;
                    }
                }
            }

            if (_specialMove == SpecialMove.Promotion)
            {
                var lastMove = _moveList[_moveList.Count - 1];
                var targetPawn = _chessPieces[lastMove[1].x, lastMove[1].y];
                if (targetPawn.type == ChessPieceType.Pawn)
                {
                    // White team
                    if (targetPawn.team == 0 && lastMove[1].y == 7)
                    {
                        var newQueen =
                            _chessPieceManager.SpawnSinglePiece(
                                ChessPieceType.Queen, 0);
                        Destroy(_chessPieces[lastMove[1].x, lastMove[1].y]
                            .gameObject);
                        _chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                        _chessPieceManager.PositionSinglePiece(lastMove[1].x,
                            lastMove[1].y, true);
                    }

                    // Black team
                    if (targetPawn.team == 1 && lastMove[1].y == 0)
                    {
                        var newQueen =
                            _chessPieceManager.SpawnSinglePiece(
                                ChessPieceType.Queen, 1);
                        Destroy(_chessPieces[lastMove[1].x, lastMove[1].y]
                            .gameObject);
                        _chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                        _chessPieceManager.PositionSinglePiece(lastMove[1].x,
                            lastMove[1].y, true);
                    }
                }
            }

            if (_specialMove == SpecialMove.Castling)
            {
                var lastMove = _moveList[_moveList.Count - 1];
                // Left rook
                if (lastMove[1].x == 2)
                {
                    if (lastMove[1].y == 0) // White side
                    {
                        var rook = _chessPieces[0, 0];
                        _chessPieces[3, 0] = rook;
                        _chessPieceManager.PositionSinglePiece(3, 0);
                        _chessPieces[0, 0] = null;
                    }
                    else if (lastMove[1].y == 7) // Black side
                    {
                        var rook = _chessPieces[0, 7];
                        _chessPieces[3, 7] = rook;
                        _chessPieceManager.PositionSinglePiece(3, 7);
                        _chessPieces[0, 7] = null;
                    }
                }
                // Right rook
                else if (lastMove[1].x == 6)
                {
                    if (lastMove[1].y == 0) // White side
                    {
                        var rook = _chessPieces[7, 0];
                        _chessPieces[5, 0] = rook;
                        _chessPieceManager.PositionSinglePiece(5, 0);
                        _chessPieces[7, 0] = null;
                    }
                    else if (lastMove[1].y == 7) // Black side
                    {
                        var rook = _chessPieces[7, 7];
                        _chessPieces[5, 7] = rook;
                        _chessPieceManager.PositionSinglePiece(5, 7);
                        _chessPieces[7, 7] = null;
                    }
                }
            }
        }

        // operations

        public void PreventCheck(ChessPiece currentlyDragging)
        {
            ChessPiece targetKing = null;
            for (int x = 0; x < BoardDimensions.TileCountX; x++)
            for (int y = 0; y < BoardDimensions.TileCountY; y++)
                if (_chessPieces[x, y] != null)
                    if (_chessPieces[x, y].type == ChessPieceType.King)
                        if (_chessPieces[x, y].team == currentlyDragging.team)
                            targetKing = _chessPieces[x, y];

            // Sicne we are sending ref _availableMoves, we will be deleting moves that are putting us in check
            SimulateMoveForSinglePiece(currentlyDragging, ref _availableMoves,
                targetKing);
        }

        private void SimulateMoveForSinglePiece(ChessPiece cp,
            ref List<Vector2Int> moves, ChessPiece targetKing)
        {
            // Save current values, to rest after the function call
            var actualX = cp.currentX;
            var actualY = cp.currentY;
            var movesToRemove = new List<Vector2Int>();

            // Going through all the moves. simulate them and check if we are in check
            for (int i = 0; i < moves.Count; i++)
            {
                var simX = moves[i].x;
                var simY = moves[i].y;

                var kingPositionThisSim =
                    new Vector2Int(targetKing.currentX, targetKing.currentY);
                // Did we simulate the king's move
                if (cp.type == ChessPieceType.King)
                    kingPositionThisSim = new Vector2Int(simX, simY);

                // Copy the [,] and not the reference
                var simulation = new ChessPiece[BoardDimensions.TileCountX,
                    BoardDimensions.TileCountY];
                var simulationAttackingPieces = new List<ChessPiece>();
                for (var x = 0; x < BoardDimensions.TileCountX; x++)
                {
                    for (var y = 0; y < BoardDimensions.TileCountY; y++)
                    {
                        if (_chessPieces[x, y] != null)
                        {
                            simulation[x, y] = _chessPieces[x, y];
                            if (simulation[x, y].team != cp.team)
                                simulationAttackingPieces.Add(simulation[x, y]);
                        }
                    }
                }

                // Simulate that move
                simulation[actualX, actualY] = null;
                cp.currentX = simX;
                cp.currentY = simY;
                simulation[simX, simY] = cp;

                // Did one of the piece got taken down during our simulation
                var deadPiece = simulationAttackingPieces.Find(c =>
                    c.currentY == simX && c.currentY == simY);
                if (deadPiece != null)
                    simulationAttackingPieces.Remove(deadPiece);

                // Get all the simulated attacking pieces moves
                var simMoves = new List<Vector2Int>();
                for (var a = 0; a < simulationAttackingPieces.Count; a++)
                {
                    var pieceMoves = simulationAttackingPieces[a]
                        .GetAvailableMoves(ref simulation,
                            BoardDimensions.TileCountX,
                            BoardDimensions.TileCountY);
                    for (int b = 0; b < pieceMoves.Count; b++)
                        simMoves.Add(pieceMoves[b]);
                }

                // Is the king in trouble? if so, remove the move
                if (ContainsValidMove(ref simMoves, kingPositionThisSim))
                {
                    movesToRemove.Add(moves[i]);
                }

                // Restore the actual CP data
                cp.currentX = actualX;
                cp.currentY = actualY;
            }

            // Remove from the current available moves list
            for (int i = 0; i < movesToRemove.Count; i++)
                moves.Remove(movesToRemove[i]);
        }

        private bool CheckForCheckmate()
        {
            var lastMove = _moveList[_moveList.Count - 1];
            var targetTeam =
                (_chessPieces[lastMove[1].x, lastMove[1].y].team == 0)
                    ? 1
                    : 0;

            var attackingPieces = new List<ChessPiece>();
            var defendingPieces = new List<ChessPiece>();
            ChessPiece targetKing = null;
            for (int x = 0; x < BoardDimensions.TileCountX; x++)
            for (int y = 0; y < BoardDimensions.TileCountY; y++)
                if (_chessPieces[x, y] != null)
                {
                    if (_chessPieces[x, y].team == targetTeam)
                    {
                        defendingPieces.Add(_chessPieces[x, y]);
                        if (_chessPieces[x, y].type == ChessPieceType.King)
                            targetKing = _chessPieces[x, y];
                    }
                    else
                    {
                        attackingPieces.Add(_chessPieces[x, y]);
                    }
                }

            // Is the king attacked right now?
            var currentAvailableMoves = new List<Vector2Int>();
            for (int i = 0; i < attackingPieces.Count; i++)
            {
                var pieceMoves = attackingPieces[i]
                    .GetAvailableMoves(ref _chessPieces,
                        BoardDimensions.TileCountX,
                        BoardDimensions.TileCountY);
                for (int b = 0; b < pieceMoves.Count; b++)
                    currentAvailableMoves.Add(pieceMoves[b]);
            }

            // Are we in check right now
            if (ContainsValidMove(ref currentAvailableMoves,
                    new Vector2Int(targetKing.currentX, targetKing.currentY)))
            {
                // King is under attack, can we move something to help him?
                for (int i = 0; i < defendingPieces.Count; i++)
                {
                    var defendingMoves = defendingPieces[i]
                        .GetAvailableMoves(ref _chessPieces,
                            BoardDimensions.TileCountX,
                            BoardDimensions.TileCountY);
                    SimulateMoveForSinglePiece(defendingPieces[i],
                        ref defendingMoves, targetKing);

                    if (defendingPieces.Count != 0)
                        return false;
                }

                return true; // Checkmate exit
            }

            return false;
        }

        public bool MoveTo(ChessPiece cp, int x, int y)
        {
            if (!ContainsValidMove(ref _availableMoves, new Vector2Int(x, y)))
                return false;

            var previousPosition = new Vector2Int(cp.currentX, cp.currentY);

            // Is there another piece on the target position?
            if (_chessPieces[x, y] != null)
            {
                var ocp = _chessPieces[x, y];
                if (cp.team == ocp.team)
                    return false;

                // If it is the enemy team
                if (ocp.team == 0)
                {
                    if (ocp.type == ChessPieceType.King)
                        CheckMate(1);

                    _deadWithes.Add(ocp);
                    ocp.SetScale(Vector3.one * deathSize);
                    ocp.SetPosition(
                        new Vector3(8 * boardStats.tileSize, boardStats.yOffset, -1 * boardStats.tileSize) -
                        boardStats.bounds + new Vector3(boardStats.tileSize / 2, 0, boardStats.tileSize / 2) +
                        (Vector3.forward * deathSpacing) * _deadWithes.Count);
                }
                else
                {
                    if (ocp.type == ChessPieceType.King)
                        CheckMate(0);

                    _deadBlacks.Add(ocp);
                    ocp.SetScale(Vector3.one * deathSize);
                    ocp.SetPosition(
                        new Vector3(-1 * boardStats.tileSize, boardStats.yOffset, 8 *boardStats. tileSize) -
                        boardStats.bounds + new Vector3(boardStats.tileSize / 2, 0, boardStats.tileSize / 2) +
                        (Vector3.back * deathSpacing) * _deadBlacks.Count);
                }
            }

            _chessPieces[x, y] = cp;
            _chessPieces[previousPosition.x, previousPosition.y] = null;
            _chessPieceManager.PositionSinglePiece(x, y);

            boardStats.isWhiteTurn = !boardStats.isWhiteTurn;
            _moveList.Add(new[]
            {
                previousPosition, new Vector2Int(x, y)
            });

            ProcessSpecialMove();
            if (CheckForCheckmate())
                CheckMate(cp.team);

            return true;
        }

        public bool ContainsValidMove(ref List<Vector2Int> moves,
            Vector2Int pos)
        {
            for (int i = 0; i < moves.Count; i++)
                if (moves[i].x == pos.x && moves[i].y == pos.y)
                    return true;

            return false;
        }
    }
}