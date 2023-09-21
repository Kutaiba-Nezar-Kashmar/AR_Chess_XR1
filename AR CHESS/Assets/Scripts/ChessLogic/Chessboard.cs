using System.Collections.Generic;
using ChessLogic.Enums;
using ChessLogic.Pieces;
using Constants;
using Models;
using UnityEngine;

namespace ChessLogic
{
    public class Chessboard : MonoBehaviour
    {
        [SerializeField] private BoardStats boardStats;
        [SerializeField] private Material tileMaterial;
        [SerializeField] private Vector3 boardCenter;
        [SerializeField] private float dragOffset = 1.5f;

        private GameManager _gameManager;
        private ChessPieceManager _chessPieceManager;
        private List<Vector2Int> _availableMoves = new();
        private ChessPiece[,] _chessPieces;
        private ChessPiece _currentlyDragging;
        private GameObject[,] _tiles;
        private Camera _currentCamera;
        private Vector2Int _currentHover;
        private List<Vector2Int[]> _moveList = new();

        private void Awake()
        {
            _chessPieceManager = gameObject.GetComponent<ChessPieceManager>();
            _gameManager = gameObject.GetComponent<GameManager>();
            boardStats.isWhiteTurn = true;

            GenerateAllTiles(boardStats.tileSize, BoardDimensions.TileCountX,
                BoardDimensions.TileCountY);
            _chessPieceManager.SpawnAllPieces();
            _chessPieceManager.PositionAllPieces();
        }

