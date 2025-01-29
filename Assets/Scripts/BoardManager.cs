using UnityEngine;
using UnityEngine.Tilemaps;
using System;
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

    // セル更新（下方向に移動して補充）
    private void updateCell()
    {
        for (int x = 0; x < Width; x++)
        {
            // 下から上に向かって確認
            for (int y = 0; y < Height; y++)
            {
                if (m_Cell[x, y] == null) // 空のスペースがある場合
                {
                    Debug.Log("Space: " + x + "," + y);
                    // 現在より上部からピースを探して移動
                    for (int aboveY = y + 1; aboveY < Height; aboveY++)
                    {
                        if (m_Cell[x, aboveY] != null)
                        {
                            Debug.Log("Bury: " + x + "," + aboveY + " => " + x + "," + y);
                            // ピースを下に移動
                            m_Cell[x, y] = m_Cell[x, aboveY];
                            m_Cell[x, aboveY].transform.position = ConvertFromCellToTransform( new Vector2Int(x, y) );
                            m_Cell[x, aboveY] = null;
                            break;
                        }
                    }
                }
            }

            // 上部から新しいピースを補充
/*
            for (int y = height - 1; y >= 0; y--)
            {
                if (grid[x, y] == null)
                {
                    AddPieceToGrid(x, y);
                }
            }
*/
        }
    }

    // ransform.position <=> m_cellインデックス変換
    public Vector2Int ConvertFromTransformToCell(Vector3 transformposition)
    {
        return new Vector2Int( Mathf.FloorToInt(transformposition.x), Mathf.FloorToInt(transformposition.y) );
    }
    public Vector3 ConvertFromCellToTransform(Vector2Int cellindex)
    {
        return new Vector3( (float)cellindex.x + 0.5f, (float)cellindex.y + 0.5f, 0 );
    }

    // セルデータ(m_Cell)を入れ替える
    public void SwapCellData(Vector3 pos1, Vector3 pos2)
    {
        // 座標をグリッドのインデックスに変換
        Vector2Int index1 = ConvertFromTransformToCell(pos1);
        Vector2Int index2 = ConvertFromTransformToCell(pos2);

        Debug.Log("1: " + index1.x + "," + index1.y + " 2: " + index2.x + "," + index2.y);
        // グリッドデータをスワップ
        GameObject temp = m_Cell[index1.x, index1.y];
        m_Cell[index1.x, index1.y] = m_Cell[index2.x, index2.y];
        m_Cell[index2.x, index2.y] = temp;
    }
    
    // 横・縦方向にピースをチェック
    public void PieceMatchCheck()
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

        // マッチしたピースを削除
        foreach (var piece in matchedPieces)
        {
            // 座標を取得
            Vector3 pos = piece.transform.position;
            int x = (int)Math.Floor(pos.x);
            int y = (int)Math.Floor(pos.y);

            // 先にm_CellをnullにしてからDestroy
            m_Cell[x, y] = null;
            Debug.Log("Delete: " + x + "," + y);
            Destroy(piece);
        }

        updateCell();
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
                int pieceIndex = UnityEngine.Random.Range(0, PiecesPatterns.Length);
                GameObject cell = Instantiate(PiecesPatterns[pieceIndex], m_Grid.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
                m_Cell[x, y] = cell;

                //Debug.Log("Groudtile: " + groundIndex + " PieceTile: " + pieceIndex);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
