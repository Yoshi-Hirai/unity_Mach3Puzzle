using UnityEngine;
using UnityEngine.Tilemaps;

public class SaidanGridManager : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase gridTile; // タイル画像

    public int width = 5;
    public int height = 5;

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                tilemap.SetTile(tilePosition, gridTile);
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateGrid();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
