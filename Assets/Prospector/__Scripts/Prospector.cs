using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum ScoreEvent
{
	draw,
	mine,
	mineGold,
	gameWin,
	gameLoss
}
public class Prospector : MonoBehaviour {

	static public Prospector 	S;
	static public int SCORE_FROM_PREV_ROUND = 0;
	static public int HIGH_SCORE = 0;

	public float reloadDelay = 3f;
	//public text gameOverText, roundResultText, highScoreText;

    public Vector3 fsPosMid = new Vector3(0.5f, 0.90f, 0);
	public Vector3 fsPosRun = new Vector3(0.5f, 0.75f, 0);
	public Vector3 fsPosMid2 = new Vector3(0.5f, 0.5f, 0);
	public Vector3 fsPosEnd = new Vector3(1.0f, 0.65f, 0);




	[Header("Set in Inspector")]
	public TextAsset			deckXML;
	public TextAsset            layoutXML;
	public float                xOffset = 3;
	public float                yOffset = -2.5f;
	public Vector3              layoutCenter;

	[Header("Set Dynamically")]
	public Deck					deck;
	public Layout layout;
	public List<CardProspector> drawPile;
	public Transform layoutAnchor;
	public CardProspector target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;

	void Awake(){
		S = this;
	}

	void Start() {
		deck = GetComponent<Deck> ();
		deck.InitDeck (deckXML.text);
		Deck.Shuffle(ref deck.cards);
		Card c;
		for (int cNum = 0; cNum < deck.cards.Count; cNum++) {                  
			c = deck.cards[cNum];
			c.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);
		}
	}

}
