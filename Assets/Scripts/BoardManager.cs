using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Match3Puzzle.Game
{
	public class BoardManager : MonoBehaviour
	{
		//	定数・静的フィールド
		public enum GameState
		{
			WaitingForInput,    //	ユーザー入力待ち
			SwapAnimating,      //	ピースの入れ替えアニメーション
			Matching,           //	マッチング判定
			DeleteFading,       //	消失時のフェーディングアニメーション
			Falling,            //	ピースの落下処理
			FallingAnimating,   //	ピースの落下アニメーション
			Createing,          //	ピースの追加
			CreatingAnimating,  //	ピースの追加アニメーション
			ResolvingChain,     //	連鎖の処理 -> Matching に遷移させて連鎖処理を行わせることができているため不要。
		}
		private const int NotSelected = -1;                     //	ピースが選択されていない
		private const int MatchThreshold = 3;                   //	マッチと判断されるピース数閾値

		//	インスペクタで設定する変数
		[SerializeField] private int Width;                     //	盤面の幅
		[SerializeField] private int Height;                    //	盤面の高さ
		[SerializeField] private Tile[] GroundPatterns;         //	背景タイルテクスチャパターン
		[SerializeField] private GameObject[] PiecesPatterns;   //	ピースパターン(prefab)

		private GameState _currentState = GameState.WaitingForInput;
		private GameObject[,] _cell;        //	ピースデータの管理
		private Tilemap _tileMap;           //	背景タイルデータの管理(子Object:Tilemap)
		private Grid _grid;                 //	子Object:Grid

		private GameObject _selectedPiece;          //	操作で選択されたピース
		private Vector2Int[] _swapIndex;            //	ピースを交換するインデックス(cellの位置。[NotSelected, NotSelected]で未設定)
		private List<GameObject> _matchedPieces;    //	マッチしたピース(GameObject)のリスト

		//	落下するピース情報のリスト
		private readonly List<(GameObject piece, Vector2Int from, Vector2Int to)> _fallingPieces = new List<(GameObject, Vector2Int, Vector2Int)>();

		//--------	Lifecycle Methods	--------
		//	オブジェクト生成時
		private void Awake()
		{
			// このメソッドは将来的な拡張用のプレースホルダー
		}

		//	Start is called once before the first execution of Update after the MonoBehaviour is created(最初のフレームの前)
		//	・グリッド (_cell) の初期化
		//	・背景 (Tilemap) の生成
		//	・ピース (CreatePieceToGrid()) の配置
		private void Start()
		{
			_tileMap = GetComponentInChildren<Tilemap>();
			_grid = GetComponentInChildren<Grid>();
			_cell = new GameObject[Width, Height];
			_swapIndex = new Vector2Int[2];
			ClearSwapIndex();

			for (int y = 0; y < Height; ++y)
			{
				for (int x = 0; x < Width; ++x)
				{
					//　背景タイルを生成
					int groundIndex = UnityEngine.Random.Range(0, GroundPatterns.Length);
					_tileMap.SetTile(new Vector3Int(x, y, 0), GroundPatterns[groundIndex]);

					// ピースを生成し登録
					CreatePieceToGrid(x, y);
				}
			}
		}

		// Update is called once per frame(毎フレーム)
		private void Update()
		{
			GameState tempState = _currentState;
			switch (_currentState)
			{
				case GameState.WaitingForInput:
					HandleWaitingForInput();
					break;
				case GameState.SwapAnimating:
					if (AreAllPiecesSettled())
					{
						SwapPieceInternal();
						ChangeGameState(GameState.Matching);
					}
					break;
				case GameState.Matching:
					HandleMatching();
					break;
				case GameState.DeleteFading:
					if (AreAllPiecesFadesCompleted())
					{
						DeletePieceInternal();
						ChangeGameState(GameState.Falling);
					}
					break;
				case GameState.Falling:
					HandleFalling();
					ChangeGameState(GameState.FallingAnimating);
					break;
				case GameState.FallingAnimating:
					if (AreAllPiecesSettled())
					{
						FallingPieceInternal();
						ChangeGameState(GameState.Createing);
					}
					break;
				case GameState.Createing:
					HandleCreating();
					ChangeGameState(GameState.CreatingAnimating);
					break;

				case GameState.CreatingAnimating:
					if (AreAllPiecesSettled())
					{
						ChangeGameState(GameState.Matching);
						/*
											for (int y = 0; y < Height; ++y)
											{
												for (int x = 0; x < Width; ++x)
												{
													if (_cell[x, y] == null)
													{
														Debug.Log("NULL :(" + x + "," + y + ")");
													}
												}
											}
						*/
					}
					break;
				case GameState.ResolvingChain:
					break;
			}

			if (_currentState != tempState)
			{
				Debug.Log("Change State: " + tempState + "=>" + _currentState);
			}
		}

		//	物理計算
		private void FixedUpdate()
		{
			// このメソッドは将来的な拡張用のプレースホルダー
		}

		//	`Update()` の後
		private void LateUpdate()
		{
			switch (_currentState)
			{
				case GameState.WaitingForInput:
				case GameState.SwapAnimating:
				case GameState.Matching:
				case GameState.DeleteFading:
				case GameState.Falling:
				case GameState.FallingAnimating:
				case GameState.Createing:
				case GameState.CreatingAnimating:
				case GameState.ResolvingChain:
					break;
			}
		}

		//--------	Event Methods	--------

		//--------	Public Methods	--------

		//--------	private Methods	--------
		//	ゲームステート(_currentState)関連
		private void ChangeGameState(GameState state)
		{
			_currentState = state;
		}

		//	各ゲームステートのHandle
		private void HandleWaitingForInput()
		{
			//  タッチ(タップ) or クリックを検出
			//  ?? false → タッチデバイスが存在しない場合は false にする
			bool isTouchStart = Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false;
			bool isTouchEnd = Touchscreen.current?.primaryTouch.press.wasReleasedThisFrame ?? false;
			bool isClickStart = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
			bool isClickEnd = Mouse.current?.leftButton.wasReleasedThisFrame ?? false;

			if (isTouchStart || isClickStart || isTouchEnd || isClickEnd)
			{
				ExecutePieceSelection();
			}
		}

		private void HandleMatching()
		{
			_matchedPieces = CheckPieceMatch();
			Debug.Log("MatchPiece Count: " + _matchedPieces.Count);
			if (_matchedPieces.Count != 0)
			{
				// マッチしたピースに削除アニメーションを指示。アニメーション終了後、削除し_cellを更新
				foreach (var piece in _matchedPieces)
				{
					// ピースのフェードアウト開始
					piece.GetComponent<PieceView>().StartFade(PieceView.FadeState.FadingOut);
				}
				ChangeGameState(GameState.DeleteFading);
			}
			else
			{
				ChangeGameState(GameState.WaitingForInput);
			}
		}

		private void HandleFalling()
		{
			_fallingPieces.Clear();

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++) // 下から順に処理する
				{
					//	空のセルがある or すでに落下したセル の場合 => ここに移動(=落下)させるピースを探す
					if ((_cell[x, y] == null) || _fallingPieces.Any(fp => fp.from == new Vector2Int(x, y)))
					{
						int fallDistance = 0; // 落下距離
						int aboveY = -1;

						//	最も遠いピースを探す
						for (aboveY = y + 1; aboveY < Height; aboveY++)
						{
							if ((_cell[x, aboveY] != null) && (!_fallingPieces.Any(fp => fp.from == new Vector2Int(x, aboveY))))
							{
								fallDistance = aboveY - y; // 落下距離を計算
								break;
							}
						}

						// 落下ピースが見つかった場合
						if (aboveY >= 0 && fallDistance > 0)
						{
							// ピースの移動
							//Debug.Log("Fall x" + x + " y " + aboveY + "=" + y);
							_cell[x, aboveY].GetComponent<PieceView>().StartMove(ConvertFromCellToTransform(new Vector2Int(x, y)));
							_fallingPieces.Add((_cell[x, aboveY], new Vector2Int(x, aboveY), new Vector2Int(x, y)));
						}
					}
				}
			}
		}

		private void HandleCreating()
		{
			// 新しいピースを補充
			for (int x = 0; x < Width; x++)
			{
				int numAddPiece = 0;    //	生成ピースを不可視化していないのでもし見えるようであれば、この変数をインクリメントしてより遠くから始動させる
				for (int y = 0; y < Height; y++)
				{
					if (_cell[x, y] == null)
					{
						// 生成時は、描画を待たずにこの段階でm_CellにGameObjectを生成する
						CreatePieceToGrid(x, y);
						_cell[x, y].GetComponent<PieceView>().transform.position = ConvertFromCellToTransform(new Vector2Int(x, Height + numAddPiece));
						_cell[x, y].GetComponent<PieceView>().StartMove(ConvertFromCellToTransform(new Vector2Int(x, y)));
						//	_cell[x, y].GetComponent<Renderer>().enabled = false;
						numAddPiece++;
					}
				}
			}
		}

		//	ピースを交換するインデックス(_swapIndex)関連
		private void SetSwapIndex(Vector2Int index0, Vector2Int index1)
		{
			_swapIndex[0] = index0;
			_swapIndex[1] = index1;
		}
		private void ClearSwapIndex()
		{
			SetSwapIndex(new Vector2Int(NotSelected, NotSelected), new Vector2Int(NotSelected, NotSelected));
		}

		// transform.position <=> m_cellインデックス変換
		private Vector2Int ConvertFromTransformToCell(Vector3 transformPosition)
		{
			Vector3Int cellPosition = _grid.WorldToCell(transformPosition);
			return new Vector2Int(cellPosition.x, cellPosition.y);
		}
		private Vector3 ConvertFromCellToTransform(Vector2Int cellIndex)
		{
			return _grid.GetCellCenterWorld(new Vector3Int(cellIndex.x, cellIndex.y, 0));
		}

		//	ピース処理関連
		//	ピースを指定位置に生成し、ピースデータを管理する_cellへ代入する
		private void CreatePieceToGrid(int x, int y)
		{
			int pieceIndex = UnityEngine.Random.Range(0, PiecesPatterns.Length);
			GameObject cell = Instantiate(PiecesPatterns[pieceIndex], _grid.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
			cell.AddComponent<PieceView>(); // PieceView を自動でアタッチ
			_cell[x, y] = cell;
		}

		//	ピース選択時の処理
		private void ExecutePieceSelection()
		{
			//	入力位置の取得 -> スクリーン座標をワールド座標に変換
			Vector2 inputPos = Touchscreen.current?.primaryTouch.position.ReadValue() ?? Mouse.current.position.ReadValue();
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPos);

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
				ExecutePieceSwap(_selectedPiece, swipeDirection);
				_selectedPiece = null; // 選択解除

				ChangeGameState(GameState.SwapAnimating);
			}
		}

		//  ピース交換処理
		private void ExecutePieceSwap(GameObject piece, Vector2 swipeDirection)
		{
			float swipeThreshold = 0.5f;
			// 入れ替え判定
			if (Mathf.Abs(swipeDirection.x) > swipeThreshold || Mathf.Abs(swipeDirection.y) > swipeThreshold)
			{
				Vector2Int direction = Vector2Int.zero;
				if (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y))
				{
					direction = (swipeDirection.x > 0 ? Vector2Int.right : Vector2Int.left);
				}
				else
				{
					direction = (swipeDirection.y > 0 ? Vector2Int.up : Vector2Int.down);
				}
				Debug.Log($"Direction: {direction}");
				//  入れ替え先にピースが存在するか？
				Vector2 targetPosition = (Vector2)piece.transform.position + (Vector2)direction;
				RaycastHit2D hit = Physics2D.Raycast(targetPosition, Vector2.zero);
				if (hit.collider != null)
				{
					GameObject otherPiece = hit.collider.gameObject;    //  入れ替え先のピースオブジェクト
																		//  m_cellのインデックスを算出 -> 内部データ(_cell)更新用に保持
					SetSwapIndex(ConvertFromTransformToCell(piece.transform.position), ConvertFromTransformToCell(otherPiece.transform.position));

					// ピースの移動開始
					piece.GetComponent<PieceView>().StartMove(otherPiece.transform.position);
					otherPiece.GetComponent<PieceView>().StartMove(piece.transform.position);
				}
			}
		}

		// 横・縦方向にピースをチェック
		private List<GameObject> CheckPieceMatch()
		{
			HashSet<GameObject> matchedPieces = new HashSet<GameObject>(); // 重複を防ぐため HashSet を使用

			// 横方向のチェック
			for (int y = 0; y < Height; y++)
			{
				int matchCount = 1; // 連続しているピースの数
				List<GameObject> tempMatches = new List<GameObject>();
				tempMatches.Add(_cell[0, y]);

				for (int x = 1; x < Width; x++)
				{
					if (_cell[x, y] != null && _cell[x - 1, y] != null &&
						_cell[x, y].tag == _cell[x - 1, y].tag)
					{
						matchCount++;
						tempMatches.Add(_cell[x, y]);
					}
					else
					{
						if (matchCount >= MatchThreshold)   // MatchThreshold以上並んでいたら追加
						{
							foreach (var piece in tempMatches)
								matchedPieces.Add(piece);
						}
						matchCount = 1;
						tempMatches.Clear();
						tempMatches.Add(_cell[x, y]);
					}
				}

				if (matchCount >= MatchThreshold) // ループ終了時もチェック
				{
					foreach (var piece in tempMatches)
						matchedPieces.Add(piece);
				}
			}

			// 縦方向のチェック
			for (int x = 0; x < Width; x++)
			{
				int matchCount = 1;
				List<GameObject> tempMatches = new List<GameObject>();
				tempMatches.Add(_cell[x, 0]);

				for (int y = 1; y < Height; y++)
				{
					if (_cell[x, y] != null && _cell[x, y - 1] != null &&
						_cell[x, y].tag == _cell[x, y - 1].tag)
					{
						matchCount++;
						tempMatches.Add(_cell[x, y]);
					}
					else
					{
						if (matchCount >= MatchThreshold)
						{
							foreach (var piece in tempMatches)
								matchedPieces.Add(piece);
						}
						matchCount = 1;
						tempMatches.Clear();
						tempMatches.Add(_cell[x, y]);
					}
				}

				if (matchCount >= MatchThreshold)
				{
					foreach (var piece in tempMatches)
						matchedPieces.Add(piece);
				}
			}

			return new List<GameObject>(matchedPieces);
		}

		//	ピース交換時の内部処理(ピース交換アニメーション終了時に呼ばれる)
		//	・ピースデータを管理する配列(_cell)に対して、交換ピースを移動させる
		//	・ピースを交換するインデックスをクリア
		private void SwapPieceInternal()
		{
			GameObject temp = _cell[_swapIndex[0].x, _swapIndex[0].y];
			_cell[_swapIndex[0].x, _swapIndex[0].y] = _cell[_swapIndex[1].x, _swapIndex[1].y];
			_cell[_swapIndex[1].x, _swapIndex[1].y] = temp;
			ClearSwapIndex();
		}

		//	ピース削除時の内部処理(ピース削除アニメーション終了時に呼ばれる)
		//	・削除されたピースをピースデータを管理する配列(_cell)から消去
		//	・削除されたピースをDestory
		//	・マッチしたピースのリスト(_matchedPieces)をクリア
		private void DeletePieceInternal()
		{
			foreach (var piece in _matchedPieces)
			{
				Vector2Int cellPos = ConvertFromTransformToCell(piece.transform.position);
				_cell[cellPos.x, cellPos.y] = null; // データを削除
				Destroy(piece);
			}
			_matchedPieces.Clear();
		}

		//	ピース落下時の内部処理(ピース落下アニメーション終了時に呼ばれる)
		//	・ピースデータを管理する配列(_cell)に対して、落下されたピースを移動させる
		//	・ピースデータを管理する配列(_cell)に対して、移動前データをNULLクリア
		//	・落下したピースのリスト(__fallingPieces)をクリア
		private void FallingPieceInternal()
		{
			foreach (var (_, from, to) in _fallingPieces)
			{
				_cell[to.x, to.y] = _cell[from.x, from.y];
				_cell[from.x, from.y] = null;
			}
			_fallingPieces.Clear();
		}

		//	全ピースの移動確認チェック
		private bool AreAllPiecesSettled()
		{
			foreach (GameObject piece in _cell)
			{
				if (piece != null && piece.GetComponent<PieceView>().IsMoving())
				{
					return false;
				}
			}
			return true;
		}

		//	全ピースのフェード確認チェック
		private bool AreAllPiecesFadesCompleted()
		{
			foreach (GameObject piece in _cell)
			{
				if (piece != null && piece.GetComponent<PieceView>().IsFading())
				{
					return false;
				}
			}
			return true;
		}
#if false
#endif
	}
}
