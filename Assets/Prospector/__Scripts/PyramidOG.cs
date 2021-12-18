using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// An enum to handle all the possible scoring events
public enum ScoreEventOG
{
	draw,
	mine,
	gameWin,
	gameLoss
}

public class PyramidOG : MonoBehaviour {

	static public PyramidOG S;
	static public int SCORE_FROM_PREV_ROUND = 0;
	static public int HIGH_SCORE = 0;

	public float reloadDelay = 3f; // The delay between rounds
	public Text gameOverText, roundResultText, highScoreText;

	public Vector3 fsPosMid = new Vector3(0.5f, 0.90f, 0);
	public Vector3 fsPosRun = new Vector3(0.5f, 0.75f, 0);
	public Vector3 fsPosMid2 = new Vector3(0.5f, 0.5f, 0);
	public Vector3 fsPosEnd = new Vector3(1.0f, 0.65f, 0);

	[Header("Set in Inspector")]
	public TextAsset deckXML;
	public TextAsset layoutXML;
	public float xOffset = 3;
	public float yOffset = -2.5f;
	public Vector3 layoutCenter;


	[Header("Set Dynamically")]
	public DeckOG deckOG;
	public LayoutOG layoutOG;
	public List<CardPyramidOG> drawPile;
	public Transform layoutAnchor;
	public CardPyramidOG target;
	public List<CardPyramidOG> tableau;
	public List<CardPyramidOG> discardPile;

	// Fields to track score info
	public int chain = 0; // of cards in this run
	public int scoreRun = 0;
	public int score = 0;
	public FloatingScore fsRun;

	void Awake() {
		S = this;
		// Check for a high score in PlayerPrefs
		if (PlayerPrefs.HasKey("ProspectorHighScore"))
		{
			HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
		}
		// Add the score from last round, which will be >0 if it was a win
		score += SCORE_FROM_PREV_ROUND;
		// And reset the SCORE_FROM_PREV_ROUND
		SCORE_FROM_PREV_ROUND = 0;
		SetUpUITexts();
	}

	void SetUpUITexts()
	{
		GameObject go = GameObject.Find("HighScoreOG");
		string hScore = "High Score: " + UtilsOG.AddCommasToNumber(HIGH_SCORE);
		go.GetComponent<Text>().text = hScore;

		go = GameObject.Find("GameOverOG");
		if (go != null)
		{
			gameOverText = go.GetComponent<Text>();
		}

		go = GameObject.Find("RoundResultOG");
		if (go != null)
		{
			roundResultText = go.GetComponent<Text>();
		}

		ShowResultsUI(false);
	}

	void ShowResultsUI(bool show)
	{
		gameOverText.gameObject.SetActive(show);
		roundResultText.gameObject.SetActive(show);
	}

	void Start()
	{
		//ScoreboardOG.S.score = score;
		deckOG = GetComponent<DeckOG>();
		deckOG.InitDeck(deckXML.text);
		DeckOG.Shuffle(ref deckOG.cards);

		layoutOG = GetComponent<LayoutOG>();
		layoutOG.ReadLayout(layoutXML.text);

		drawPile = ConvertListCardsToListCardPyramid(deckOG.cards);
		LayoutGame();
	}

	List<CardPyramidOG>

		ConvertListCardsToListCardPyramid(List<Card> lCD) {
		List<CardPyramidOG> lCP = new List<CardPyramidOG>();
		CardPyramidOG tCP;
		foreach (Card tCD in lCD)
		{
			tCP = tCD as CardPyramidOG;
			lCP.Add(tCP);

		}
		return (lCP);
	}
	CardPyramidOG Draw()
	{
		CardPyramidOG cd = drawPile[0]; // Pull the 0th CardPyramidOG
		drawPile.RemoveAt(0); // Then remove it from List<> drawPile
		return (cd); // And return it
	}

	// Convert from the layoutID int to the CardPyramidOG with that ID
	CardPyramidOG FindCardByLayoutID(int layoutID)
	{
		foreach (CardPyramidOG tCP in tableau)
		{
			// Search through all cards in the tableau List<>
			if (tCP.layoutID == layoutID)
			{
				// If the card has the same ID, return it
				return (tCP);
			}
		}
		// If it's not found, return null
		return (null);
	}

