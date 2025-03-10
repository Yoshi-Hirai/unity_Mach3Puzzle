using UnityEngine;
using System;
using System.Collections;

public class PieceView : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isMoving = false;
    private float moveSpeed = 5f;

    public IEnumerator MoveToWithCallback(Vector3 targetPosition, Action callback = null)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
        callback?.Invoke(); // コールバック実行
    }

    public void PlayDestroyAnimation(Action onComplete)
    {
        StartCoroutine(DestroyAnimationCoroutine(onComplete));
    }

    private IEnumerator DestroyAnimationCoroutine(Action onComplete)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        float fadeDuration = 0.3f;
        float elapsedTime = 0f;
        Color originalColor = spriteRenderer.color;

        while (elapsedTime < fadeDuration)
        {
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1 - (elapsedTime / fadeDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
        onComplete?.Invoke(); // 完全に消えたらコールバック実行
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (!isMoving)
        {
            StartCoroutine(SmoothMove(targetPosition));
        }
    }

    private IEnumerator SmoothMove(Vector3 targetPosition)
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
        isMoving = false;
    }

    private void OnMouseEnter()
    {
        spriteRenderer.color = originalColor * 1.5f;
    }

    private void OnMouseExit()
    {
        spriteRenderer.color = originalColor;
    }
}
