﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenBoard : MonoBehaviour {

	public GameObject B_Tile;
	public GameObject R_Tile;

	public GameObject R_Piece;
	public GameObject B_Piece;

	public Pieces[,] Pieces = new Pieces[8,8];
	public Pieces selectedPiece;
	public Vector2 mouseOver;
	public Vector2 startDrag;
	public Vector2 endDrag;
	public List<Pieces> forcedPieces = new List<Pieces>();
	public bool isRedTurn;
	public bool isRed;
	public bool hasCap; 

	// Use this for initialization
	void Start () {
		GenerateBoard ();
		isRedTurn = true;
	}
	void Update(){
		UpdateMouseOver ();
		if (isRed ? isRedTurn : !isRedTurn) {
			
			if (selectedPiece != null) {
				UpdatePieceDrag (selectedPiece);
			}
			int x = (int)mouseOver.x;
			int y = (int)mouseOver.y;
			if (Input.GetMouseButtonDown (0)) {
				SelectPiece (x, y);
			}
			if (Input.GetMouseButtonUp (0)) {
				TryMove ((int)(startDrag.x), (int)(startDrag.y), x, y);
			}
		}
		if (isRed==false && isRedTurn==false) {
			//needs to get the move from minmax and piece then move the piece to position 
			List<Pieces> chose = PossMove();
			Pieces chosenOne = chose[gameObject.GetComponent<Evalution>().MinMax(3, this, -9999, 9999, true)]; //piece chosen
			Vector2 finalMove = endDrag; // insert endDrag given by minmax
			TryMove((int)chosenOne.transform.position.x, (int)chosenOne.transform.position.y, (int)finalMove.x, (int)finalMove.y);
		}
	}
	//Use ray
	void UpdateMouseOver(){ //allows click from camera
		if(!Camera.main){
			Debug.Log ("not available");
		}
		RaycastHit hit;
		if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, 25f, LayerMask.GetMask ("Board"))) {
			mouseOver.x = (int)hit.point.x;
			mouseOver.y = (int)hit.point.z;
		} else {
			mouseOver.x = -1;
			mouseOver.y = -1;
		}
	}

	void UpdatePieceDrag(Pieces p){
		if(!Camera.main){
			Debug.Log ("not available");
		}
		RaycastHit hit;
		if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, 25f, LayerMask.GetMask ("Board"))) {
			p.transform.position = hit.point + Vector3.up;
		} 
	}
	public void SelectPiece(int x, int y){
		if (x < 0 || x >= 8 || y < 0 || y >= 8) {
			return;
		}
		Pieces p = Pieces [x, y];
		if (p != null && p.isRed == isRed) {
			if (forcedPieces.Count == 0) {
				selectedPiece = p;
				startDrag = mouseOver;
			} else {
				if (forcedPieces.Find (fp => fp == p) == null)
					return;
				selectedPiece = p;
				startDrag = mouseOver;
			}
		}
	}
	public void pseudoMove (Pieces p, Vector2 v) {
		MovePiece (p, (int)v.x, (int)v.y);
	}

	public void undoMove (Pieces p, Vector2 v) {
		MovePiece (p, (int)v.x, (int)v.y);
	}

	public void TryMove(int x1, int y1, int x2, int y2){
		forcedPieces = PossMove ();
		startDrag = new Vector2 (x1, y1);
		endDrag = new Vector2 (x2, y2);
		selectedPiece = Pieces [x1, y1];

		//out of bounds check
		MovePiece (selectedPiece, x2, y2);
		if (x2 < 0 || x2 >= 8 || y2 < 0 || x2 >= 8) {
			if (selectedPiece != null) {
				MovePiece (selectedPiece, x1, y1);
			}
			selectedPiece = null;
			startDrag = Vector2.zero;
			return;
		}
		if (selectedPiece != null) {
			if (endDrag == startDrag) {
				MovePiece (selectedPiece, x1, y1);
				selectedPiece = null;
				startDrag = Vector2.zero;
				return;
			}
			if (selectedPiece.ValidMove (Pieces, x1, y1, x2, y2)) {
				if (Mathf.Abs (x2 - x1) == 2) {
					Pieces p = Pieces [(x1 + x2) / 2, (y1 + y2) / 2];
					if (p != null) {
						Pieces [(x1 + x2) / 2, (y1 + y2) / 2] = null;
						DestroyImmediate (p.gameObject);
						hasCap = true;
					}
				}

				if (forcedPieces.Count != 0&&!hasCap) {
					MovePiece (selectedPiece, x1, y1);
					selectedPiece = null;
					startDrag = Vector2.zero;
					return;
				}
				Pieces [x2, y2] = selectedPiece;
				Pieces [x1, y1] = null;
				MovePiece (selectedPiece, x2, y2);

				EndTurn ();
			} else {
				MovePiece (selectedPiece, x1, y1);
				selectedPiece = null;
				startDrag = Vector2.zero;
				return;
			}
		}

	}
	public void EndTurn(){
		int x = (int)endDrag.x;
		int y = (int)endDrag.y;

		if (selectedPiece != null && selectedPiece.isRed && !selectedPiece.isKing && y == 7) {
			selectedPiece.isKing = true;
			selectedPiece.transform.Rotate (180, 0, 0);
			selectedPiece.tag = "RedK";
		} else if (selectedPiece != null && !selectedPiece.isRed && !selectedPiece.isKing && y == 0) {
			selectedPiece.isKing = true;
			selectedPiece.transform.Rotate (180, 0, 0);
			selectedPiece.tag = "BlackK";
		}
		selectedPiece = null;
		startDrag = Vector2.zero;

		if (PossMove (selectedPiece, x, y).Count != 0 && hasCap) {
			return;
		}

		isRed = !isRed;
		isRedTurn = !isRedTurn;
		hasCap = false;
		checkVictory ();
	}
	public void checkVictory(){
		Pieces[] piece = FindObjectsOfType<Pieces> ();
		bool hasRed = false; 
		bool hasBlack = false;
		for (int i = 0; i < piece.Length; i++) {
			if (piece [i].isRed) {
				hasRed = true;
			} else {
				hasBlack = true;
			}
		}
		if (!hasRed) {
			victory (false);
		}
		if (!hasBlack) {
			victory (true);
		}
	}
	public void victory(bool isRed){
		if (isRed) {
			Debug.Log ("you win");
		}
		if (!isRed) {
			Debug.Log("you lose");
		}
	}
	public List<Pieces> PossMove(Pieces p, int x, int y){
		forcedPieces = new List<Pieces> ();

		if (Pieces [x, y].isForceMove (Pieces, x, y)) {
			Debug.Log (x + " +" + y);
			forcedPieces.Add (Pieces [x, y]);
				}

		return forcedPieces;
	}

	public List<Pieces> PossMove(){
		forcedPieces = new List<Pieces>();

		for (int i = 0; i < 8; i++) {
			for (int j = 0; j < 8; j++) {
				if (Pieces [i, j] != null && Pieces [i, j].isRed == isRedTurn) {
					if (Pieces [i, j].isForceMove (Pieces, i, j)) {
						forcedPieces.Add (Pieces [i, j]);
					}
				}
			}
		}
		return forcedPieces;
	}
	public void MovePiece(Pieces p, int x, int y){
		Debug.Log (x + ", " + y);
		p.transform.position = (Vector3.right * x) + (Vector3.forward * y);
	}
	public void GenerateBoard(){ //generate board and pieces
		for (int x = 0; x < 8; x++) {
			if (x % 2 == 0) {
				for (int y = 0; y < 8; y++) {
					//generate tiles
					if (y % 2 == 0) {
						GameObject Bt = Instantiate (B_Tile, new Vector3 (x, 0, y), Quaternion.identity)as GameObject;
						Bt.transform.SetParent (this.transform);
					}
					if (y % 2 == 1) {
						GameObject Rt = Instantiate (R_Tile, new Vector3 (x, 0, y), Quaternion.identity)as GameObject;
						Rt.transform.SetParent (this.transform);
						//generate pieces
						if (y == 1) {
							GameObject cool = Instantiate (R_Piece, new Vector3 (x, 0.1f, y), Quaternion.identity) as GameObject;
							cool.transform.SetParent (transform);
							Pieces p = cool.GetComponent<Pieces> ();
							Pieces [x, y] = p;
						}
						if (y>4) {
							GameObject cool = Instantiate (B_Piece, new Vector3 (x, 0.1f, y), Quaternion.identity) as GameObject;
							cool.transform.SetParent (transform);
							Pieces p = cool.GetComponent<Pieces> ();
							Pieces [x, y] = p;
						}
					}
				}
			}
			if (x % 2 == 1) {
				for (int y = 0; y < 8; y++) {
					//generate tiles
					if (y % 2 == 0) {
						GameObject Rt = Instantiate (R_Tile, new Vector3 (x, 0, y), Quaternion.identity) as GameObject;
						Rt.transform.SetParent (this.transform);
						//generate pieces
						if (y < 3) {
							GameObject cool = Instantiate (R_Piece, new Vector3 (x, 0.1f, y), Quaternion.identity) as GameObject;
							cool.transform.SetParent (transform);
							Pieces p = cool.GetComponent<Pieces> ();
							Pieces [x, y] = p;
						}
						if (y == 6) {
							GameObject cool  = Instantiate (B_Piece, new Vector3 (x, 0.1f, y), Quaternion.identity) as GameObject;
							cool.transform.SetParent (transform);
							Pieces p = cool.GetComponent<Pieces> ();
							Pieces [x, y] = p;
						}
					}
					if (y % 2 == 1) {
						GameObject Bt = Instantiate (B_Tile, new Vector3 (x, 0, y), Quaternion.identity) as GameObject;
						Bt.transform.SetParent (this.transform);
					}
				}
			}
		}
	}
}

