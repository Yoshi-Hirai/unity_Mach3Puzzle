using UnityEngine;
using System;
using System.Collections;

public class PieceController : MonoBehaviour
{
    private Vector2 startPos;
    private Vector2 endPos;

    // マウスホバー時の明度アップ
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private float swipeThreshold = 0.5f; // スワイプの判定閾値
    private float moveSpeed = 5f; // 移動速度

    // スワイプ方向を検知する
    private void DetectSwipe()
    {
        Vector2 swipeDelta = endPos - startPos;

        if (Mathf.Abs(swipeDelta.x) > swipeThreshold || Mathf.Abs(swipeDelta.y) > swipeThreshold)
        {
            // スワイプ方向を判定
            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                // 左右スワイプ
                if (swipeDelta.x > 0) Swap(Vector2.right); // 右
                else Swap(Vector2.left); // 左
            }
            else
            {
                // 上下スワイプ
                if (swipeDelta.y > 0) Swap(Vector2.up); // 上
                else Swap(Vector2.down); // 下
            }
        }
    }

    // ピース移動の補間処理
    // Vector3.Lerp を使用して位置を徐々に移動。目標位置に近づいたらループを終了し、最後に位置を固定。
    private IEnumerator SmoothMove(Transform movingPiece, Vector3 targetPosition, float speed, System.Action onComplete)
    {
        while (Vector3.Distance(movingPiece.position, targetPosition) > 0.01f)
        {
            movingPiece.position = Vector3.Lerp(movingPiece.position, targetPosition, speed * Time.deltaTime);
            yield return null; // 次のフレームまで待つ
        }
        movingPiece.position = targetPosition; // 最後に位置を固定

        // コルーチン終了時にコールバックを実行
        onComplete?.Invoke();
    }

    // ピース交換処理
    public void SwapPosition(Transform targetPiece)
    {
        Vector3 originalPos = transform.position;   //  現在の位置
        Vector3 targetPos = targetPiece.position;   //  入れ替え先の位置

        // 入れ替え移動が完了したらピースマッチチェックを呼び出す
        // [Todo] 処理負荷の懸念があるので、GameManagerなどにインスタンス化をしておく対応をする)
        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        if (boardManager == null) {
            throw new Exception("BoardManager is Null.");
        }

        // 内部データ(m_cell配列)を更新
        boardManager.SwapCellData(originalPos, targetPos);        
        StartCoroutine(SmoothMove(transform, targetPos, moveSpeed, boardManager.PieceMatchCheck));
        StartCoroutine(SmoothMove(targetPiece, originalPos, moveSpeed, null));
    }

    void Swap(Vector2 direction)
    {
        // 入れ替え処理
        Vector2 targetPos = (Vector2)transform.position + direction;
        Collider2D targetPiece = Physics2D.OverlapPoint(targetPos);

        // ↓ この判定が必要か検討。操作感が悪くなりそう。
        /*
        if (Mathf.Abs(targetPos.x - originalPos.x) > 1 || Mathf.Abs(targetPos.y - originalPos.y) > 1)
        {
            return; // 隣接していない場合は何もしない
        }
        */

        if (targetPiece)
        {
            // ピースを入れ替える
            Debug.Log("Swap: (" + transform.position.x + "," + transform.position.y + ") <=> (" + targetPiece.transform.position.x + "," + targetPiece.transform.position.y);
            SwapPosition(targetPiece.transform);
        }
    }

    private void OnMouseEnter() // マウスがピースの上に乗った時
    {
        spriteRenderer.color = originalColor * 1.5f; // 明るくする
    }

    private void OnMouseExit() // マウスがピースから離れた時
    {
        spriteRenderer.color = originalColor; // 元の色に戻す
    }
    
    void OnMouseDown()
    {
        spriteRenderer.color = originalColor * 1.25f; // 少し明るくする
        startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Debug.Log("MouseDown: " + startPos.x + " , " + startPos.y);
    }

    void OnMouseUp()
    {
        spriteRenderer.color = originalColor; // 元の色に戻す
        endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        DetectSwipe();
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
}
