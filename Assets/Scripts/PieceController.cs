using UnityEngine;

public class PieceController : MonoBehaviour
{
    private Vector2 startPos;
    private Vector2 endPos;
    private float swipeThreshold = 0.5f; // スワイプの判定閾値

    void OnMouseDown()
    {
        startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Debug.Log("MouseDown: " + startPos.x + " , " + startPos.y);
    }

    void OnMouseUp()
    {
        endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        DetectSwipe();
    }

    void DetectSwipe()
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
            Vector2 originalPos = transform.position;
            transform.position = targetPiece.transform.position;
            targetPiece.transform.position = originalPos;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