	// LayoutGame() positions the initial tableau of cards, a.k.a. the "mine"
	void LayoutGame()
	{
		// Create an empty GameObject to serve as an anchor for the tableau //1
		if (layoutAnchor == null)
		{
			GameObject tGO = new GameObject("_LayoutAnchor");
			// ^ Create an empty GameObject named _LayoutAnchor in the Hierarchy
			layoutAnchor = tGO.transform; // Grab its Transform
			layoutAnchor.transform.position = layoutCenter; // Position it
		}

		CardPyramidOG cp;
		// Follow the layoutOG
		foreach (SlotDefOG tSD in layoutOG.slotDefs)
		{
			// ^ Iterate through all the SlotDefs in the layoutOG.slotDefs as tSD
			cp = Draw(); // Pull a card from the top (beginning) of the drawPile
			cp.faceUp = tSD.faceUp; // Set its faceUp to the value in SlotDefOG
			cp.transform.parent = layoutAnchor; // Make its parent layoutAnchor
			cp.transform.localPosition = new Vector3(
			layoutOG.multiplier.x * tSD.x,
			layoutOG.multiplier.y * tSD.y,
			-tSD.layerID);
			// ^ Set the localPosition of the card based on slotDef
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;
			cp.state = CardStateOG.tableau;
			// CardProspectors in the tableau have the state CardStateOG.tableau

			cp.SetSortingLayerName(tSD.layerName); // Set the sorting layers
			tableau.Add(cp); // Add this CardPyramidOG to the List<> tableau
		}

		foreach (CardPyramidOG tCP in tableau)
		{
			foreach (int hid in tCP.slotDef.hiddenBy)
			{
				cp = FindCardByLayoutID(hid);
				tCP.hiddenBy.Add(cp);
			}
		}
			// Set up the initial target card
		MoveToTarget(Draw());

		// Set up the Draw pile
		UpdateDrawPile();
	}

	// CardClicked is called any time a card in the game is clicked
	public void CardClicked(CardPyramidOG cd)
	{
		// The reaction is determined by the state of the clicked card
		switch (cd.state)
		{
			case CardStateOG.target:
				// Clicking the target card does nothing
				break;
			case CardStateOG.drawpile:
				// Clicking any card in the drawPile will draw the next card
				MoveToDiscard(target); // Moves the target to the discardPile
				MoveToTarget(Draw()); // Moves the next drawn card to the target
				UpdateDrawPile(); // Restacks the drawPile
				ScoreManager(ScoreEventOG.draw);
				break;
			case CardStateOG.tableau:
				// Clicking a card in the tableau will check if it's a valid play
				bool validMatch = true;
				// Highlights the card on the tableau
				if (!cd.faceUp)
				{
					// If the card is face-down, it's not valid
					validMatch = false;
				}
				if (!AddUptoThirteen(cd, target))
				{
					// If it's not an adjacent rank, it's not valid
					validMatch = false;
				}
				if (cd.rank == 13)
                {
					validMatch = true;
                }
				if (!validMatch) return; // return if not valid
										 // Yay! It's a valid card.
				tableau.Remove(cd); // Remove it from the tableau List
				MoveToTarget(cd); // Make it the target card
				SetTableauFaces(); // Update tableau card face-ups
                break;
		}
		// Check to see whether the game is over or not
		CheckForGameOver();
	}

	public bool AddUptoThirteen(CardPyramidOG c0, CardPyramidOG c1)
	{
		if ((c0.rank + c1.rank) == 13)
		{
			return (true);
		}
		return (false);
	}

	//public void CardClickedTableau(CardPyramidOG c0)
 //   {
	//	card1 = c0.tableau; 
	//	switch (card1)
 //       {
	//		case CardStateOG.tableau:
	//			if (AddUptoThirteen(c0, card1))
					
	//		tableau.Remove(c0); // Remove it from the tableau List
	//		tableau.Remove(c1);
	//		MoveToTarget(c0); // Make it the target card
	//		MoveToTarget(c1);
	//		SetTableauFaces(); // Update tableau card face-ups
	//			break;
	//	}

