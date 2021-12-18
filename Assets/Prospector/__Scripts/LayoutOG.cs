using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//The SlotDefOG class is not a subclass of Monobehavior, so it doesn't need a separate C# file
[System.Serializable]
public class SlotDefOG
{
	public float x;
	public float y;
	public bool faceUp = false;
	public string layerName = "Default";
	public int layerID = 0;
	public int id;
	public List<int> hiddenBy = new List<int>();
	public string type = "slot";
	public Vector2 stagger;
}

public class LayoutOG : MonoBehaviour
{
	public PT_XMLReaderOG xmlr; //Just like DeckOG, this has a PT_XMLReaderOG
	public PT_XMLHashtableOG xml; //This variable is for easier xml access
	public Vector2 multiplier; //Sets the spacing of the tableau

	//SlotDefOG references
	public List<SlotDefOG> slotDefs; //All the SlotDefs for Row0 - Row6
	public SlotDefOG drawPile;
	public SlotDefOG discardPile;

	//This holds all of the possible names for the layers set by layerID
	public string[] sortingLayerNames = new string[] { "Row0", "Row1", "Row2", "Row3", "Row4", "Row5", "Row6", "Discard", "Draw" };

	//This function is called to read in the LayoutXML.xml file
	public void ReadLayout(string xmlText)
	{
		xmlr = new PT_XMLReaderOG();
		xmlr.Parse(xmlText); //The XML is parsed
		xml = xmlr.xml["xml"][0]; //And xml is set as a shortcut to the XML

		//Read in a multiplier, which sets card spacing
		multiplier.x = float.Parse(xml["multiplier"][0].att("x"));
		multiplier.y = float.Parse(xml["multiplier"][0].att("y"));

		//Read in the slots
		SlotDefOG tSD;

		//slotsX is used as a shortcut to all the <slot>s
		PT_XMLHashListOG slotsX = xml["slot"];

		for (int i = 0; i < slotsX.Count; i++)
		{
			tSD = new SlotDefOG(); //Create a new SlotDefOG instance
			if (slotsX[i].HasAtt("type"))
			{
				//If this <slot> has a type attribute, parse it
				tSD.type = slotsX[i].att("type");
			}
			else
			{
				//If not, set its type to "slot"; it's a tableau card
				tSD.type = "slot";
			}
			//Various attributes are parsed into numerical values
			tSD.x = float.Parse(slotsX[i].att("x"));
			tSD.y = float.Parse(slotsX[i].att("y"));
			tSD.layerID = int.Parse(slotsX[i].att("layer"));

			//This converts the number of the layerID into a text layerName
			tSD.layerName = sortingLayerNames[tSD.layerID];

			//The layers are used to make sure that the correct cards are on top of the others. In Unity 2D, all of our assets
			//are effectively at the same Z depth, so the layer is used to differentiate between them.

			switch (tSD.type)
			{
				//Pull additional attributes based on the type of this <slot>
				case "slot":
					tSD.faceUp = (slotsX[i].att("faceup") == "1");
					tSD.id = int.Parse(slotsX[i].att("id"));
					if (slotsX[i].HasAtt("hiddenby"))
					{
						string[] hiding = slotsX[i].att("hiddenby").Split(',');
						foreach (string s in hiding)
						{
							tSD.hiddenBy.Add(int.Parse(s));
						}
					}
					slotDefs.Add(tSD);
					break;

				case "drawpile":
					tSD.stagger.x = float.Parse(slotsX[i].att("xstagger"));
					drawPile = tSD;
					break;

				case "discardpile":
					discardPile = tSD;
					break;
			}
		}
	}
}
