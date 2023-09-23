using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}

public class Chessboard : MonoBehaviour
{
    void HandleFingerTap(Lean.Touch.LeanFinger finger)
    {
        Debug.Log("You just tapped the screen with finger " + finger.Index + " at " + finger.ScreenPosition);
    }

    private void OnEnable()
    {
        Lean.Touch.LeanTouch.OnFingerTap += HandleFingerTap;
    }

    private void OnDisable()
    {
        Lean.Touch.LeanTouch.OnFingerTap -= HandleFingerTap;
    }

    [Header("Art")] [SerializeField] private Material tileMaterial;

    [SerializeField] private float tileSize = 1.0f;

    // Used to keep track of the height of the board 
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter;

    [Header("Prefabs & Materials")] [SerializeField]
    private GameObject[] prefabs;

    [SerializeField] private Material[] materials;
    [SerializeField] private float deathSize = 1.5f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject victoryScreen;
    ARRaycastManager m_RaycastManager;

    // Logic
    private List<Vector2Int> _availableMoves = new List<Vector2Int>();
    private ChessPiece[,] _chessPieces;
    private ChessPiece _currentlyDragging;
    private List<ChessPiece> _deadWithes = new List<ChessPiece>();
    private List<ChessPiece> _deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;

    private const int TILE_COUNT_Y = 8;

    // The [,] indicates that it is two dimensional array
    private GameObject[,] _tiles;
    private Camera _currentCamera;
    private Vector2Int _currentHover;
    private Vector3 _bounds;
    private bool _isWhiteTurn;
    private SpecialMove _specialMove;
    private List<Vector2Int[]> _moveList = new List<Vector2Int[]>();


    public TextMeshPro statusText;

    private void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        _isWhiteTurn = true;

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            var touchPosition = touch.position;
            if (touch.phase is TouchPhase.Began)
            {
                RaycastHit info;
                if (Camera.main == null)
                {
                    return;
                }

                var ray = Camera.main.ScreenPointToRay(touchPosition);
            //    statusText.text = "position: " + touch.position.ToString();

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
                            (ContainsValidMove(ref _availableMoves, _currentHover))
                                ? LayerMask.NameToLayer("Highlight")
                                : LayerMask.NameToLayer("Tile");
                        _currentHover = hitPosition;
                        _tiles[hitPosition.x, hitPosition.y].layer =
                            LayerMask.NameToLayer("Hover");
                    }

