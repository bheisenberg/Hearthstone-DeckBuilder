/* * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Card class - Brian Eisenberg                          *
 * Used to create Card objects queried from the cards db *
 * Contains all of the displayed properties of a card    *
 * Artist and how to get are currently omitted           *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace RecyclerViewTutorial
{

    //Contains all values that make up a card
    public class Card
    {
        //Card's ID value (uses string because card IDs are a combination of letters and numbers and therefore stored as type VARCHAR in the db)
        public string card_id { get; set; }

        //Name of the card
        public string name { get; set; }

        //Mana cost of the card
        public int cost { get; set; }

        //Description text of the card
        public string text { get; set; }

        //Flavor text of the card
        public string flavor { get; set; }

        //PNG image of the card itself. Not currently used.
        public string img { get; set; }

        //Image displayed if the card is the special gold edition
        public string img_gold { get; set; }

        //Boolean (1 or 0) to show if the card is collectible or not. Stored in the form of an int because of the lack of bools in SQLite
        //Will be converted in future releases
        public int collectible { get; set; }

        //Health value of the card
        public int health { get; set; }

        //Attack value of the card
        public int attack { get; set; }

        //Rarity of the card (Free, Common, Rare, Epic, Legendary)
        public string rarity_name { get; set; }

        //Faction that the card belongs to (None, Alliance, Horde)
        public string faction_name { get; set; }

        //Type of card (Weapon, Spell, Enchantment, Minion, Hero Power, Hero)
        public string type_name { get; set; }

        //Overrides to ToString function to return a formatted card for easier debugging
        public override string ToString()
        {
            return string.Format("[Card: ID={0}, Name={1}, Cost={2}, Text={3}, flavor={4}, img={5}, img_gold={6}, collectible={7}, Health={8}, Attack={9}, Rarity={10}, Faction={11}, Type={12}]", 
                card_id, name, cost, text, flavor, img, img_gold, collectible, health, attack, rarity_name, faction_name, type_name);
        }
    }
}