	//}

	// Moves the current target to the discardPile
	void MoveToDiscard(CardPyramidOG cd)
	{
		// Set the state of the card to discard
		cd.state = CardStateOG.discard;
		discardPile.Add(cd); // Add it to the discardPile List<>
		cd.transform.parent = layoutAnchor; // Update its transform parent
		cd.transform.localPosition = new Vector3(
		layoutOG.multiplier.x * layoutOG.discardPile.x,
		layoutOG.multiplier.y * layoutOG.discardPile.y,
		-layoutOG.discardPile.layerID + 0.5f);
		// ^ Position it on the discardPile
		cd.faceUp = true;
		// Place it on top of the pile for depth sorting
		cd.SetSortingLayerName(layoutOG.discardPile.layerName);
		cd.SetSortOrder(-100 + discardPile.Count);
	}

	// Make cd the new target card
	void MoveToTarget(CardPyramidOG cd)
	{
		// If there is currently a target card, move it to discardPile
		if (target != null) MoveToDiscard(target);
		target = cd; // cd is the new target
		cd.state = CardStateOG.target;
		cd.transform.parent = layoutAnchor;
		// Move to the target position
		cd.transform.localPosition = new Vector3(
		layoutOG.multiplier.x * layoutOG.discardPile.x,
		layoutOG.multiplier.y * layoutOG.discardPile.y,
		-layoutOG.discardPile.layerID);
		cd.faceUp = true; // Make it face-up
						  // Set the depth sorting
		cd.SetSortingLayerName(layoutOG.discardPile.layerName);
		cd.SetSortOrder(0);
	}

	// Arranges all the cards of the drawPile to show how many are left
	void UpdateDrawPile()
	{
		CardPyramidOG cd;
		// Go through all the cards of the drawPile
		for (int i = 0; i < drawPile.Count; i++)
		{
			cd = drawPile[i];
			cd.transform.parent = layoutAnchor;
			// Position it correctly with the layoutOG.drawPile.stagger
			Vector2 dpStagger = layoutOG.drawPile.stagger;
			cd.transform.localPosition = new Vector3(
			layoutOG.multiplier.x * (layoutOG.drawPile.x + i * dpStagger.x),
			layoutOG.multiplier.y * (layoutOG.drawPile.y + i * dpStagger.y),
			-layoutOG.drawPile.layerID + 0.1f * i);
			cd.faceUp = false; // Make them all face-down
			cd.state = CardStateOG.drawpile;
			// Set depth sorting
			cd.SetSortingLayerName(layoutOG.drawPile.layerName);
			cd.SetSortOrder(-10 * i);
		}
	}
	// Return true if the two cards are adjacent in rank (A & K wrap around)
	//public bool adjacentrank(cardpyramidog c0, cardpyramidog c1)
	//{
	//	 if either card is face-down, it's not adjacent.
	//	if (!c0.faceup || !c1.faceup) return (false);

	//	 if they are 1 apart, they are adjacent
	//	if (mathf.abs(c0.rank - c1.rank) == 1)
	//	{
	//		return (true);
	//	}
	//	 if one is a and the other king, they're adjacent
	//	if (c0.rank == 1 && c1.rank == 13) return (true);
	//	if (c0.rank == 13 && c1.rank == 1) return (true);

	//	 otherwise, return false
	//	return (false);
	//}

	// This turns cards in the Mine face-up or face-down
	void SetTableauFaces()
	{
		foreach (CardPyramidOG cd in tableau)
		{
			bool faceUp = true; // Assume the card will be face-up
			foreach (CardPyramidOG cover in cd.hiddenBy)
			{
				// If either of the covering cards are in the tableau
				if (cover.state == CardStateOG.tableau)
				{
					faceUp = false; // then this card is face-down
				}
			}
			cd.faceUp = faceUp; // Set the value on the card
		}
	}

