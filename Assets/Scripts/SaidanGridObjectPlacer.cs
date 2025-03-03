using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections.Generic; // HashSet<> を使うために追加

public class SaidanGridObjectPlacer : MonoBehaviour
{
    public Tilemap tilemap; // グリッドのTilemap
    public GameObject objectPrefab; // 配置するオブジェクトのPrefab
    private PlayerInputActions inputActions;

    // 配置済みかどうかをチェック
    private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    private bool IsOccupied(Vector3Int position)
    {
        return occupiedCells.Contains(position);
    }
    private void MarkOccupied(Vector3Int position)
    {
        occupiedCells.Add(position);
    }

    private void PlaceObject(Vector3Int gridPosition)
    {
        Debug.Log($"配置するグリッド座標: {gridPosition}");
        // 既に配置されているかチェック
        if (!IsOccupied(gridPosition))
        {
            Debug.Log("配置する!");
            Vector3 placePosition = tilemap.GetCellCenterWorld(gridPosition);
            Instantiate(objectPrefab, placePosition, Quaternion.identity);
            MarkOccupied(gridPosition);
        }
    }

    private void OnClick()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        //mouseScreenPos.z = Camera.main.nearClipPlane; // 🔹 ここでZ座標をカメラの近接クリップに設定
        //mouseScreenPos.z = -Camera.main.transform.position.z; // 🔹 Z座標をカメラの位置に設定        

        //Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        // 🔹 Camera.main.transform.position.y を基準にワールド座標に変換
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Camera.main.transform.position.y));

        mouseWorldPos.z = 0; // Z座標を0にする
        Vector3Int gridPosition = tilemap.WorldToCell(mouseWorldPos);

        //Debug.Log($"クリックしたグリッド座標: {gridPosition}");

        PlaceObject(gridPosition);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // オブジェクトがシーンにロードされたときに最初に呼ばれるメソッドです。Start() よりも早く実行される。
    void Awake()
    {
        inputActions = new PlayerInputActions();                //  インスタンス化
        inputActions.Enable();                                  //  入力を有効化
        inputActions.Click.Newaction.performed += ctx => OnClick();    //  クリック時のイベント
    }

    // Update is called once per frame
    void Update()
    {
    }
}
