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

    private Tilemap m_Tilemap;              // 背景タイルの表示
    private Grid m_Grid;

    private Vector2 startPos;               //  スワイプ開始位置
    private Vector2 endPos;                 //  スワイプ終了位置
    private PieceController selectedPiece;  // スワイプ開始時に選択したピース

    // ピースが消えたことにより落下するピースの情報リスト
    private List<(GameObject piece, Vector2Int from, Vector2Int to)> fallingPieces = new List<(GameObject, Vector2Int, Vector2Int)>();
    //  新しく生成されたピースの情報リスト
    private List<(GameObject piece, Vector2Int from, Vector2Int to)> createPieces = new List<(GameObject, Vector2Int, Vector2Int)>();
    
    private void SwapPieces(PieceController piece, Vector2 swipeDirection)
    {
        float swipeThreshold = 0.5f; // スワイプの判定閾値
        Vector2Int direction = Vector2Int.zero;

        if (Mathf.Abs(swipeDirection.x) > swipeThreshold || Mathf.Abs(swipeDirection.y) > swipeThreshold)
        {
            // スワイプ方向を判定
            if (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y))
            {
                // 左右スワイプ
                direction = (swipeDirection.x > 0) ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                // 上下スワイプ
                direction = (swipeDirection.y > 0) ? Vector2Int.up : Vector2Int.down;
            }

            //Debug.Log($"Swap {piece.gameObject.name} to {direction}");
            piece.SwapWithNeighbor(direction); // PieceController にスワップ処理を実装
        }
    }
    
    // 落下アニメーションをコルーチンで実行
    private IEnumerator AnimateFallingPieces(int listId)
    {
        List<(GameObject piece, Vector2Int from, Vector2Int to)> animtaionPieceList = fallingPieces;
        if (listId == 1) {
            animtaionPieceList = createPieces;
        }

        int runningCoroutines = animtaionPieceList.Count;    //  落下するピースの数

        foreach (var (piece, from, to) in animtaionPieceList)
        {
            Vector3 startPos = ConvertFromCellToTransform(from);
            Vector3 endPos = ConvertFromCellToTransform(to);
            PieceController pieceController = piece.GetComponent<PieceController>();
            if( pieceController == null ){
                throw new Exception("PieceController is Null.");
            }

            // GameObjectを可視化(すでに可視設定になっているオブジェクトも再設定)
            pieceController.GetComponent<Renderer>().enabled = true;
            StartCoroutine(pieceController.SmoothMove(pieceController.transform, startPos, endPos, () =>
                {
                    runningCoroutines--;
                }));
        }

        // 全てのコルーチンが終わるのを待つ
        yield return new WaitUntil(() => runningCoroutines == 0);
    }    

    // セル更新（下方向に移動して補充）
    private IEnumerator updateCell()
    {
        fallingPieces.Clear();  // 落ちるピースのリストを初期化
        createPieces.Clear();   // 生成されるピースのリストを初期化

        for (int x = 0; x < Width; x++)
        {
            // 下から上に向かって確認
            for (int y = 0; y < Height; y++)
            {
                if (m_Cell[x, y] == null) // 空のスペースがある場合
                {
                    //Debug.Log("Space: " + x + "," + y);
                    // 現在より上部からピースを探して移動
                    for (int aboveY = y + 1; aboveY < Height; aboveY++)
                    {
                        if (m_Cell[x, aboveY] != null)
                        {
                            //Debug.Log("Bury: " + x + "," + aboveY + " => " + x + "," + y);

                            // ピース移動情報をリストに記録
                            fallingPieces.Add((m_Cell[x, aboveY], new Vector2Int(x, aboveY), new Vector2Int(x, y)));

                            // データだけ先に更新（表示は後でアニメーション）
                            m_Cell[x, y] = m_Cell[x, aboveY];
                            m_Cell[x, aboveY] = null;
                            break;
                        }
                    }
                }
            }

            // 新しいピースを補充チェック
            int numAddPiece = 0;
            for (int y = 0; y < Height; y++)
            {
                if (m_Cell[x, y] == null)
                {
                    //Debug.Log("Create: " + x + "," + y);

                    AddPieceToGrid(x, y);
                    // ピース補充情報をリストに記録 & 不可視化(アニメーションを開始する時に可視化)
                    createPieces.Add((m_Cell[x, y], new Vector2Int(x, Height + numAddPiece), new Vector2Int(x, y)));
                    m_Cell[x,y].GetComponent<Renderer>().enabled = false;
                    numAddPiece++;
                }
            }
        }

        // すべての落下処理をアニメーションで実行
        yield return StartCoroutine(AnimateFallingPieces(0));
        yield return StartCoroutine(AnimateFallingPieces(1));

    }

    // ピースを生成してグリッドに登録
    private void AddPieceToGrid(int x, int y)
    {
        int pieceIndex = UnityEngine.Random.Range(0, PiecesPatterns.Length);
        GameObject cell = Instantiate(PiecesPatterns[pieceIndex], m_Grid.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
        m_Cell[x, y] = cell;
    }

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

    // セルデータ(m_Cell)を入れ替える
    public void SwapCellData(Vector3 pos1, Vector3 pos2)
    {
        // 座標をグリッドのインデックスに変換
        Vector2Int index1 = ConvertFromTransformToCell(pos1);
        Vector2Int index2 = ConvertFromTransformToCell(pos2);

        // グリッドデータをスワップ
        GameObject temp = m_Cell[index1.x, index1.y];
        m_Cell[index1.x, index1.y] = m_Cell[index2.x, index2.y];
        m_Cell[index2.x, index2.y] = temp;
    }
    
    // 横・縦方向にピースをチェック
    public List<GameObject> PieceMatchCheck()
    {
        List<GameObject> matchedPieces = new List<GameObject>();

        // 横方向のチェック
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width - 2; x++)
            {
                if (m_Cell[x, y] != null &&
                    m_Cell[x + 1, y] != null &&
                    m_Cell[x + 2, y] != null)
                {
                    if (m_Cell[x, y].tag == m_Cell[x + 1, y].tag &&
                        m_Cell[x, y].tag == m_Cell[x + 2, y].tag)
                    {
                        matchedPieces.Add(m_Cell[x, y]);
                        matchedPieces.Add(m_Cell[x + 1, y]);
                        matchedPieces.Add(m_Cell[x + 2, y]);
                    }
                }
            }
        }

        // 縦方向のチェック
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height - 2; y++)
            {
                if (m_Cell[x, y] != null &&
                    m_Cell[x, y + 1] != null &&
                    m_Cell[x, y + 2] != null)
                {
                    if (m_Cell[x, y].tag == m_Cell[x, y + 1].tag &&
                        m_Cell[x, y].tag == m_Cell[x, y + 2].tag)
                    {
                        matchedPieces.Add(m_Cell[x, y]);
                        matchedPieces.Add(m_Cell[x, y + 1]);
                        matchedPieces.Add(m_Cell[x, y + 2]);
                    }
                }
            }
        }

        return matchedPieces;
    }

    // 横・縦方向にピースをチェックして、セル情報を更新(アニメーションこみ)
    public IEnumerator PieceMatchCheckUpdate()
    {
        List<GameObject> matchedPieces;
        do {
            matchedPieces = PieceMatchCheck();
            Debug.Log("MatchPiece Start: " + matchedPieces.Count );

            // マッチしたピースを削除して、セル情報を更新
            foreach (var piece in matchedPieces)
            {
                // 座標を取得
                Vector3 pos = piece.transform.position;
                int x = (int)Math.Floor(pos.x);
                int y = (int)Math.Floor(pos.y);

                // 先にm_CellをnullにしてからDestroy
                m_Cell[x, y] = null;
                //Debug.Log("Delete: " + x + "," + y);
                Destroy(piece);
            }

            yield return StartCoroutine(updateCell());
        } while (matchedPieces.Count > 0 );
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Tilemap = GetComponentInChildren<Tilemap>();
        m_Grid = GetComponentInChildren<Grid>();
        m_Cell = new GameObject[Width, Height];

        for (int y = 0; y < Height; ++y)
        {
            for(int x = 0; x < Width; ++x)
            {
                //　背景タイルを生成
                int groundIndex = UnityEngine.Random.Range(0, GrounPatterns.Length);
                m_Tilemap.SetTile(new Vector3Int(x, y, 0), GrounPatterns[groundIndex]);

                // ピースを生成し登録
                AddPieceToGrid(x, y);

                //Debug.Log("Groudtile: " + groundIndex + " PieceTile: " + pieceIndex);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //  タッチ or クリックを検出
        //  ?? false → タッチデバイスが存在しない場合は false にする
        bool isTouchStart = Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false;
        bool isTouchEnd = Touchscreen.current?.primaryTouch.press.wasReleasedThisFrame ?? false;
        bool isClickStart = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
        bool isClickEnd = Mouse.current?.leftButton.wasReleasedThisFrame ?? false;

        // タッチ or クリック開始
        if (isTouchStart || isClickStart)
        {
            //  入力位置の取得（タッチ or マウス）
            Vector2 inputPos = isTouchStart
                ? Touchscreen.current.primaryTouch.position.ReadValue()
                : Mouse.current.position.ReadValue();

            // 2D
            // スクリーン座標をワールド座標に変換
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(inputPos);
            startPos = worldPos;    // スワイプ開始位置を保存

            //  2D用の Raycast を使用(「指定した座標にオブジェクトがあるか」調べる)
            //  Physics2D.Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask);
            //  origin	Ray の開始位置（ワールド座標）
            //  direction	Ray の飛ぶ方向（通常は Vector2.zero で「その点」をチェック）
            //  distance	Ray の距離（通常は Mathf.Infinity）
            //  layerMask	当たり判定のレイヤー（~0 なら全レイヤー）
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null) // ヒットしたオブジェクトがあるかチェック
            {
                selectedPiece = hit.collider.GetComponent<PieceController>();
                if (selectedPiece != null)
                {
                    selectedPiece.OnTouch();
                }
            }
            
            // 3D
            /*
            //  カメラから `inputPos` に向けて Ray を飛ばす
            Ray ray = Camera.main.ScreenPointToRay(inputPos);
            RaycastHit hit;

            //  Ray がオブジェクトに当たったか判定
            if (Physics.Raycast(ray, out hit))
            {
                PieceController piece = hit.collider.GetComponent<PieceController>();
                if (piece != null)
                {
                    piece.OnTouch(); // ピースにタッチ処理を通知
                }
            }
            */
        }

        // タッチ or クリック終了（リリース）
        if ((isTouchEnd || isClickEnd) && selectedPiece != null)
        {
            Vector2 inputPos = isTouchEnd
                ? Touchscreen.current.primaryTouch.position.ReadValue()
                : Mouse.current.position.ReadValue();

            Vector2 worldPos = Camera.main.ScreenToWorldPoint(inputPos);
            endPos = worldPos; // スワイプ終了位置を保存

            // SwapPieces関数内でスワイプ方向を判定
            Vector2 swipeDirection = endPos - startPos;
            SwapPieces(selectedPiece, swipeDirection);

            selectedPiece = null; // 選択解除
        }
    }
}
