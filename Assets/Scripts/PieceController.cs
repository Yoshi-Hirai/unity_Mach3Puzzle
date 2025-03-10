using UnityEngine;
using System;
using System.Collections;

public class PieceController : MonoBehaviour
{
    // マウスホバー時の明度アップ
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private float moveSpeed = 5f; // 移動速度

# if false
// ピース移動の補間処理
    // Vector3.Lerp を使用して位置を徐々に移動。目標位置に近づいたらループを終了し、最後に位置を固定。
    public IEnumerator SmoothMove(Transform movingPiece, Vector3 startPosition, Vector3 targetPosition, System.Action onComplete)
    {
        movingPiece.position = startPosition;
        while (Vector3.Distance(movingPiece.position, targetPosition) > 0.01f)
        {
            movingPiece.position = Vector3.Lerp(movingPiece.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null; // 次のフレームまで待つ
        }
        movingPiece.position = targetPosition; // 最後に位置を固定

        // コルーチン終了時にコールバックを実行(設定されていれば)
        onComplete?.Invoke();
    }

    //  [TODO]SwapWithNeighborとSwapPositionをまとめるリファクタリングを検討
    //  ピース交換処理(外部)
    public void SwapWithNeighbor(Vector2Int direction)
    {
        Vector2 targetPosition = (Vector2)transform.position + (Vector2)direction;
        RaycastHit2D hit = Physics2D.Raycast(targetPosition, Vector2.zero);

        if (hit.collider != null)
        {
            PieceController otherPiece = hit.collider.GetComponent<PieceController>();
            if (otherPiece != null)
            {
                SwapPosition(otherPiece.transform);
            }
        }
    }
    // ピース交換処理(内部)
    public void SwapPosition(Transform targetPiece)
    {
        Vector3 originalPos = transform.position;   //  現在の位置
        Vector3 targetPos = targetPiece.position;   //  入れ替え先の位置

        // [Todo] 処理負荷の懸念があるので、GameManagerなどにインスタンス化をしておく対応をする)
        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        if (boardManager == null)
        {
            throw new Exception("BoardManager is Null.");
        }
        // 内部データ(m_cell配列)を更新
        boardManager.SwapCellData(originalPos, targetPos);
        // 入れ替え移動アニメーションを行い、完了したらピースマッチチェックを呼び出す
        StartCoroutine(SmoothMove(transform, transform.position, targetPos, () => StartCoroutine(boardManager.PieceMatchCheckUpdate())));
        StartCoroutine(SmoothMove(targetPiece, targetPiece.position, originalPos, null));
    }

    private void OnMouseEnter() // マウスがピースの上に乗った時
    {
        spriteRenderer.color = originalColor * 1.5f; // 明るくする
    }

    private void OnMouseExit() // マウスがピースから離れた時
    {
        spriteRenderer.color = originalColor; // 元の色に戻す
    }

    // [TODO]Touch&Click対応
    void OnMouseDown()
    {
        spriteRenderer.color = originalColor * 1.25f; // 少し明るくする
    }

    // [TODO]Touch&Click対応
    void OnMouseUp()
    {
        spriteRenderer.color = originalColor; // 元の色に戻す
    }

    public void OnTouch()
    {
        Debug.Log("タッチ処理を実行: " + gameObject.name);
    }

    public void OnRelease()
    {
        Debug.Log("リリース処理を実行: " + gameObject.name);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color; // 元の色を保存
    }

    // Update is called once per frame
    void Update()
    {

    }
#endif
}
