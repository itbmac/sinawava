﻿using UnityEngine;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using MiniJSON;
using System.Linq;

public enum BoardState {Valid, Incomplete, Duplicates, InvalidPair, InvalidCounts}

public class BoardManager : MonoBehaviour {
	
	public enum BoardSelector {NoLoad, FirstChildBoard, RandomBoard}
	public BoardSelector boardSelector = BoardSelector.NoLoad;
	
	public float Size = 2.95f;
	public Vector2 OffsetPixel = new Vector2(.45f, -1.2f);	

	private int AdjustToInt(float f) {
		return Mathf.RoundToInt(.5f + (f / Size)) + 1;
	}	
	
	public GridCoord GetGridCoord(Vector2 pos) {
		pos -= OffsetPixel;
		
		if (pos.x < MacKenzieThreshold)
			pos.x += MacKenzieOffsetX;
		
		return new GridCoord(AdjustToInt(pos.x), AdjustToInt(pos.y));		
	}
	
	const float MacKenzieThreshold = -6.5f;
	const float MacKenzieOffsetX = 1.9f;
	const float MacKenzieOffsetY = 0.4f;
	
	public Vector2 GridCoord2Pos(GridCoord gc) {
		if (gc.x < 0)
			return new Vector2(
				Size * (gc.x - 1.5f) + OffsetPixel.x - MacKenzieOffsetX,
				Size * (gc.y - 1.5f) + OffsetPixel.y - MacKenzieOffsetY
			);
		else
			return new Vector2(
				Size * (gc.x - 1.5f) + OffsetPixel.x,
				Size * (gc.y - 1.5f) + OffsetPixel.y
			);
	}
	
	public GridCoord SnapGridCoord(GridCoord gc) {
		gc.x = Mathf.Clamp(gc.x, -3, 3);
		if (gc.x < 0) {
			gc.y = Mathf.Clamp(gc.y, 0, 3);

			if ((gc.y == 3) && (gc.x == -3))
				gc.y = 2;
		}
		else
			gc.y = Mathf.Clamp(gc.y, 0, 3);
		return gc;
	}
	
	public List<GridCoord> GetAllPossibleBoardPositions() {
		var result = new List<GridCoord>();
		
		for (int i = 0; i < 4; i++) {
			for (int j = 0; j < 4; j++) {
				result.Add(new GridCoord(i, j));
			}
		}
		
		return result;
	}
	
	public IEnumerable<GridCoord> GetAllPossibleSidePositions() {
		var result = new List<GridCoord>();
		
		for (int j = 0; j <= 2; j++)
			result.Add(new GridCoord(-3, j));
	
		for (int i = -2; i <= -1; i++) {
			for (int j = 0; j <= 3; j++) {
				result.Add(new GridCoord(i, j));
			}
		}
		
		return result;
	}
	
	public GameObject GetTileAtPosition(GridCoord pos, GameObject exceptObject) {
		GridCoord myGridCoord = SnapGridCoord(pos);
		// assumes only one tile at a position
		
		var tilesAtPosition = GameObject.FindGameObjectsWithTag("Tile").Where(
			tile => tile != exceptObject && GetGridCoord(tile.transform.position) == myGridCoord
		);
		
		return tilesAtPosition.FirstOrDefault();
	}
	
	public IEnumerable<GridCoord> FilterForOpenPositions(IEnumerable<GridCoord> positions, GameObject forObject) {
		// currently inefficient
		
		return positions.Where(x => IsPositionOpen(x.ToVector2(), forObject));
	}
	
	private Dictionary<GridCoord, GameObject> GetTileDictionary() {
		var result = new Dictionary<GridCoord, GameObject>();
		GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
		
		// does not account for more than one piece at a GridCoord
		foreach (GameObject tile in tiles)
			result[tile.transform.position.ToGridCoord()] = tile;
			
		return result;		
	}
	
	private static Vector2? FindClosestPosition(Vector2 pos, IEnumerable<Vector2> positions) {
		Vector2? bestPosition = null;
		float bestDistance = Mathf.Infinity;
				
		foreach (var boardPosition in positions) {
			float distance = Vector2.Distance(boardPosition, pos);
			
			if (distance < bestDistance) {
				bestDistance = distance;
				bestPosition = boardPosition;
			}
		}
		
		return bestPosition	;
	}
	
