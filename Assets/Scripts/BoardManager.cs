using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public int Width;
    public int Height;
    public Tile[] GrounPatterns;            // 背景タイルテクスチャパターン
    public GameObject[] PiecesPatterns;     // ピースパターン(prefab)

    private GameObject[,] m_Cell;           // セル(ピース)データの管理
    private Tilemap m_Tilemap;
    private Grid m_Grid;

    private Vector2 startPos;
    private Vector2 endPos;
    private GameObject selectedPiece;

    private List<(GameObject piece, Vector2Int from, Vector2Int to)> fallingPieces = new List<(GameObject, Vector2Int, Vector2Int)>();
    private List<(GameObject piece, Vector2Int from, Vector2Int to)> createPieces = new List<(GameObject, Vector2Int, Vector2Int)>();

    // transform.position <=> m_cellインデックス変換
    public Vector2Int ConvertFromTransformToCell(Vector3 transformposition)
    {
        Vector3Int cellPosition = m_Grid.WorldToCell(transformposition);
        return new Vector2Int(cellPosition.x, cellPosition.y);
    }
    public Vector3 ConvertFromCellToTransform(Vector2Int cellindex)
    {
        return m_Grid.GetCellCenterWorld(new Vector3Int(cellindex.x, cellindex.y, 0));
    }

    private void SwapPieces(GameObject piece, Vector2 swipeDirection)
    {
        float swipeThreshold = 0.5f;
        Vector2Int direction = Vector2Int.zero;

        if (Mathf.Abs(swipeDirection.x) > swipeThreshold || Mathf.Abs(swipeDirection.y) > swipeThreshold)
        {
            direction = (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y)) ?
                (swipeDirection.x > 0 ? Vector2Int.right : Vector2Int.left) :
                (swipeDirection.y > 0 ? Vector2Int.up : Vector2Int.down);

            Vector2 targetPosition = (Vector2)piece.transform.position + (Vector2)direction;
            RaycastHit2D hit = Physics2D.Raycast(targetPosition, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject otherPiece = hit.collider.gameObject;
                SwapPosition(piece, otherPiece);
            }
        }
    }

    private void SwapPosition(GameObject piece1, GameObject piece2)
    {
        Vector3 pos1 = piece1.transform.position;
        Vector3 pos2 = piece2.transform.position;

        // 内部データ(m_Cell)を更新
        SwapCellData(pos1, pos2);

        // ピースの移動アニメーションを開始
        piece1.GetComponent<PieceView>().MoveTo(pos2);
        piece2.GetComponent<PieceView>().MoveTo(pos1);

        StartCoroutine(PieceMatchCheckUpdate());
    }

    private void AddPieceToGrid(int x, int y)
    {
        int pieceIndex = UnityEngine.Random.Range(0, PiecesPatterns.Length);
        GameObject cell = Instantiate(PiecesPatterns[pieceIndex], m_Grid.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
        cell.AddComponent<PieceView>(); // PieceView を自動でアタッチ
        m_Cell[x, y] = cell;
    }

    private void SwapCellData(Vector3 pos1, Vector3 pos2)
    {
        Vector2Int index1 = ConvertFromTransformToCell(pos1);
        Vector2Int index2 = ConvertFromTransformToCell(pos2);

        GameObject temp = m_Cell[index1.x, index1.y];
        m_Cell[index1.x, index1.y] = m_Cell[index2.x, index2.y];
        m_Cell[index2.x, index2.y] = temp;
    }

    //  タップ / クリックしたピースを取得
    private void HandlePieceSelection()
    {
        Vector2 inputPos = Touchscreen.current?.primaryTouch.position.ReadValue() ?? Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPos);
        worldPos.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null)
        {
            GameObject piece = hit.collider.gameObject;
            Debug.Log($"選択されたピース: {piece.name}");

            if (selectedPiece == null)
            {
                selectedPiece = piece; // 1つ目のピース選択
            }
            else
            {
                Vector2 swipeDirection = piece.transform.position - selectedPiece.transform.position;
                SwapPieces(selectedPiece, swipeDirection);
                selectedPiece = null; // 選択解除
            }
        }
    }

    // 横・縦方向にピースをチェック
    public List<GameObject> PieceMatchCheck()
    {
        HashSet<GameObject> matchedPieces = new HashSet<GameObject>(); // 重複を防ぐため HashSet を使用

        // 横方向のチェック
        for (int y = 0; y < Height; y++)
        {
            int matchCount = 1; // 連続しているピースの数
            List<GameObject> tempMatches = new List<GameObject>();
            tempMatches.Add(m_Cell[0, y]);

            for (int x = 1; x < Width; x++)
            {
                if (m_Cell[x, y] != null && m_Cell[x - 1, y] != null &&
                    m_Cell[x, y].tag == m_Cell[x - 1, y].tag)
                {
                    matchCount++;
                    tempMatches.Add(m_Cell[x, y]);
                }
                else
                {
                    if (matchCount >= 3) // 3つ以上並んでいたら追加
                    {
                        foreach (var piece in tempMatches)
                            matchedPieces.Add(piece);
                    }
                    matchCount = 1;
                    tempMatches.Clear();
                    tempMatches.Add(m_Cell[x, y]);
                }
            }

            if (matchCount >= 3) // ループ終了時もチェック
            {
                foreach (var piece in tempMatches)
                    matchedPieces.Add(piece);
            }
        }

        // 縦方向のチェック
        for (int x = 0; x < Width; x++)
        {
            int matchCount = 1;
            List<GameObject> tempMatches = new List<GameObject>();
            tempMatches.Add(m_Cell[x, 0]);

            for (int y = 1; y < Height; y++)
            {
                if (m_Cell[x, y] != null && m_Cell[x, y - 1] != null &&
                    m_Cell[x, y].tag == m_Cell[x, y - 1].tag)
                {
                    matchCount++;
                    tempMatches.Add(m_Cell[x, y]);
                }
                else
                {
                    if (matchCount >= 3)
                    {
                        foreach (var piece in tempMatches)
                            matchedPieces.Add(piece);
                    }
                    matchCount = 1;
                    tempMatches.Clear();
                    tempMatches.Add(m_Cell[x, y]);
                }
            }

            if (matchCount >= 3)
            {
                foreach (var piece in tempMatches)
                    matchedPieces.Add(piece);
            }
        }

        return new List<GameObject>(matchedPieces);
    }


    public IEnumerator PieceMatchCheckUpdate()
    {
        List<GameObject> matchedPieces;
        do
        {
            matchedPieces = PieceMatchCheck();
            Debug.Log("MatchPiece Start: " + matchedPieces.Count);

            // マッチしたピースを削除し、m_Cellを更新
            foreach (var piece in matchedPieces)
            {
                Vector2Int cellPos = ConvertFromTransformToCell(piece.transform.position);
                m_Cell[cellPos.x, cellPos.y] = null; // データを削除

                // ピースのアニメーションを開始
                piece.GetComponent<PieceView>().PlayDestroyAnimation(() =>
                {
                    Destroy(piece);
                });
            }

            // 落下処理を待つ
            yield return StartCoroutine(updateCell());

        } while (matchedPieces.Count > 0);

        Debug.Log("PieceMatchCheckUpdate() END");
    }

    private IEnumerator updateCell()
    {
        fallingPieces.Clear();
        createPieces.Clear();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (m_Cell[x, y] == null) // 空のセルがある場合
                {
                    for (int aboveY = y + 1; aboveY < Height; aboveY++)
                    {
                        if (m_Cell[x, aboveY] != null)
                        {
                            fallingPieces.Add((m_Cell[x, aboveY], new Vector2Int(x, aboveY), new Vector2Int(x, y)));
                            m_Cell[x, y] = m_Cell[x, aboveY];
                            m_Cell[x, aboveY] = null;
                            break;
                        }
                    }
                }
            }

            // 新しいピースを補充
            int numAddPiece = 0;
            for (int y = 0; y < Height; y++)
            {
                if (m_Cell[x, y] == null)
                {
                    AddPieceToGrid(x, y);
                    createPieces.Add((m_Cell[x, y], new Vector2Int(x, Height + numAddPiece), new Vector2Int(x, y)));
                    m_Cell[x, y].GetComponent<Renderer>().enabled = false;
                    numAddPiece++;
                }
            }
        }

        // すべての落下処理をアニメーションで実行
        yield return StartCoroutine(AnimateFallingPieces());
        Debug.Log("update cell function end.");
    }

    private IEnumerator AnimateFallingPieces()
    {
        List<Coroutine> fallCoroutines = new List<Coroutine>();

        foreach (var (piece, from, to) in fallingPieces)
        {
            // ピースの移動アニメーションを開始
            Coroutine fallCoroutine = StartCoroutine(piece.GetComponent<PieceView>().MoveToWithCallback(ConvertFromCellToTransform(to)));
            fallCoroutines.Add(fallCoroutine);
        }

        // すべての落下アニメーションが終わるのを待つ
        foreach (var coroutine in fallCoroutines)
        {
            yield return coroutine;
        }

        Debug.Log("AnimateFallingPieces 完了！");
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // グリッド (m_Cell) の初期化
    // 背景 (Tilemap) の生成
    // ピース (AddPieceToGrid()) の配置
    void Start()
    {
        m_Tilemap = GetComponentInChildren<Tilemap>();
        m_Grid = GetComponentInChildren<Grid>();
        m_Cell = new GameObject[Width, Height];

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                //　背景タイルを生成
                int groundIndex = UnityEngine.Random.Range(0, GrounPatterns.Length);
                m_Tilemap.SetTile(new Vector3Int(x, y, 0), GrounPatterns[groundIndex]);

                // ピースを生成し登録
                AddPieceToGrid(x, y);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // スマホのタップ判定（null の場合は false）
        bool isTouch = (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false);

        // マウスのクリック判定（null の場合は false）
        bool isClick = (Mouse.current?.leftButton.wasPressedThisFrame ?? false);

        if (isTouch || isClick)
        {
            HandlePieceSelection();
        }
    }
}
