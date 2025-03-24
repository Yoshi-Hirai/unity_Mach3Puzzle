using UnityEngine;

namespace Match3Puzzle.Game
{
	public class CameraAdjust : MonoBehaviour
	{
		private const float HeightBufferRatio = 1.8f;
		private const float WideBufferRatio = 2f;
		private const float CameraYOffset = 0.75f;

		private int _gridWidth;     // グリッドの横サイズ
		private int _gridHeight;    // グリッドの縦サイズ
		private float _cellSize;     // 各ピースのサイズ

		//--------	Lifecycle Methods	--------
		private void Awake()
		{
			_gridWidth = BoardManager.Instance.BoardWidth;
			_gridHeight = BoardManager.Instance.BoardHeight;
			_cellSize = BoardManager.Instance.BoardGrid.cellSize.x; //	タイルは正方形を想定
		}

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		private void Start()
		{
			AdjustCamera();
		}

#if UNITY_EDITOR
		//	Update is called once per frame
		//	エディタ時のみ有効
		private void Update()
		{

		}
#endif
		//--------	Public Methods	--------

		//--------	private Methods	--------
		private void AdjustCamera()
		{
			Camera cam = Camera.main;
			if (cam == null) return;

			float screenRatio = (float)Screen.width / Screen.height;
			float targetRatio = (float)_gridWidth / _gridHeight;

			//  画面のアスペクト比に応じてカメラを調整する！
			//  グリッドが全体表示されるように orthographicSize を自動調整！
			if (screenRatio >= targetRatio)
			{
				// 画面が横長の場合 → 高さ基準でカメラサイズを設定(少し大きくはみ出るので 2 -> 1.8)
				cam.orthographicSize = (_gridHeight * _cellSize) / HeightBufferRatio;
			}
			else
			{
				// 画面が縦長の場合 → 幅基準でカメラサイズを設定
				cam.orthographicSize = ((_gridWidth * _cellSize) / screenRatio) / WideBufferRatio;
			}

			// グリッドの中心にカメラを配置(上によっているのでYオフセット+0.75f)
			cam.transform.position = new Vector3((_gridWidth - 1) * _cellSize / 2, (_gridHeight - 1) * _cellSize / 2 + CameraYOffset, -10);
			// Debug用：カメラ位置確認のため残しています
			//Debug.Log("size" + cam.orthographicSize + " " + cam.transform.position.y);
		}
	}
}