	public Vector2? FindClosestOpenPositionOnBoard(Vector2 pos, GameObject forObject) {
		return FindClosestPosition(pos, FilterForOpenPositions(GetAllPossibleBoardPositions(), forObject).Select(x => x.ToVector2()));
	}
	
	public Vector2? FindClosestOpenPositionOnSide(Vector2 pos, GameObject forObject) {
		return FindClosestPosition(pos, FilterForOpenPositions(GetAllPossibleSidePositions(), forObject).Select(x => x.ToVector2()));
	}	
	
	public bool IsOutOfGame(Vector2 pos) {
		var gc = GetGridCoord(pos);
		var gcs = SnapGridCoord(gc);
		
		return gc != gcs;
	}
	
	public bool IsOnBoard(Vector2 pos) {
		var gc = GetGridCoord(pos);
		return 0 <= gc.x && gc.x < 4 && 0 <= gc.y && gc.y < 4;
	}
	
	public Vector2 SnapPos(Vector2 pos) {
		return GridCoord2Pos(GetGridCoord(pos));
	}
	
	public Vector2 SnapPosConstrained(Vector2 pos) {
		return GridCoord2Pos(SnapGridCoord(GetGridCoord(pos)));
	}
	
	private int Classify(GameObject g) {
		string n = g.transform.parent.name;
		if (n == "Suspects")
			return 0;
		else if (n == "Locations")
			return 1;
		else if (n == "Victims")
			return 2;
		else if (n == "Weapons")
			return 3;
		
		Debug.LogError("unknown " + n);
		return 4;
	}
	
	GameObject[,] GetCurrentBoard() {
		GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
		GameObject[,] board = new GameObject[4, 4];
		
		foreach (GameObject tile in tiles) {
			GridCoord gc = GetGridCoord(tile.transform.position);
			int x = gc.x;
			int y = gc.y;
			if (x < 0 || x > 3 || y < 0 || y > 3) {
				continue;
			}
			
			if (board[x,y] != null) {
//				Debug.LogWarning("Overlapping tiles: " + board[x,y].name + " and " + tile.name);
				continue;
			}
			board[x,y] = tile;
		}
		
		return board;
	}
	
	public bool IsPositionOpen(Vector3 pos, GameObject forObject) {
		GridCoord myGridCoord = SnapGridCoord(GetGridCoord(pos));
		
		return GameObject.FindGameObjectsWithTag("Tile").All (
			tile => tile == forObject || GetGridCoord(tile.transform.position) != myGridCoord
		);
	}
	
	public BoardState GetCurrentBoardState() {
		GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
		GameObject[,] board = new GameObject[4, 4];
		
		foreach (GameObject tile in tiles) {
			GridCoord gc = GetGridCoord(tile.transform.position);
			int x = gc.x;
			int y = gc.y;
			if (x < 0 || x > 3 || y < 0 || y > 3) {
				continue;
			}
			
			if (board[x,y] != null) {
				return BoardState.Duplicates;
			}
			board[x,y] = tile;
		}
		
		return GetBoardState(board);
	}
	
	bool CheckCurrentBoard() {
		return GetCurrentBoardState() == BoardState.Valid;
	}
	
	public bool IsCurrentBoardFilled() {
		GameObject[,] board = GetCurrentBoard();
		foreach (var t in board) {
			if (t == null)
				return false;
		}
	
		return true;
	}
	
	void GenerateBoard() {
		GameObject[,] board = new GameObject[4,4];
		GenerateBoard(board, 0, 0);
	}
	
	string BoardToString(GameObject[,] board) {
		// Note: does not print out as the physical board appears (specifically, prints out upside down)
		StringBuilder builder = new StringBuilder();
		builder.Append("[");
		for (int j = 0; j < board.GetLength(1); j++) {
			builder.Append("[");
			for (int i = 0; i < board.GetLength(0); i++) {
				var e = board[i,j];
				builder.Append(e ? e.name : "_");
				if (i < board.GetLength(1) - 1)
					builder.Append(",\t");
			}
			builder.Append("]");
			if (j != board.GetLength(0) - 1) {
				builder.Append(",\n");
			}
		}
		builder.Append("]");
		return builder.ToString();
	}
	