                    // If we press down on the mouse
                    if (touch.phase == TouchPhase.Began)
                    {
                        if (_chessPieces[hitPosition.x, hitPosition.y] != null)
                        {
                            // Is our turn
                            if ((_chessPieces[hitPosition.x, hitPosition.y].team == 0 &&
                                 _isWhiteTurn) ||
                                (_chessPieces[hitPosition.x, hitPosition.y].team == 1 &&
                                 !_isWhiteTurn))
                            {
                                _currentlyDragging =
                                    _chessPieces[hitPosition.x, hitPosition.y];

                                // Get a list of where I can go, highlight as well
                                _availableMoves =
                                    _currentlyDragging.GetAvailableMoves(
                                        ref _chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                                // Get a list of special move
                                _specialMove =
                                    _currentlyDragging.GetSpecialMove(ref _chessPieces,
                                        ref _moveList, ref _availableMoves);

                                PreventCheck();
                                HighlightTiles();
                            }
                        }
                    }

                    // If we releasing mouse button
                    if (_currentlyDragging != null && touch.phase == TouchPhase.Ended)
                    {
                        var previousPosition = new Vector2Int(
                            _currentlyDragging.currentX, _currentlyDragging.currentY);
                        var validMove = MoveTo(_currentlyDragging, hitPosition.x,
                            hitPosition.y);
                        if (!validMove)
                            _currentlyDragging.SetPosition(
                                GetTileCenter(previousPosition.x, previousPosition.y));

                        _currentlyDragging = null;
                        RemoveHighlightTiles();
                    }
                }
                else
                {
                    if (_currentHover != -Vector2Int.one)
                    {
                        _tiles[_currentHover.x, _currentHover.y].layer =
                            (ContainsValidMove(ref _availableMoves, _currentHover))
                                ? LayerMask.NameToLayer("Highlight")
                                : LayerMask.NameToLayer("Tile");

                        _currentHover = -Vector2Int.one;
                    }

                    if (_currentlyDragging && touch.phase == TouchPhase.Ended)
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
                    var horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                    var distance = 0.0f;
                    if (horizontalPlane.Raycast(ray, out distance))
                        _currentlyDragging.SetPosition(ray.GetPoint(distance) +
                                                       Vector3.up * dragOffset);
                }
            }
        }
    }

    // Generate board
    private void GenerateAllTiles(float tileSize, int tileContX, int tileCountY)
    {
        yOffset += transform.position.y;
        _bounds = new Vector3((tileContX / 2) * this.tileSize, 0,
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
            new Vector3(x * tileSize, yOffset, y * tileSize) - _bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) -
                      _bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) -
                      _bounds;
        vertices[3] =
            new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) -
            _bounds;

        var tris = new int[] {0, 1, 2, 1, 3, 2};

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // Spawn pieces
    private void SpawnAllPieces()
    {
        _chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        var whiteTeam = 0;
        var blackTeam = 1;

        // White team
        _chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        _chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        _chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        _chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        _chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        _chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        _chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        _chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            _chessPieces[i, 1] =
                SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);

        // Black team
        _chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        _chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        _chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        _chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        _chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        _chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        _chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        _chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            _chessPieces[i, 6] =
                SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        // Type -1 because we have non at index 0 in the enum, so to be in sync with array we use - 1
        var cp = Instantiate(prefabs[(int) type - 1], transform)
            .GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = materials[team];
        return cp;
    }

    // Positioning
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        for (int y = 0; y < TILE_COUNT_Y; y++)
            if (_chessPieces[x, y] != null)
                PositionSinglePiece(x, y, true);
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        _chessPieces[x, y].currentX = x;
        _chessPieces[x, y].currentY = y;
        _chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - _bounds +
               new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    // Checkmate
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }

    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject
            .SetActive(true);
    }

    public void OnResetButton()
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
    }

    public void OnExitButton()
    {
        Application.Quit();
    }

    // Special moves
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
                            new Vector3(8 * tileSize, yOffset, -1 * tileSize) -
                            _bounds +
                            new Vector3(tileSize / 2, 0, tileSize / 2) +
                            (Vector3.forward * deathSpacing) *
                            _deadWithes.Count);
                    }
                    else
                    {
                        _deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(
                            new Vector3(8 * tileSize, yOffset, -1 * tileSize) -
                            _bounds +
                            new Vector3(tileSize / 2, 0, tileSize / 2) +
                            (Vector3.forward * deathSpacing) *
                            _deadBlacks.Count);
                    }

                    _chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
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
                    var newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    Destroy(_chessPieces[lastMove[1].x, lastMove[1].y]
                        .gameObject);
                    _chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y, true);
                }

                // Black team
                if (targetPawn.team == 1 && lastMove[1].y == 0)
                {
                    var newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    Destroy(_chessPieces[lastMove[1].x, lastMove[1].y]
                        .gameObject);
                    _chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y, true);
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
                    PositionSinglePiece(3, 0);
                    _chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) // Black side
                {
                    var rook = _chessPieces[0, 7];
                    _chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
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
                    PositionSinglePiece(5, 0);
                    _chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) // Black side
                {
                    var rook = _chessPieces[7, 7];
                    _chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    _chessPieces[7, 7] = null;
                }
            }
        }
    }

    private void PreventCheck()
    {
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
        for (int y = 0; y < TILE_COUNT_Y; y++)
            if (_chessPieces[x, y] != null)
                if (_chessPieces[x, y].type == ChessPieceType.King)
                    if (_chessPieces[x, y].team == _currentlyDragging.team)
                        targetKing = _chessPieces[x, y];

        // Sicne we are sending ref _availableMoves, we will be deleting moves that are putting us in check
        SimulateMoveForSinglePiece(_currentlyDragging, ref _availableMoves,
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
            var simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            var simulationAttackingPieces = new List<ChessPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
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
            for (int a = 0; a < simulationAttackingPieces.Count; a++)
            {
                var pieceMoves = simulationAttackingPieces[a]
                    .GetAvailableMoves(ref simulation, TILE_COUNT_X,
                        TILE_COUNT_Y);
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
        var targetTeam = (_chessPieces[lastMove[1].x, lastMove[1].y].team == 0)
            ? 1
            : 0;

        var attackingPieces = new List<ChessPiece>();
        var defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
        for (int y = 0; y < TILE_COUNT_Y; y++)
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
                .GetAvailableMoves(ref _chessPieces, TILE_COUNT_X,
                    TILE_COUNT_Y);
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
                var defendingMoves = defendingPieces[i].GetAvailableMoves(ref _chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveForSinglePiece(defendingPieces[i],
                    ref defendingMoves, targetKing);

                if (defendingPieces.Count != 0)
                    return false;
            }

            return true; // Checkmate exit
        }

        return false;
    }

    // Operations
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        for (int y = 0; y < TILE_COUNT_Y; y++)
            if (_tiles[x, y] == hitInfo)
                return new Vector2Int(x, y);
        return -Vector2Int.one; // Invalid
    }

    private bool MoveTo(ChessPiece cp, int x, int y)
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
                    new Vector3(8 * tileSize, yOffset, -1 * tileSize) -
                    _bounds + new Vector3(tileSize / 2, 0, tileSize / 2) +
                    (Vector3.forward * deathSpacing) * _deadWithes.Count);
            }
            else
            {
                if (ocp.type == ChessPieceType.King)
                    CheckMate(0);

                _deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                    new Vector3(-1 * tileSize, yOffset, 8 * tileSize) -
                    _bounds + new Vector3(tileSize / 2, 0, tileSize / 2) +
                    (Vector3.back * deathSpacing) * _deadBlacks.Count);
            }
        }

        _chessPieces[x, y] = cp;
        _chessPieces[previousPosition.x, previousPosition.y] = null;
        PositionSinglePiece(x, y);

        _isWhiteTurn = !_isWhiteTurn;
        _moveList.Add(new Vector2Int[]
        {
            previousPosition, new Vector2Int(x, y)
        });

        ProcessSpecialMove();
        if (CheckForCheckmate())
            CheckMate(cp.team);

        return true;
    }

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    }

    // Highlight tiles
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