using UnityEngine;
using System;
using System.Collections;

public class PieceView : MonoBehaviour
{
	public enum FadeState
	{
		None,       // フェードなし
		FadingIn,   // フェードイン中
		FadingOut   // フェードアウト中
	}
	private Vector3 targetPosition;
	private int targetAlpha;
	private SpriteRenderer spriteRenderer;
	private Color originalColor;
	private bool isMoving = false;
	private FadeState fadeState = FadeState.None;
	private float moveSpeed = 5f;
	private float fadeSpeed = 2.0f;

#if false
    public IEnumerator MoveToWithCallback(Vector3 targetPosition, Action callback = null)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
        callback?.Invoke(); // �R�[���o�b�N���s
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
        onComplete?.Invoke(); // ���S�ɏ�������R�[���o�b�N���s
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
#endif

	public bool IsMoving()
	{
		return isMoving;
	}
	public void MoveTo(Vector3 newPosition)
	{
		targetPosition = newPosition;
		isMoving = true;
	}

	public bool IsFading()
	{
		return fadeState == FadeState.None ? false : true;
	}
	public void SetFading(FadeState state)
	{
		fadeState = state;
	}

	//--------  Lifecycle Methods   --------
	void LateUpdate()
	{
		if (isMoving)
		{
			transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
			if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
			{
				transform.position = targetPosition;
				isMoving = false;
			}
		}
		{
			SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
			switch (fadeState)
			{
				case FadeState.FadingIn:
					//FadeIn();
					break;
				case FadeState.FadingOut:
					Color color = spriteRenderer.color;
					color.a -= fadeSpeed * Time.deltaTime;
					color.a = Mathf.Clamp01(color.a);
					spriteRenderer.color = color;

					if (color.a <= 0) // 完全に透明になったら処理を停止
					{
						SetFading(FadeState.None);
					}
					break;
			}
		}
	}
}