	void PrintBoard(GameObject[,] board) {
		Debug.Log(board);
	}
	
	bool GenerateBoard(GameObject[,] board, int i, int j) {	
		if (j >= board.GetLength(1)) {
			i += 1;
			j = 0;
		}
		if (i >= board.GetLength(0)) {
			Debug.Log ("Generated new board for designer, press G for more: \n" + BoardToString(board));
			return true;
		}

		var d = new Dictionary<GameObject, bool>();
		foreach (var t in board) {
			if (t != null)
				d[t] = true;
		}

		var tilesRemaining = GameObject.FindGameObjectsWithTag("Tile").Where(x => !d.ContainsKey(x)).AsRandom();
		foreach (GameObject t in tilesRemaining) {
			board[i,j] = t;
			if (GetBoardState(board, true) == BoardState.Valid) {
				if (GenerateBoard(board, i, j + 1))
					return true;
			} 
		}
		
		board[i,j] = null;
		return false;
	}
	
	const int MAX_SOLUTIONS_TO_FIND = 1000;
	int CountSolutions() {
		GameObject[,] board = GetCurrentBoard();
		int result = CountSolutions(board, 0, 0);
		if (result > MAX_SOLUTIONS_TO_FIND)
			return MAX_SOLUTIONS_TO_FIND;
		return result;
	}
	
	int CountSolutions(GameObject[,] board, int i, int j) {	
		// Counts the number of possible solutions formed by filling in the remaining
		// open slots on the board. 
		
		if (j >= board.GetLength(1)) {
			i += 1;
			j = 0;
		}
		if (i >= board.GetLength(0)) {
			return 1;
		}
		
		if (board[i,j] != null)
			return CountSolutions(board, i, j + 1)	;
			
		
		var d = new Dictionary<GameObject, bool>();
		foreach (var t in board) {
			if (t != null)
				d[t] = true;
		}
		
		GameObject original = board[i,j];
		
		// TODO: not excluded
		var tilesRemaining =
			GameObject
			.FindGameObjectsWithTag("Tile")
			.Where(x => !d.ContainsKey(x) && !IsOnBoard(x.transform.position) && !IsOutOfGame(x.transform.position));
		int totalSolutions = 0;
		foreach (GameObject t in tilesRemaining) {
			board[i,j] = t;
			if (GetBoardState(board, true) == BoardState.Valid) {
				totalSolutions += CountSolutions(board, i, j + 1);
			} 
			
			// for performance reasons, if there's too many solutions break out of everything
			if (totalSolutions > MAX_SOLUTIONS_TO_FIND)
				break;
		}
		
		board[i,j] = original;
		return totalSolutions;
	}

