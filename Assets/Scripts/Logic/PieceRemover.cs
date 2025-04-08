using UnityEngine;
using System;
using System.Collections.Generic;

namespace Match3Puzzle.Game
{
	public class PieceRemover
	{
		private readonly GameObject[,] _cell;
		private readonly Func<Vector3, Vector2Int> _posToCellIndex;     //	「Vector3を引数に取って、Vector2Intを返す関数」

		//--------	コンストラクタ	--------
		public PieceRemover(GameObject[,] cell, Func<Vector3, Vector2Int> posToCellIndex)
		{
			_cell = cell;
			_posToCellIndex = posToCellIndex;
		}

		//--------	public	--------

		//	指定されたピースを取り除く
		//	・削除されたピースをピースデータを管理する配列(_cell)から消去
		//	・削除されたピースをDestory
		public void Remove(GameObject piece)
		{
			Vector2Int cellPos = _posToCellIndex(piece.transform.position);
			_cell[cellPos.x, cellPos.y] = null; // セル情報から削除
			UnityEngine.Object.Destroy(piece);  // 表示上も削除
		}

		//	ピース削除時の内部処理(ピース削除アニメーション終了時に呼ばれる)
		//	・削除されたピースを取り除く
		//	・マッチしたピースのリスト(_matchedPieces)をクリア
		public void RemoveMany(List<(GameObject, BoardManager.MatchedType, int)> matchedPieces)
		{
			foreach (var (piece, _, _) in matchedPieces)
			{
				Remove(piece);
			}
			matchedPieces.Clear();
		}
	}
}
