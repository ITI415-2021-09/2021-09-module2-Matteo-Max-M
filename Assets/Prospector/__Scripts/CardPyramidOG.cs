using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//To be pedagogical: this is an enum, which defines a type of variable that only has a few possible named values.
//The CardStateOG cariable type has one of four values: drawpile, tableau, target, & discard
public enum CardStateOG
{
	drawpile,
	tableau,
	target,
	discard
}

public class CardPyramidOG : Card
{ //Make sure CardPyramidOG extends Card
  //This is how you use the enum CardStateOG
	public CardStateOG state = CardStateOG.drawpile;

	//Thie hiddenBy list stores which other cards will keep this one face down
	public List<CardPyramidOG> hiddenBy = new List<CardPyramidOG>();

	//LayoutID matches this card to a LayoutOG XML id if it's a tableau card
	public int layoutID;

	//The SlotDefOG class stores information pulled in from the LayoutXML <slot>
	public SlotDefOG slotDef;

	// This allows the card to react to being clicked
	override public void OnMouseUpAsButton()
	{
		// Call the CardClicked method on the PyramidOG singleton
		PyramidOG.S.CardClicked(this);
		//PyramidOG.S.CardClickedTableau(this);
		// Also call the base class (Card.cs) version of this method
		base.OnMouseUpAsButton();
	}
}