	BoardState GetBoardState(GameObject[,] board, bool provisional = false) {
		IDictionary<GameObject, GridCoord> tileCoords = new Dictionary<GameObject, GridCoord>();
		
		for (int i = 0; i < board.GetLength(0); i++) {
			for (int j = 0; j < board.GetLength(1); j++) {
				var e = board[i,j];
				if (e == null) {
					if (!provisional)
						return BoardState.Incomplete;
				} else {
					tileCoords[e] = new GridCoord(i, j);
				}
			}
		}
	
		InvalidPair[] invalidPairs = gameObject.GetComponentsInChildren<InvalidPair>();
		foreach (InvalidPair invalidPair in invalidPairs) {
			GameObject someTile = invalidPair.SomeTile;
			if (!tileCoords.ContainsKey(someTile))
				continue;
			
			GridCoord pos = tileCoords[someTile];
			foreach (GameObject blockedTile in invalidPair.BlockedTiles) {
				if (!tileCoords.ContainsKey(blockedTile))
					continue;
			
				GridCoord blockedTilePos = tileCoords[blockedTile];
				
				if (pos.x == blockedTilePos.x || pos.y == blockedTilePos.y) {
//					Debug.Log ("Invalid pair: " + someTile.name + " and " + blockedTile.name);
					return BoardState.InvalidPair;
				}
			}
		}
		
		for (int x = 0; x < 4; x++) {
			int[] test = new int[4];
			int[] test2 = new int[4];
			
			for (int y = 0; y < 4; y++) {
				if (!provisional && (board[x,y] == null || board[y,x] == null)) {
					return BoardState.Incomplete;
				}
				
				if (board[x,y] != null)
					test[Classify(board[x,y])] += 1;
				if (board[y,x] != null)
					test2[Classify(board[y,x])] += 1;
			}
			
			if (!test.All(k => k == 1 || (provisional && k <= 1))) {
//				Debug.Log ("Fail col " + x);
//				Debug.Log (string.Join(",", test.Select(k => k.ToString()).ToArray()));
				return BoardState.InvalidCounts;
			}
			
			if (!test2.All(k => k == 1 || (provisional && k <= 1))) {
//				Debug.Log ("Fail row " + x);
//				Debug.Log (string.Join(",", test2.Select(k => k.ToString()).ToArray()));
				return BoardState.InvalidCounts;
			}
		}

		return BoardState.Valid;
	}
	
	public static BoardManager Instance;
	void Awake() {
		Instance = this;
	}
	
	List<int> randomBoards;
	int randomBoardIndex;
	
	public void NewBoard() {
		if (boardSelector == BoardSelector.FirstChildBoard || boardSelector == BoardSelector.RandomBoard) {
			Transform premadeBoards = transform.Find("PremadeBoards");
			if (premadeBoards && premadeBoards.childCount > 0) {
				int boardIndex = 0;
				if (boardSelector == BoardSelector.RandomBoard) {
					if (randomBoards == null || randomBoardIndex >= randomBoards.Count) {
						randomBoards = Enumerable.Range(0, premadeBoards.childCount).AsRandom().ToList();
						randomBoardIndex = 0;
					}
					
					boardIndex = randomBoards[randomBoardIndex];
					randomBoardIndex += 1;
				}
				
				Debug.Log ("Loading new board... " + boardIndex);
				LoadBoard(premadeBoards.GetChild(boardIndex).GetComponent<BoardData>().Data);
			} else {
				Debug.LogWarning("Could not find board to load!");
			}
		}
	}

	public void Reset() {
		Transform premadeBoards = transform.Find("PremadeBoards");
		int boardIndex = randomBoards[randomBoardIndex-1];
		LoadBoard(premadeBoards.GetChild(boardIndex).GetComponent<BoardData>().Data);
	}

	void Start () {
		Debug.Log ("Current board:\n" + DumpBoard());
		
		GenerateBoard();
		
		NewBoard();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.G))
			GenerateBoard();
//		else if (Input.GetKeyDown(KeyCode.S))
//			Reset ();
		else if (Input.GetKeyDown(KeyCode.C)) {
			int numSolutions = CountSolutions();
			if (numSolutions == MAX_SOLUTIONS_TO_FIND)
				Debug.Log(numSolutions + " or more possible solutions");
			else
				Debug.Log(numSolutions + " possible solutions");
		}
	}
	
	void LoadBoard(string board) {
		var tiles = Json.Deserialize(board) as Dictionary<string, object>;
		
		foreach (var pair in tiles) {
			string name = pair.Key;
			var pos = pair.Value as Dictionary<string, object>;
			var x = (double)pos["x"];
			var y = (double)pos["y"];
			
			var go = GameObject.Find(name);
			go.transform.position = new Vector2((float)x, (float)y);
			go.SendMessage("Reset");
		}
	}
	
	string DumpBoard() {
		GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
		var tilesDict = new Dictionary<string, object>();
		
		foreach (GameObject tile in tiles) {
			tilesDict[tile.name] = new Dictionary<string, double> {
				{"x", tile.transform.position.x},
				{"y", tile.transform.position.y},
			};
		}
		
		return Json.Serialize(tilesDict);
	}
}
