using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
	public enum GameState
	{
		WaitingForInput,    // ユーザー入力待ち
		SwapAnimating,      // ピースの入れ替えアニメーション
		Matching,           // マッチング判定
		DeleteFading,       //	消失時のフェーディングアニメーション
		Falling,            // ピースの落下処理
		FallingAnimating,
		Createing,
		CreatingAnimating,
		ResolvingChain,     // 連鎖の処理
	}
	private GameState currentState = GameState.WaitingForInput; // 初期状態
	private void changeGameState(GameState state)
	{
		currentState = state;
	}

	public int Width;
	public int Height;
	public Tile[] GrounPatterns;            // 背景タイルテクスチャパターン
	public GameObject[] PiecesPatterns;     // ピースパターン(prefab)

	private GameObject[,] m_Cell;           // セル(ピース)データの管理
	private Tilemap m_Tilemap;
	private Grid m_Grid;

	private Vector2 startPos;
	private Vector2 endPos;
	private GameObject selectedPiece;

	private Vector2Int[] m_SwapIndex;           //	ピースを交換するインデックス(m_cellの位置。[-1, -1]で未設定)
	private List<GameObject> m_MatchedPieces;   //	マッチしたピース(GameObject)のリスト	
	private List<(GameObject piece, Vector2Int from, Vector2Int to)> fallingPieces = new List<(GameObject, Vector2Int, Vector2Int)>();
	private List<(GameObject piece, Vector2Int from, Vector2Int to)> createPieces = new List<(GameObject, Vector2Int, Vector2Int)>();
	private List<(GameObject piece, Vector2Int from, Vector2Int to)> movingPieces = new List<(GameObject, Vector2Int, Vector2Int)>();

	private void SetSwapIndex(Vector2Int index0, Vector2Int index1)
	{
		m_SwapIndex[0] = index0;
		m_SwapIndex[1] = index1;
	}
	private void ClearSwapIndex()
	{
		SetSwapIndex(new Vector2Int(-1, -1), new Vector2Int(-1, -1));
	}

	// transform.position <=> m_cellインデックス変換
	public Vector2Int ConvertFromTransformToCell(Vector3 transformposition)
	{
		Vector3Int cellPosition = m_Grid.WorldToCell(transformposition);
		return new Vector2Int(cellPosition.x, cellPosition.y);
	}
	public Vector3 ConvertFromCellToTransform(Vector2Int cellindex)
	{
		return m_Grid.GetCellCenterWorld(new Vector3Int(cellindex.x, cellindex.y, 0));
	}

	private void AddPieceToGrid(int x, int y)
	{
		int pieceIndex = UnityEngine.Random.Range(0, PiecesPatterns.Length);
		GameObject cell = Instantiate(PiecesPatterns[pieceIndex], m_Grid.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
		cell.AddComponent<PieceView>(); // PieceView を自動でアタッチ
		m_Cell[x, y] = cell;
	}

	// 横・縦方向にピースをチェック
	public List<GameObject> PieceMatchCheck()
	{
		HashSet<GameObject> matchedPieces = new HashSet<GameObject>(); // 重複を防ぐため HashSet を使用

		// 横方向のチェック
		for (int y = 0; y < Height; y++)
		{
			int matchCount = 1; // 連続しているピースの数
			List<GameObject> tempMatches = new List<GameObject>();
			tempMatches.Add(m_Cell[0, y]);

			for (int x = 1; x < Width; x++)
			{
				if (m_Cell[x, y] != null && m_Cell[x - 1, y] != null &&
					m_Cell[x, y].tag == m_Cell[x - 1, y].tag)
				{
					matchCount++;
					tempMatches.Add(m_Cell[x, y]);
				}
				else
				{
					if (matchCount >= 3) // 3つ以上並んでいたら追加
					{
						foreach (var piece in tempMatches)
							matchedPieces.Add(piece);
					}
					matchCount = 1;
					tempMatches.Clear();
					tempMatches.Add(m_Cell[x, y]);
				}
			}

			if (matchCount >= 3) // ループ終了時もチェック
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
			tempMatches.Add(m_Cell[x, 0]);

			for (int y = 1; y < Height; y++)
			{
				if (m_Cell[x, y] != null && m_Cell[x, y - 1] != null &&
					m_Cell[x, y].tag == m_Cell[x, y - 1].tag)
				{
					matchCount++;
					tempMatches.Add(m_Cell[x, y]);
				}
				else
				{
					if (matchCount >= 3)
					{
						foreach (var piece in tempMatches)
							matchedPieces.Add(piece);
					}
					matchCount = 1;
					tempMatches.Clear();
					tempMatches.Add(m_Cell[x, y]);
				}
			}

			if (matchCount >= 3)
			{
				foreach (var piece in tempMatches)
					matchedPieces.Add(piece);
			}
		}

		return new List<GameObject>(matchedPieces);
	}


	private void pieceDeleteInternal()
	{
		foreach (var piece in m_MatchedPieces)
		{
			Vector2Int cellPos = ConvertFromTransformToCell(piece.transform.position);
			m_Cell[cellPos.x, cellPos.y] = null; // データを削除
			Destroy(piece);
		}
		m_MatchedPieces.Clear();
	}
	public void PieceMatchCheckUpdate()
	{
		m_MatchedPieces = PieceMatchCheck();
		Debug.Log("MatchPiece Count: " + m_MatchedPieces.Count);
		if (m_MatchedPieces.Count != 0)
		{
			// マッチしたピースを削除し、m_Cellを更新
			foreach (var piece in m_MatchedPieces)
			{
				// ピースのフェードアウト開始
				piece.GetComponent<PieceView>().SetFading(PieceView.FadeState.FadingOut);
			}
			changeGameState(GameState.DeleteFading);
		}
		else
		{
			changeGameState(GameState.WaitingForInput);
		}
	}

	private void pieceFallingInternal()
	{
		foreach (var (piece, from, to) in fallingPieces)
		{
			m_Cell[to.x, to.y] = m_Cell[from.x, from.y];
			m_Cell[from.x, from.y] = null;
		}
		fallingPieces.Clear();
	}

	private void HandleFallingPieces()
	{
		fallingPieces.Clear();
		createPieces.Clear();

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++) // 下から順に処理する
			{
				// 空のセルがある or すでに落下したセル の場合
				if ((m_Cell[x, y] == null) || fallingPieces.Any(fp => fp.from == new Vector2Int(x, y)))
				{
					int fallDistance = 0; // 落下距離
					int aboveY;

					// 上から最も遠いピースを探す
					for (aboveY = y + 1; aboveY < Height; aboveY++)
					{
						if ((m_Cell[x, aboveY] != null) && (!fallingPieces.Any(fp => fp.from == new Vector2Int(x, aboveY))))
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
						m_Cell[x, aboveY].GetComponent<PieceView>().MoveTo(ConvertFromCellToTransform(new Vector2Int(x, y)));
						fallingPieces.Add((m_Cell[x, aboveY], new Vector2Int(x, aboveY), new Vector2Int(x, y)));
					}
				}
			}
		}
		/*
				for (int x = 0; x < Width; x++)
				{
					for (int y = 0; y < Height; y++)
					{
						if (m_Cell[x, y] == null) // 空のセルがある場合
						{
							for (int aboveY = y + 1; aboveY < Height; aboveY++)
							{
								if (m_Cell[x, aboveY] != null)
								{
									// ピースの移動開始
									m_Cell[x, aboveY].GetComponent<PieceView>().MoveTo(ConvertFromCellToTransform(new Vector2Int(x, y)));
									//	fallingPieces.Add((m_Cell[x, aboveY], new Vector2Int(x, aboveY), new Vector2Int(x, y)));
									//							piece.GetComponent<PieceView>().MoveTo(otherPiece.transform.position);
									//							otherPiece.GetComponent<PieceView>().MoveTo(piece.transform.position);
									//							m_Cell[x, y] = m_Cell[x, aboveY];
									//							m_Cell[x, aboveY] = null;
									break;
								}
							}
						}
					}
				}
		*/

		changeGameState(GameState.FallingAnimating);
		/*
					// 新しいピースを補充
					int numAddPiece = 0;
					for (int y = 0; y < Height; y++)
					{
						if (m_Cell[x, y] == null)
						{
							AddPieceToGrid(x, y);
							createPieces.Add((m_Cell[x, y], new Vector2Int(x, Height + numAddPiece), new Vector2Int(x, y)));
							m_Cell[x, y].GetComponent<Renderer>().enabled = false;
							numAddPiece++;
						}
					}
				}

				// すべての落下処理をアニメーションで実行
				yield return StartCoroutine(AnimateFallingPieces());
				Debug.Log("update cell function end.");
		*/
	}

	private void HandleCreatingPieces()
	{
		createPieces.Clear();

		// 新しいピースを補充
		for (int x = 0; x < Width; x++)
		{
			int numAddPiece = 0;
			for (int y = 0; y < Height; y++)
			{
				if (m_Cell[x, y] == null)
				{
					// 生成時は、描画を待たずにこの段階でm_CellにGameObjectを生成する
					AddPieceToGrid(x, y);
					createPieces.Add((m_Cell[x, y], new Vector2Int(x, Height + numAddPiece), new Vector2Int(x, y)));
					m_Cell[x, y].GetComponent<PieceView>().transform.position = ConvertFromCellToTransform(new Vector2Int(x, Height + numAddPiece));
					m_Cell[x, y].GetComponent<PieceView>().MoveTo(ConvertFromCellToTransform(new Vector2Int(x, y)));
					//	m_Cell[x, y].GetComponent<Renderer>().enabled = false;
					numAddPiece++;
				}
			}
		}
		changeGameState(GameState.CreatingAnimating);
	}

