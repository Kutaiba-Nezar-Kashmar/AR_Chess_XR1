using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}
public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 _desiredPosition;
    private Vector3 _desiredScale = new(2.5f,2.5f,2.5f);

    // This does not work for now because he is using different asset (not important and can fix later)
    /*private void Start()
    {
        transform.rotation = Quaternion.Euler((team == 0) ? Vector3.zero : new Vector3(0, 0, 180));
    }*/

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _desiredPosition,
            Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, _desiredScale,
            Time.deltaTime * 10);
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        _desiredPosition = position;
        if (force)
            transform.position = _desiredPosition;
    }

    public virtual SpecialMove GetSpecialMove(ref ChessPiece[,] board,
        ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        return SpecialMove.None;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        _desiredScale = scale;
        if (force)
            transform.localScale = _desiredScale;
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board,
        int tileCountX, int tileCountY)
    {
        var r = new List<Vector2Int>();
        r.Add(new Vector2Int(3,3));
        r.Add(new Vector2Int(3,4));
        r.Add(new Vector2Int(4,3));
        r.Add(new Vector2Int(4,4));
        return r;
    }
}
