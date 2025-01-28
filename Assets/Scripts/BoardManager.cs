using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour
{
    public int Width;
    public int Height;
    public Tile[] GrounPatterns;            // 背景タイルテクスチャパターン
    public GameObject[] PiecesPatterns;      // ピースパターン 

    private Tilemap m_Tilemap;
    private Grid m_Grid;

    private GameObject[,] m_Cell;      // セルデータの管理

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
                int groundIndex = Random.Range(0, GrounPatterns.Length);
                m_Tilemap.SetTile(new Vector3Int(x, y, 0), GrounPatterns[groundIndex]);

                // ピースを生成し登録
                int pieceIndex = Random.Range(0, PiecesPatterns.Length);
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