        private void Update()
        {
            var specialMove = SpecialMove.None;
            if (!_currentCamera)
            {
                _currentCamera = Camera.main;
                return;
            }

            RaycastHit info;
            var ray = _currentCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out info, 100,
                    LayerMask.GetMask("Tile", "Hover", "Highlight")))
            {
                // Get the index of the tiles I've hit
                var hitPosition = LookupTileIndex(info.transform.gameObject);

                // If we're hovering a tile agter not hovering any tiles
                if (_currentHover == -Vector2Int.one)
                {
                    _currentHover = hitPosition;
                    _tiles[hitPosition.x, hitPosition.y].layer =
                        LayerMask.NameToLayer("Hover");
                }

                // If we were already hovering a tile, change the previous one
                if (_currentHover != hitPosition)
                {
                    _tiles[_currentHover.x, _currentHover.y].layer =
                        (_gameManager.ContainsValidMove(ref _availableMoves,
                            _currentHover))
                            ? LayerMask.NameToLayer("Highlight")
                            : LayerMask.NameToLayer("Tile");
                    _currentHover = hitPosition;
                    _tiles[hitPosition.x, hitPosition.y].layer =
                        LayerMask.NameToLayer("Hover");
                }

                // If we press down on the mouse
                if (Input.GetMouseButtonDown(0))
                {
                    if (_chessPieces[hitPosition.x, hitPosition.y] != null)
                    {
                        // Is our turn
                        if ((_chessPieces[hitPosition.x, hitPosition.y].team ==
                             0 &&
                             boardStats.isWhiteTurn) ||
                            (_chessPieces[hitPosition.x, hitPosition.y].team ==
                             1 &&
                             !boardStats.isWhiteTurn))
                        {
                            _currentlyDragging =
                                _chessPieces[hitPosition.x, hitPosition.y];

                            // Get a list of where I can go, highlight as well
                            _availableMoves =
                                _currentlyDragging.GetAvailableMoves(
                                    ref _chessPieces,
                                    BoardDimensions.TileCountX,
                                    BoardDimensions.TileCountY);
                            // Get a list of special move
                            specialMove =
                                _currentlyDragging.GetSpecialMove(
                                    ref _chessPieces,
                                    ref _moveList, ref _availableMoves);

                            _gameManager.PreventCheck(_currentlyDragging);
                            HighlightTiles();
                        }
                    }
                }

                // If we releasing mouse button
                if (_currentlyDragging != null && Input.GetMouseButtonUp(0))
                {
                    var previousPosition = new Vector2Int(
                        _currentlyDragging.currentX,
                        _currentlyDragging.currentY);
                    var validMove = _gameManager.MoveTo(_currentlyDragging,
                        hitPosition.x,
                        hitPosition.y);
                    if (!validMove)
                        _currentlyDragging.SetPosition(
                            GetTileCenter(previousPosition.x,
                                previousPosition.y));

                    _currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }
            else
            {
                if (_currentHover != -Vector2Int.one)
                {
                    _tiles[_currentHover.x, _currentHover.y].layer =
                        (_gameManager.ContainsValidMove(ref _availableMoves,
                            _currentHover))
                            ? LayerMask.NameToLayer("Highlight")
                            : LayerMask.NameToLayer("Tile");

                    _currentHover = -Vector2Int.one;
                }

                if (_currentlyDragging && Input.GetMouseButtonUp(0))
                {
                    _currentlyDragging.SetPosition(GetTileCenter(
                        _currentlyDragging.currentX,
                        _currentlyDragging.currentY));
                    _currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }

            // If we are dragging a piece
            if (_currentlyDragging)
            {
                var horizontalPlane =
                    new Plane(Vector3.up, Vector3.up * boardStats.yOffset);
                var distance = 0.0f;
                if (horizontalPlane.Raycast(ray, out distance))
                    _currentlyDragging.SetPosition(ray.GetPoint(distance) +
                                                   Vector3.up * dragOffset);
            }
        }

        // Generate board
        private void GenerateAllTiles(float tileSize, int tileContX,
            int tileCountY)
        {
            boardStats.yOffset += transform.position.y;
            boardStats.bounds = new Vector3((tileContX / 2) * boardStats.tileSize, 0,
                (tileContX / 2) * tileSize) + boardCenter;

            _tiles = new GameObject[tileContX, tileCountY];
            for (int x = 0; x < tileContX; x++)
            for (int y = 0; y < tileCountY; y++)
                _tiles[x, y] = GenerateSingleTile(tileSize, x, y);
        }

        private GameObject GenerateSingleTile(float tileSize, int x, int y)
        {
            var tileObject = new GameObject($"X:{x}, Y:{y}");
            tileObject.transform.parent = transform;

            var mesh = new Mesh();
            tileObject.AddComponent<MeshFilter>().mesh = mesh;
            tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

            var vertices = new Vector3[4];
            // Order matters fpr the "tris"
            vertices[0] =
                new Vector3(x * tileSize, boardStats.yOffset, y * tileSize) -
                boardStats.bounds;
            vertices[1] =
                new Vector3(x * tileSize, boardStats.yOffset, (y + 1) * tileSize) -
                boardStats.bounds;
            vertices[2] =
                new Vector3((x + 1) * tileSize, boardStats.yOffset, y * tileSize) -
                boardStats.bounds;
            vertices[3] =
                new Vector3((x + 1) * tileSize, boardStats.yOffset, (y + 1) * tileSize) -
                boardStats.bounds;

            var tris = new int[] {0, 1, 2, 1, 3, 2};

            mesh.vertices = vertices;
            mesh.triangles = tris;

            mesh.RecalculateNormals();

            tileObject.layer = LayerMask.NameToLayer("Tile");
            tileObject.AddComponent<BoxCollider>();

            return tileObject;
        }

        /*public void OnResetButton()
        {
            // UI
            victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
            victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
            victoryScreen.SetActive(false);

            // Field reset
            _currentlyDragging = null;
            _availableMoves.Clear();
            _moveList.Clear();

            // Clean up
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (_chessPieces[x, y] != null)
                        Destroy(_chessPieces[x, y].gameObject);

                    _chessPieces[x, y] = null;
                }
            }

            for (int i = 0; i < _deadWithes.Count; i++)
                Destroy(_deadWithes[i].gameObject);
            for (int i = 0; i < _deadBlacks.Count; i++)
                Destroy(_deadBlacks[i].gameObject);

            _deadWithes.Clear();
            _deadBlacks.Clear();

            SpawnAllPieces();
            PositionAllPieces();
            _isWhiteTurn = true;
        }*/

        private Vector3 GetTileCenter
        (
            int x,
            int y
        )
        {
            return new Vector3(x * boardStats.tileSize, boardStats.yOffset, y * boardStats.tileSize) -
                   boardStats.bounds +
                   new Vector3(boardStats.tileSize / 2, 0, boardStats.tileSize / 2);
        }

        private Vector2Int LookupTileIndex(GameObject hitInfo)
        {
            for (int x = 0; x < BoardDimensions.TileCountX; x++)
            for (int y = 0; y < BoardDimensions.TileCountY; y++)
                if (_tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
            return -Vector2Int.one; // Invalid
        }

        private void HighlightTiles()
        {
            for (int i = 0; i < _availableMoves.Count; i++)
                _tiles[_availableMoves[i].x, _availableMoves[i].y].layer =
                    LayerMask.NameToLayer("Highlight");
        }

        private void RemoveHighlightTiles()
        {
            for (int i = 0; i < _availableMoves.Count; i++)
                _tiles[_availableMoves[i].x, _availableMoves[i].y].layer =
                    LayerMask.NameToLayer("Tile");
            _availableMoves.Clear();
        }
    }
}