#if false
    public IEnumerator PieceMatchCheckUpdate()
    {
        List<GameObject> matchedPieces;
        do
        {
            matchedPieces = PieceMatchCheck();
            Debug.Log("MatchPiece Start: " + matchedPieces.Count);

            // マッチしたピースを削除し、m_Cellを更新
            foreach (var piece in matchedPieces)
            {
                Vector2Int cellPos = ConvertFromTransformToCell(piece.transform.position);
                m_Cell[cellPos.x, cellPos.y] = null; // データを削除

                // ピースのアニメーションを開始
                piece.GetComponent<PieceView>().PlayDestroyAnimation(() =>
                {
                    Destroy(piece);
                });
            }

            // 落下処理を待つ
            yield return StartCoroutine(updateCell());

        } while (matchedPieces.Count > 0);

        Debug.Log("PieceMatchCheckUpdate() END");
    }

    private IEnumerator updateCell()
    {
        fallingPieces.Clear();
        createPieces.Clear();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (m_Cell[x, y] == null) // 空のセルがある場合
                {
                    for (int aboveY = y + 1; aboveY < Height; aboveY++)
                    {
                        if (m_Cell[x, aboveY] != null)
                        {
                            fallingPieces.Add((m_Cell[x, aboveY], new Vector2Int(x, aboveY), new Vector2Int(x, y)));
                            m_Cell[x, y] = m_Cell[x, aboveY];
                            m_Cell[x, aboveY] = null;
                            break;
                        }
                    }
                }
            }

            // 新しいピースを補充
            int numAddPiece = 0;
            for (int y = 0; y < Height; y++)
            {
                if (m_Cell[x, y] == null)
                {
                    AddPieceToGrid(x, y);
                    createPieces.Add((m_Cell[x, y], new Vector2Int(x, Height + numAddPiece), new Vector2Int(x, y)));
                    m_Cell[x, y].GetComponent<Renderer>().enabled = false;
                    numAddPiece++;
                }
            }
        }

        // すべての落下処理をアニメーションで実行
        yield return StartCoroutine(AnimateFallingPieces());
        Debug.Log("update cell function end.");
    }

    private IEnumerator AnimateFallingPieces()
    {
        List<Coroutine> fallCoroutines = new List<Coroutine>();

        foreach (var (piece, from, to) in fallingPieces)
        {
            // ピースの移動アニメーションを開始
            Coroutine fallCoroutine = StartCoroutine(piece.GetComponent<PieceView>().MoveToWithCallback(ConvertFromCellToTransform(to)));
            fallCoroutines.Add(fallCoroutine);
        }

        // すべての落下アニメーションが終わるのを待つ
        foreach (var coroutine in fallCoroutines)
        {
            yield return coroutine;
        }

        Debug.Log("AnimateFallingPieces 完了！");
    }

    private IEnumerator AnimateMovingPieces()
    {
        List<Coroutine> moveCoroutines = new List<Coroutine>();

        foreach (var (piece, from, to) in movingPieces)
        {
            // ピースの移動アニメーションを開始
            Coroutine moveCoroutine = StartCoroutine(piece.GetComponent<PieceView>().MoveToWithCallback(ConvertFromCellToTransform(to)));
            moveCoroutines.Add(moveCoroutine);
        }

        // すべての落下アニメーションが終わるのを待つ
        foreach (var coroutine in moveCoroutines)
        {
            yield return coroutine;
        }

        Debug.Log("AnimateMovingPieces 完了！");
    }