	// Test whether the game is over
	void CheckForGameOver()
	{
		// If the tableau is empty, the game is over
		if (tableau.Count == 0)
		{
			// Call GameOver() with a win
			GameOver(true);
			return;
		}
		// If there are still cards in the draw pile, the game's not over
		if (drawPile.Count > 0)
		{
			return;
		}
		if (drawPile.Count == 0)
        {
			GameOver(true);
			return;
        }
		// Check for remaining valid plays
		foreach (CardPyramidOG cd in tableau)
		{
			if (AddUptoThirteen(cd, target))
			{
				// If there is a valid play, the game's not over
				return;
			}
		}
		// Since there are no valid plays, the game is over
		// Call GameOver with a loss
		GameOver(false);
	}

	// Called when the game is over. Simple for now, but expandable
	void GameOver(bool won)
	{
		if (won)
		{
			ScoreManager(ScoreEventOG.gameWin); 
		}
		else
		{
			ScoreManager(ScoreEventOG.gameLoss);
		}
		// Reload the scene in reloadDelay seconds
		// This will give the score a moment to travel
		Invoke("ReloadLevel", reloadDelay); //1

		//Application.LoadLevel("__Prospector_Scene_0");
	}

	void ReloadLevel()
	{
		// Reload the scene, resetting the game
		SceneManager.LoadScene("GameScene");
	}

	// ScoreManager handles all of the scoring
	void ScoreManager(ScoreEventOG sEvt)
	{
		List<Vector3> fsPts;
		switch (sEvt)
		{
			// Same things need to happen whether it's a draw, a win, or a loss
			case ScoreEventOG.draw: // Drawing a card
			case ScoreEventOG.gameWin: // Won the round
			case ScoreEventOG.gameLoss: // Lost the round
				chain = 0; // resets the score chain
				score += scoreRun; // add scoreRun to total score
				scoreRun = 0; // reset scoreRun
							  // Add fsRun to the _Scoreboard score
				if (fsRun != null)
				{
					// Create points for the Bezier curve
					fsPts = new List<Vector3>();
					fsPts.Add(fsPosRun);
					fsPts.Add(fsPosMid2);
					fsPts.Add(fsPosEnd);
					fsRun.reportFinishTo = ScoreboardOG.S.gameObject;
					fsRun.Init(fsPts, 0, 1);
					// Also adjust the fontSize
					fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
					fsRun = null; // Clear fsRun so it's created again
				}
				break;
			case ScoreEventOG.mine: // Remove a mine card
				chain++; // increase the score chain
				scoreRun += chain; // add score for this card to run
								   // Create a FloatingScore for this score
				FloatingScore fs;
				// Move it from the mousePosition to fsPosRun
				Vector3 p0 = Input.mousePosition;
				p0.x /= Screen.width;
				p0.y /= Screen.height;
				fsPts = new List<Vector3>();
				fsPts.Add(p0);
				fsPts.Add(fsPosMid);
				fsPts.Add(fsPosRun);
				fs = ScoreboardOG.S.CreateFloatingScore(chain, fsPts);
				fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
				if (fsRun == null)
				{
					fsRun = fs;
					fsRun.reportFinishTo = null;
				}
				else
				{
					fs.reportFinishTo = fsRun.gameObject;
				}
				break;
		}

		// This second switch statement handles round wins and losses
		switch (sEvt)
		{
			case ScoreEventOG.gameWin:
				gameOverText.text = "Round Over";
				// If it's a win, add the score to the next round
				// static fields are NOT reset by Application.LoadLevel()
				PyramidOG.SCORE_FROM_PREV_ROUND = score;
				//print("You won this round! Round score: " + score);
				roundResultText.text = "You won this round! \nRound Score: " + score;
				ShowResultsUI(true);
				break;
			case ScoreEventOG.gameLoss:
				gameOverText.text = "Game Over";
				if (PyramidOG.HIGH_SCORE <= score)
				{
					//print("You got the high score! High score: " + score);
					string str = "You got the high score! \nHigh score: " + score;
					roundResultText.text = str;
					PyramidOG.HIGH_SCORE = score;
					PlayerPrefs.SetInt("PyramidOGHighScore", score);
				}
				else
				{
					//print("Your final score for the game was: " + score);
					roundResultText.text = "Your final score was: " + score;
				}
				ShowResultsUI(true);
				break;
			default:
				//print("score: " + score + " scoreRun:" + scoreRun + " chain:" + chain);
				break;
		}
	}
}