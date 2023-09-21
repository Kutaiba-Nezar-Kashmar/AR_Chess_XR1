using System;
using ChessLogic.Enums;
using Models;
using UnityEngine;

namespace ChessLogic.Pieces
{
    public class ChessPieceManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] prefabs;
        [SerializeField] private Material[] materials;
        [SerializeField] private BoardStats boardStats;
        private ChessPiece[,] _chessPieces;

        private const int TileCountX = 8;
        private const int TileCountY = 8;


        public void SpawnAllPieces()
        {
            _chessPieces = new ChessPiece[TileCountX, TileCountY];

            var whiteTeam = 0;
            var blackTeam = 1;

            // White team
            _chessPieces[0, 0] =
                SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
            _chessPieces[1, 0] =
                SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
            _chessPieces[2, 0] =
                SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
            _chessPieces[3, 0] =
                SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
            _chessPieces[4, 0] =
                SpawnSinglePiece(ChessPieceType.King, whiteTeam);
            _chessPieces[5, 0] =
                SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
            _chessPieces[6, 0] =
                SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
            _chessPieces[7, 0] =
                SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
            for (var i = 0; i < TileCountX; i++)
                _chessPieces[i, 1] =
                    SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);

            // Black team
            _chessPieces[0, 7] =
                SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
            _chessPieces[1, 7] =
                SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
            _chessPieces[2, 7] =
                SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
            _chessPieces[3, 7] =
                SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
            _chessPieces[4, 7] =
                SpawnSinglePiece(ChessPieceType.King, blackTeam);
            _chessPieces[5, 7] =
                SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
            _chessPieces[6, 7] =
                SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
            _chessPieces[7, 7] =
                SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
            for (var i = 0; i < TileCountX; i++)
                _chessPieces[i, 6] =
                    SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }

        public ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
        {
            // Type -1 because we have non at index 0 in the enum, so to be in sync with array we use - 1
            Debug.Log($"Number of prefabs are: {prefabs.Length}");
            var cp = Instantiate(prefabs[(int) type - 1], transform)
                .GetComponent<ChessPiece>();
            Debug.Log($"CP holds: {cp.type}");
            cp.type = type;
            cp.team = team;
            cp.GetComponent<MeshRenderer>().material = materials[team];
            return cp;
        }

        public void PositionAllPieces()
        {
            for (int x = 0; x < TileCountX; x++)
            for (int y = 0; y < TileCountY; y++)
                if (_chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
        }

        public void PositionSinglePiece
        (
            int x,
            int y,
            bool force = false
        )
        {
            _chessPieces[x, y].currentX = x;
            _chessPieces[x, y].currentY = y;
            _chessPieces[x, y]
                .SetPosition
                    (boardStats.tileCenter, force);
        }
    }
}