#endif

	//  ------------    (リファクタ済)  ------------
	//	全ピースの移動確認チェック
	private bool AreAllPiecesSettled()
	{
		foreach (GameObject piece in m_Cell)
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
		foreach (GameObject piece in m_Cell)
		{
			if (piece != null && piece.GetComponent<PieceView>().IsFading())
			{
				return false;
			}
		}
		return true;
	}

	//	ピース交換で発生する内部情報を更新する
	private void SwapPiecesInternal()
	{
		GameObject temp = m_Cell[m_SwapIndex[0].x, m_SwapIndex[0].y];
		m_Cell[m_SwapIndex[0].x, m_SwapIndex[0].y] = m_Cell[m_SwapIndex[1].x, m_SwapIndex[1].y];
		m_Cell[m_SwapIndex[1].x, m_SwapIndex[1].y] = temp;
		ClearSwapIndex();
	}

	//  タップ / クリックしたピースを取得
	private void SwapPieces(GameObject piece, Vector2 swipeDirection)
	{
		float swipeThreshold = 0.5f;
		Vector2Int direction = Vector2Int.zero;
		// 入れ替え判定
		if (Mathf.Abs(swipeDirection.x) > swipeThreshold || Mathf.Abs(swipeDirection.y) > swipeThreshold)
		{
			direction = (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y)) ?
				(swipeDirection.x > 0 ? Vector2Int.right : Vector2Int.left) :
				(swipeDirection.y > 0 ? Vector2Int.up : Vector2Int.down);
			Debug.Log($"Direction: {direction}");
			//  入れ替え先にピースが存在するか？
			Vector2 targetPosition = (Vector2)piece.transform.position + (Vector2)direction;
			RaycastHit2D hit = Physics2D.Raycast(targetPosition, Vector2.zero);
			if (hit.collider != null)
			{
				GameObject otherPiece = hit.collider.gameObject;    //  入れ替え先のピースオブジェクト
																	//  m_cellのインデックスを算出 -> 内部データ(m_Cell)更新用に保持
				SetSwapIndex(ConvertFromTransformToCell(piece.transform.position), ConvertFromTransformToCell(otherPiece.transform.position));

				// ピースの移動開始
				piece.GetComponent<PieceView>().MoveTo(otherPiece.transform.position);
				otherPiece.GetComponent<PieceView>().MoveTo(piece.transform.position);
			}
		}
	}

	private void HandlePieceSelection()
	{
		// 入力位置の取得 -> スクリーン座標をワールド座標に変換
		Vector2 inputPos = Touchscreen.current?.primaryTouch.position.ReadValue() ?? Mouse.current.position.ReadValue();
		Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPos);

		if (selectedPiece == null)
		{
			// 1つ目のピース選択

			//  2D用の Raycast を使用(「指定した座標にオブジェクトがあるか」調べる)
			//  Physics2D.Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask);
			//  origin	Ray の開始位置（ワールド座標）
			//  direction	Ray の飛ぶ方向（通常は Vector2.zero で「その点」をチェック）
			//  distance	Ray の距離（通常は Mathf.Infinity）
			//  layerMask	当たり判定のレイヤー（~0 なら全レイヤー）
			RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
			if (hit.collider != null)
			{
				selectedPiece = hit.collider.gameObject;
				Debug.Log($"選択されたピース: {selectedPiece.name}");
			}
		}
		else
		{
			// 2つ目のピース(入れ替え先)を選択
			Vector2 selectedPiece2D = new Vector2(selectedPiece.transform.position.x, selectedPiece.transform.position.y);
			Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);
			Vector2 swipeDirection = worldPos2D - selectedPiece2D;
			Debug.Log($"スワップ方向: {swipeDirection}");
			SwapPieces(selectedPiece, swipeDirection);
			selectedPiece = null; // 選択解除

			changeGameState(GameState.SwapAnimating);
		}
	}

	private void CheckUserInput()
	{
		//  タッチ(タップ) or クリックを検出
		//  ?? false → タッチデバイスが存在しない場合は false にする
		bool isTouchStart = Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false;
		bool isTouchEnd = Touchscreen.current?.primaryTouch.press.wasReleasedThisFrame ?? false;
		bool isClickStart = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
		bool isClickEnd = Mouse.current?.leftButton.wasReleasedThisFrame ?? false;

		if (isTouchStart || isClickStart || isTouchEnd || isClickEnd)
		{
			HandlePieceSelection();
		}
	}

	//--------  Lifecycle Methods   --------
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	// グリッド (m_Cell) の初期化
	// 背景 (Tilemap) の生成
	// ピース (AddPieceToGrid()) の配置
	void Start()
	{
		m_Tilemap = GetComponentInChildren<Tilemap>();
		m_Grid = GetComponentInChildren<Grid>();
		m_Cell = new GameObject[Width, Height];
		m_SwapIndex = new Vector2Int[2];

		for (int y = 0; y < Height; ++y)
		{
			for (int x = 0; x < Width; ++x)
			{
				//　背景タイルを生成
				int groundIndex = UnityEngine.Random.Range(0, GrounPatterns.Length);
				m_Tilemap.SetTile(new Vector3Int(x, y, 0), GrounPatterns[groundIndex]);

				// ピースを生成し登録
				AddPieceToGrid(x, y);
			}
		}
	}

	// Update is called once per frame
	void Update()
	{
		GameState tempState = currentState;
		switch (currentState)
		{
			case GameState.WaitingForInput:
				CheckUserInput();
				break;

			case GameState.SwapAnimating:
				if (AreAllPiecesSettled())
				{
					SwapPiecesInternal();
					changeGameState(GameState.Matching);
				}
				break;

			case GameState.Matching:
				PieceMatchCheckUpdate();
				break;
			case GameState.DeleteFading:
				if (AreAllPiecesFadesCompleted())
				{
					pieceDeleteInternal();
					changeGameState(GameState.Falling);
				}
				break;

			case GameState.Falling:
				HandleFallingPieces();
				break;
			case GameState.FallingAnimating:
				if (AreAllPiecesSettled())
				{
					pieceFallingInternal();
					changeGameState(GameState.Createing);

					/*
									for (int y = 0; y < Height; ++y)
									{
										for (int x = 0; x < Width; ++x)
										{
											if (m_Cell[x, y] == null)
											{
												Debug.Log("NULL :(" + x + "," + y + ")");
											}
										}
									}
					*/
				}
				break;

			case GameState.Createing:
				HandleCreatingPieces();
				break;

			case GameState.CreatingAnimating:
				if (AreAllPiecesSettled())
				{
					changeGameState(GameState.Matching);
					for (int y = 0; y < Height; ++y)
					{
						for (int x = 0; x < Width; ++x)
						{
							if (m_Cell[x, y] == null)
							{
								Debug.Log("NULL :(" + x + "," + y + ")");
							}
						}
					}
				}
				break;

			case GameState.ResolvingChain:
				/*
						if (AreAllPiecesSettled())
						{
							StartCoroutine(PieceMatchCheckUpdate());
						}
						*/
				break;
		}

		if (currentState != tempState)
		{
			Debug.Log("Change State: " + tempState + "=>" + currentState);
		}
	}

	void LateUpdate()
	{
		//Debug.Log("currentState: " + currentState);
		switch (currentState)
		{
			case GameState.WaitingForInput:
				break;

			case GameState.Matching:
				break;

			case GameState.SwapAnimating:
				//StartCoroutine(AnimateMovingPieces());
				break;
			case GameState.Falling:
				//        StartCoroutine(updateCell());
				break;

			case GameState.ResolvingChain:
				/*
						if (AreAllPiecesSettled())
						{
							StartCoroutine(PieceMatchCheckUpdate());
						}
						*/
				break;
		}
	}

}
