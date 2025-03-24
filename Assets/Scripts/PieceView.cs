using UnityEngine;
using System;
using System.Collections;

namespace Match3Puzzle.Game
{
	public class PieceView : MonoBehaviour
	{
		//	定数・静的フィールド
		public enum FadeState
		{
			None,       //	フェードなし
			FadingIn,   //	フェードイン中
			FadingOut   //	フェードアウト中
		}
		private const float MoveSpeed = 5f;
		private const float FadeSpeed = 2.0f;

		private bool _isMoving = false;                     //	移動中フラグ(trueで移動中)
		private FadeState _fadeState = FadeState.None;      //	フェード状態
		private Vector3 _targetPosition;                    //	目標移動位置

		//--------	Lifecycle Methods	--------
		void LateUpdate()
		{
			if (_isMoving)
			{
				transform.position = Vector3.Lerp(transform.position, _targetPosition, MoveSpeed * Time.deltaTime);
				if (Vector3.Distance(transform.position, _targetPosition) < 0.01f)
				{
					transform.position = _targetPosition;
					_isMoving = false;
				}
			}
			{
				SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
				switch (_fadeState)
				{
					case FadeState.FadingIn:
						//FadeIn();
						break;
					case FadeState.FadingOut:
						Color color = spriteRenderer.color;
						color.a -= FadeSpeed * Time.deltaTime;
						color.a = Mathf.Clamp01(color.a);
						spriteRenderer.color = color;

						if (color.a <= 0) // 完全に透明になったら処理を停止
						{
							StartFade(FadeState.None);
						}
						break;
				}
			}
		}

		//--------	Public Methods	--------
		//	移動系
		public bool IsMoving()
		{
			return _isMoving;
		}
		public void StartMove(Vector3 newPosition)
		{
			_targetPosition = newPosition;
			_isMoving = true;
		}
		//	フェード系
		public bool IsFading()
		{
			return _fadeState != FadeState.None;
		}
		public void StartFade(FadeState newState)
		{
			_fadeState = newState;
		}

		//--------	private Methods	--------

#if false
#endif

	}
}