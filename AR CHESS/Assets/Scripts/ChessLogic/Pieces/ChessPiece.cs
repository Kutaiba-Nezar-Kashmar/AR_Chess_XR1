using System.Collections.Generic;
using ChessLogic.Enums;
using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;
    private Vector3 _desiredPosition;
    [SerializeField] private Vector3 desiredScale = new(2.5f, 2.5f, 2.5f);


    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _desiredPosition,
            Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale,
            Time.deltaTime * 10);
    }

    public virtual void SetPosition
    (
        Vector3 position,
        bool force = false
    )
    {
        _desiredPosition = position;
        if (force)
            transform.position = _desiredPosition;
    }

    public virtual SpecialMove GetSpecialMove
    (
        ref ChessPiece[,] board,
        ref List<Vector2Int[]> moveList,
        ref List<Vector2Int> availableMoves
    )
    {
        return SpecialMove.None;
    }

    public virtual void SetScale
    (
        Vector3 scale,
        bool force = false
    )
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }

    public virtual List<Vector2Int> GetAvailableMoves
    (
        ref ChessPiece[,] board,
        int tileCountX,
        int tileCountY
    )
    {
        return new List<Vector2Int>();
    }
}