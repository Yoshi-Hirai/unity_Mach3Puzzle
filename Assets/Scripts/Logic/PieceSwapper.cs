using UnityEngine;

namespace Match3Puzzle.Game
{
	public class PieceSwapper
	{
		//	定数・静的フィールド
		private const int NotSelected = -1;     //	ピースが選択されていない

		private readonly GameObject[,] _cell;
		private readonly Vector2Int[] _swapIndex;        //	ピースを交換するインデックス(cellの位置。[NotSelected, NotSelected]で未設定)

		//--------	コンストラクタ	--------
		public PieceSwapper(GameObject[,] cell)
		{
			_cell = cell;
			_swapIndex = new Vector2Int[2];
			ClearSwapIndex();
		}

		//--------	Public Methods	--------

		//  ピース交換処理
		public void Swap(GameObject piece, GameObject otherPiece, Vector2Int pieceCellPos, Vector2Int otherPieceCellPos)
		{
			SetSwapIndex(pieceCellPos, otherPieceCellPos);

			// ピースの移動開始
			piece.GetComponent<PieceView>().StartMove(otherPiece.transform.position);
			otherPiece.GetComponent<PieceView>().StartMove(piece.transform.position);
		}

		//	ピース交換時の内部処理(ピース交換アニメーション終了時に呼ばれる)
		//	・ピースデータを管理する配列(_cell)に対して、交換ピースを移動させる
		//	・ピースを交換するインデックスをクリア
		public void SwapPieceInternal()
		{
			GameObject temp = _cell[_swapIndex[0].x, _swapIndex[0].y];
			_cell[_swapIndex[0].x, _swapIndex[0].y] = _cell[_swapIndex[1].x, _swapIndex[1].y];
			_cell[_swapIndex[1].x, _swapIndex[1].y] = temp;
			ClearSwapIndex();
		}

		public void ClearSwapIndex()
		{
			SetSwapIndex(new Vector2Int(NotSelected, NotSelected), new Vector2Int(NotSelected, NotSelected));
		}

		//--------	Private Methods	--------

		//	ピースを交換するインデックス(_swapIndex)関連
		private void SetSwapIndex(Vector2Int index0, Vector2Int index1)
		{
			_swapIndex[0] = index0;
			_swapIndex[1] = index1;
		}
	}
}
