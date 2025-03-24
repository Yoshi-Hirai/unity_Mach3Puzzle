using UnityEngine;

namespace Match3Puzzle.Game
{
	public class CameraAdjust : MonoBehaviour
	{
		//  [TODO]BoardManagerと重複しているのでリファクタリング
		public int gridWidth = 8;   // グリッドの横サイズ
		public int gridHeight = 8;  // グリッドの縦サイズ
		public float cellSize = 1f; // 各ピースのサイズ

		//--------	Lifecycle Methods	--------
		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			AdjustCamera();
		}

		// Update is called once per frame
		void Update()
		{
			// このメソッドは将来的な拡張用のプレースホルダー
		}

		//--------	Public Methods	--------

		//--------	private Methods	--------
		private void AdjustCamera()
		{
			Camera cam = Camera.main;
			if (cam == null) return;

			float screenRatio = (float)Screen.width / Screen.height;
			float targetRatio = (float)gridWidth / gridHeight;

			//  画面のアスペクト比に応じてカメラを調整する！
			//  グリッドが全体表示されるように orthographicSize を自動調整！
			if (screenRatio >= targetRatio)
			{
				// 画面が横長の場合 → 高さ基準でカメラサイズを設定(少し大きくはみ出るので 2 -> 1.8)
				cam.orthographicSize = (gridHeight * cellSize) / 1.8f;
			}
			else
			{
				// 画面が縦長の場合 → 幅基準でカメラサイズを設定
				cam.orthographicSize = ((gridWidth * cellSize) / screenRatio) / 2f;
			}

			// グリッドの中心にカメラを配置(上によっているのでYオフセット+0.75f)
			cam.transform.position = new Vector3((gridWidth - 1) * cellSize / 2, (gridHeight - 1) * cellSize / 2 + 0.75f, -10);
			// Debug用：カメラ位置確認のため残しています
			//Debug.Log("size" + cam.orthographicSize + " " + cam.transform.position.y);
		}
	}
}
