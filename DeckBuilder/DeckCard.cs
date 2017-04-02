/* * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * DeckCard class - Brian Eisenberg                      *
 * Used to populate the user's deck of cards             *
 * Contents: Auto incremented ID, card ID                *
 * The rest of the info for each card is queried from    *
 * the card table, so only card ID is necessary.         *
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
    public class DeckCard
    {
        //Auto incremented value to give each card a unique primary key
        public string deck_card_id { get; set; }

        //Actual ID value of the card
        public string card_id { get; set; }

        //Override of the ToString function that neatly displays the card. Used for debugging purposes.
        public override string ToString()
        {
            return string.Format("[DeckCard: ID={0}, Card={1}", 
                deck_card_id, card_id);
        }
    }
}