using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace Match3Puzzle.Game
{
	public class InputManager : MonoBehaviour
	{
		//	定数・静的フィールド

		//	ピース交換時のイベント
		//	処理内容はピース交換処理
		public event Action<GameObject, Vector2> OnPieceSwiped;

		private GameObject _selectedPiece;

		//--------	Lifecycle Methods	--------
		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{

		}

		//--------	Public Methods	--------
		public void ProcessInput()
		{
			// 入力の開始と終了チェック（タッチ or マウス）
			if (TouchStarted() || ClickStarted() || TouchEnded() || ClickEnded())
			{
				HandleInput();
			}
		}

		//--------	private Methods	--------
		//	ピース選択時の処理
		private void HandleInput()
		{
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(GetInputPosition());
			if (_selectedPiece == null)
			{
				//	1つ目のピース選択
				//	2D用の Raycast を使用(「指定した座標にオブジェクトがあるか」調べる)
				//	Physics2D.Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask);
				//	origin		Ray の開始位置（ワールド座標）
				//	direction	Ray の飛ぶ方向（通常は Vector2.zero で「その点」をチェック）
				//	distance	Ray の距離（通常は Mathf.Infinity）
				//	layerMask	当たり判定のレイヤー（~0 なら全レイヤー）
				RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
				if (hit.collider != null)
				{
					_selectedPiece = hit.collider.gameObject;
					//Debug.Log($"選択されたピース: {_selectedPiece.name}");
				}
			}
			else
			{
				//	2つ目のピース(入れ替え先)を選択
				Vector2 selectedPiece2D = new Vector2(_selectedPiece.transform.position.x, _selectedPiece.transform.position.y);
				Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);
				Vector2 swipeDirection = worldPos2D - selectedPiece2D;
				Debug.Log($"スワップ方向: {swipeDirection}");
				OnPieceSwiped?.Invoke(_selectedPiece, swipeDirection);
				_selectedPiece = null; // 選択解除
			}
		}

		//	入力位置の検出
		private static Vector2 GetInputPosition() => Touchscreen.current?.primaryTouch.position.ReadValue() ?? Mouse.current.position.ReadValue();
		//  タッチ(タップ) or クリックを検出
		//  ?? false → タッチデバイスが存在しない場合は false にする
		private static bool TouchStarted() => Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false;
		private static bool TouchEnded() => Touchscreen.current?.primaryTouch.press.wasReleasedThisFrame ?? false;
		private static bool ClickStarted() => Mouse.current?.leftButton.wasPressedThisFrame ?? false;
		private static bool ClickEnded() => Mouse.current?.leftButton.wasReleasedThisFrame ?? false;

	}
}
