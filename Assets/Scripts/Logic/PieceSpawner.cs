using UnityEngine;
using System;

namespace Match3Puzzle.Game
{
	public class PieceSpawner
	{
		private readonly GameObject[,] _cell;
		private readonly GameObject[] _piecesPatterns;
		private readonly Grid _grid;
		private readonly Func<Vector2Int, Vector3> _cellIndexToPos;     //	「Vector2Intを引数に取って、Vector3を返す関数」

		//--------	コンストラクタ	--------
		public PieceSpawner(GameObject[,] cell, GameObject[] piecesPatterns, Grid grid, Func<Vector2Int, Vector3> cellIndexToPos)
		{
			_cell = cell;
			_piecesPatterns = piecesPatterns;
			_grid = grid;
			_cellIndexToPos = cellIndexToPos;
		}

		//--------	Public Methods	--------
		// ✅ シード指定メソッド
		public static void SetRandomSeed(int seed)
		{
			UnityEngine.Random.InitState(seed); // ランダムをシード値で初期化
		}

		//	ピース処理関連
		//	ピースを指定位置に生成し、ピースデータを管理する_cellへ代入する
		public void CreateToGrid(int x, int y)
		{
			int pieceIndex = UnityEngine.Random.Range(0, _piecesPatterns.Length);
			GameObject cell = UnityEngine.Object.Instantiate(_piecesPatterns[pieceIndex], _grid.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
			_cell[x, y] = cell;
		}

		public void CreateToGridWithType(int x, int y, int pieceType)
		{
			if (pieceType < 0 || _piecesPatterns.Length <= pieceType)
			{
				Debug.Log("Piece Type is out of range " + pieceType);
				return;
			}
			GameObject cell = UnityEngine.Object.Instantiate(_piecesPatterns[pieceType], _grid.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
			_cell[x, y] = cell;
		}

		//	ピース処理関連
		//	ピースを指定位置に生成し、ピースデータを管理する_cellへ代入する
		//	Dropアニメーションの実行を指示する
		public void CreateAndDropToGrid(int x, int y, int startY)
		{
			// 生成時は、描画を待たずにこの段階でm_CellにGameObjectを生成する
			CreateToGrid(x, y);
			var view = _cell[x, y].GetComponent<PieceView>();
			view.transform.position = _cellIndexToPos(new Vector2Int(x, startY));
			view.StartMove(_cellIndexToPos(new Vector2Int(x, y)));
			//	非表示で開始したい場合は下記を実行する
			//	_cell[x, y].GetComponent<Renderer>().enabled = false;
		}

	}
}
