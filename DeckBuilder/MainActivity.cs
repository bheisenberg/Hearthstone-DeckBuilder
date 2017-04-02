/* * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * MainActivity Class - Brian Eisenberg                  *
 * Contains the main functionality of the application    *
 *                                                       *
 * Description:                                          *
 * Uses a RecyclerView populated with                    *
 * CardViews to display queried Hearthstone cards from a *
 * local SQLite db.                                      *
 *                                                       *
 * Packages used:                                        *
 * SQLite - Database Querying/Updating                   *
 * Calligraphy - Custom TextView font                    *
 * Standard Android packages                             *
 *                                                       *
 * Credits: Joe Rock's Recycler View Tutorial            *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V7.Widget;
using SQLite;
using System.IO;
using Android.Graphics;
using Calligraphy;
using Android.Text;

namespace RecyclerViewTutorial
{
    [Activity(Label = "Deck Builder", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private RecyclerView mRecyclerView;
        private RecyclerView.LayoutManager mLayoutManager;
        private RecyclerView.Adapter mAdapter;
        private bool deckMode = false; //Boolean to determine whether the application is in Deck Viewing mode or Searching mode (Should be enum but there's only two states)
        int numCards = 0; //UNUSED Number of cards in the deck
        private List<Card> Cards; //List of cards populated by db queries that populates the RecyclerView
        private QueryString mQueryString;
        private BuildQuery mBuildQuery;
        private string[] screens;

        #region Query Classes

        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * QueryString Class                                     *
         *                                                       *
         * Description:                                          *
         * Was originally used to fill in the "?" in the SQL     *
         * Query. Somewhat deprecated as only Name is currently  *
         * used as the rest of the values are populated through  *
         * string formatting.                                    *
         * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
        public class QueryString
        {
            public string Name { get; set; }
            public string Rarity { get; set; }
            public string Faction { get; set; }
            public string Type { get; set; }
            public int Cost { get; set; }
            public int Attack { get; set; }
            public int Health { get; set; }

            public QueryString(string name, string rarity, string faction, string type, int cost, int attack, int health)
            {
                name = Name;
                rarity = Rarity;
                faction = Faction;
                type = Type;
                cost = Cost;
                attack = Attack;
                health = Health;
            }
        }

        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * BuldQuery Class                                       *
         *                                                       *
         * Description:                                          *
         * Used to build dynamic queries.                        *
         * Uses string concatenation to build complete SQL query *
         * from multiple strings. This is particularly useful    *
         * because it allows querying of multiple conditions     *
         * without requiring any conditions                      *
         * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
        public class BuildQuery
        {
            //Initial query string, handles selecting every part of the card that we display as well as all of the joins.
            private string StartString = @"SELECT c.*, cr.rarity_name, cf.faction_name, ct.type_name
                                                    FROM Card c, CardRarity cr, Rarity r, CardFaction cf, Faction f, CardType ct, TypeName t
                                                    WHERE cr.card_id = c.card_id
                                                    AND cf.card_id = c.card_id
                                                    AND ct.card_id = c.card_id
                                                    AND c.name LIKE ?";

            //Will be filled with a query to find cards of a specific rarity
            //Populated from the Rarity Spinner
            public string Rarity { get; set; } = "";

            //Will be filled with a query to find cards of a specific faction
            //Populated from the Faction Spinner
            public string Faction { get; set; } = "";

            //Will be filled with a query to find cards of a specific type
            //Popualted from the Type Spinner
            public string Cardtype { get; set; } = "";

            //Will be filled with a query to find cards of a specific name
            //Populated from the search bar (EditText)
            public string Name { get; set; } = "";

            //Will be filled with a query to find cards of a specific cost value
            //Populated from the cost search bar (EditText)
            public string Cost { get; set; } = "";

            //Will be filled with a query to find cards of a specific health value
            //Populated from the health search bar (EditText)
            public string Health { get; set; } = "";

            //Will be filled with a query to find cards of a specific attack value
            //Populated from the health search bar (EditText)
            public string Attack { get; set; } = "";

            //Aggregate query string that gets added on to the end of the query to group results by Card ID
            private string EndString = "GROUP BY c.card_id";


            public BuildQuery(string rarity, string faction, string cardtype, string name, string cost, string health, string attack)
            {
                rarity = Rarity;
                faction = Faction;
                cardtype = Cardtype;
                name = Name;
                cost = Cost;
                health = Health;
                attack = Attack;
            }

            //Override of the ToString function to create a query based on the current criterion
            //This method is used in the actual query
            public override string ToString()
            {
                string MiddleString = Rarity + Faction + Cardtype + Cost + Health + Attack;
                string FinalString = StartString + MiddleString + " " + EndString;
                return FinalString;
            }
        }
        #endregion

        #region SQLite Connection
        public SQLiteConnection GetConnection(string db)
        {
            var sqliteFilename = db;
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal); // Documents folder
            var path = System.IO.Path.Combine(documentsPath, sqliteFilename);

            // This is where we copy in the prepopulated database
            Console.WriteLine(path);
            if (!File.Exists(path))
            {
                var s = Assets.Open(db);
                // create a write stream
                FileStream writeStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                // write to the stream
                ReadWriteStream(s, writeStream);
            }

            var conn = new SQLiteConnection(path);

            // Return the database connection 
            return conn;
        }


        void ReadWriteStream(Stream readStream, Stream writeStream)
        {
            int Length = 256;
            Byte[] buffer = new Byte[Length];
            int bytesRead = readStream.Read(buffer, 0, Length);
            // write the required bytes
            while (bytesRead > 0)
            {
                writeStream.Write(buffer, 0, bytesRead);
                bytesRead = readStream.Read(buffer, 0, Length);
            }
            readStream.Close();
            writeStream.Close();
        }
        #endregion


        #region Initialization
        //Initializes the dropdown menu for rarity
        void InitializeRaritySpinner ()
        {
            //Populates the spinner with values from rarity.xml
            Spinner raritySpinner = FindViewById<Spinner>(Resource.Id.rarity_spinner);

            raritySpinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(raritySpinner_ItemSelected);//Calls raritySpinner_ItemSelected when an item is selected
            var rarityadapter = ArrayAdapter.CreateFromResource(
                    this, Resource.Array.rarity_array, Android.Resource.Layout.SimpleSpinnerItem);

            rarityadapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            raritySpinner.Adapter = rarityadapter;
        }

        //Initializes the dropdown menu for factions
        void InitializeFactionSpinner ()
        {
            //Populates the spinner with values from faction.xml
            Spinner factionSpinner = FindViewById<Spinner>(Resource.Id.faction_spinner);

            factionSpinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(factionSpinner_ItemSelected);//Calls factionSpinner_ItemSelected when an item is selected
            var factionAdapter = ArrayAdapter.CreateFromResource(
                    this, Resource.Array.faction_array, Android.Resource.Layout.SimpleSpinnerItem);

            factionAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            factionSpinner.Adapter = factionAdapter;
        }

        //Initializes the dropdown menu for factions
        void InitializeTypeSpinner()
        {
            //Populates the spinner with values from faction.xml
            Spinner typeSpinner = FindViewById<Spinner>(Resource.Id.type_spinner);

            typeSpinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(typeSpinner_ItemSelected);//Calls factionSpinner_ItemSelected when an item is selected
            var typeAdapter = ArrayAdapter.CreateFromResource(
                    this, Resource.Array.type_array, Android.Resource.Layout.SimpleSpinnerItem);

            typeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            typeSpinner.Adapter = typeAdapter;
        }

        //Initializes the name Search Bar
        void InitNameField ()
        {
            EditText searchBar = FindViewById<EditText>(Resource.Id.search);

            //Updates the query whenever the user changes the text inside of it
            searchBar.TextChanged += (object sender, TextChangedEventArgs e) => {
                mQueryString.Name = "%" + searchBar.Text + "%"; //Uses % for more accurate LIKE statement
                mAdapter.NotifyDataSetChanged();
                Query(mQueryString.Name, mQueryString.Rarity, mQueryString.Faction, mQueryString.Type, mQueryString.Cost);
            };
        }

        //Initializes the Cost Search Bar
        void InitCostField ()
        {
            EditText cost = FindViewById<EditText>(Resource.Id.cost);

            cost.TextChanged += (object sender, TextChangedEventArgs e) => {
                //Updates the query whenever the user changes the text inside of it
                if (cost.Text.ToString().Trim().Length > 0)
                {
                    mBuildQuery.Cost = string.Format(" AND c.cost = {0}", int.Parse(cost.Text));
                }
                //If there's no text, don't query by cost
                else
                {
                    mBuildQuery.Cost = "";
                }
                mAdapter.NotifyDataSetChanged();
                Query(mQueryString.Name, mQueryString.Rarity, mQueryString.Faction, mQueryString.Type, mQueryString.Cost);
            };
        }

        //Initializes the Attack Search Bar
        void InitAttackField ()
        {
            EditText attack = FindViewById<EditText>(Resource.Id.attack);

            attack.TextChanged += (object sender, TextChangedEventArgs e) => {
                //Updates the query whenever the user changes the text inside of it
                if (attack.Text.ToString().Trim().Length > 0)
                {
                    mBuildQuery.Attack = string.Format(" AND c.attack = {0}", int.Parse(attack.Text));
                }
                //If there's no text, don't query by attack
                else
                {
                    mBuildQuery.Attack = "";
                }
                mAdapter.NotifyDataSetChanged();
                Query(mQueryString.Name, mQueryString.Rarity, mQueryString.Faction, mQueryString.Type, mQueryString.Cost);
            };
        }

        //Initializes the Health Search Bar
        void InitHealthField ()
        {
            EditText health = FindViewById<EditText>(Resource.Id.health);

            health.TextChanged += (object sender, TextChangedEventArgs e) => {
                //Updates the query whenever the user changes the text inside of it
                if (health.Text.ToString().Trim().Length > 0)
                {
                    mBuildQuery.Health = string.Format(" AND c.health = {0}", int.Parse(health.Text));
                }
                //If there's no text, don't query by health
                else
                {
                    mBuildQuery.Health = "";
                }
                mAdapter.NotifyDataSetChanged();
                Query(mQueryString.Name, mQueryString.Rarity, mQueryString.Faction, mQueryString.Type, mQueryString.Cost);
            };
        }


        //Initializes the left drawer for navigation. Used in material design for apps that use multiple screens (UNUSED)
        void InitializeLeftDrawer ()
        {
            screens = new string[] { "Find Cards", "Build Decks" };
            ListView leftDrawer = FindViewById<ListView>(Resource.Id.left_drawer);
            ArrayAdapter<String> itemsAdapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleListItem1, screens);
            leftDrawer.Adapter = itemsAdapter;
        }

        //Bulk method to initialize all search bars
        void InitializeSearchBars ()
        {
            InitNameField();
            InitAttackField();
            InitHealthField();
            InitCostField();
        }

        //Bulk method to initialize all spinners
        void InitializeSpinners ()
        {
            InitializeFactionSpinner();
            InitializeRaritySpinner();
            InitializeTypeSpinner();
        }

        //Uses the Calligraphy package to set the default font to Google's Roboto
        void SetFont ()
        {
            CalligraphyConfig.InitDefault(new CalligraphyConfig.Builder()
                .SetDefaultFontPath("fonts/Roboto-Regular.ttf")
                .SetFontAttrId(Resource.Attribute.fontPath).Build());
        }
        

        //Creates instances of the QueryString and BuildQuery class so we can use them to query the db
        void InitQuery ()
        {
            mQueryString = new QueryString("", "", "", "", 0, 0, 0);//Filled with empty strings and 0's so there's no conditions
            mBuildQuery = new BuildQuery("", "", "", "", "", "", "");//Filled with empty strings so there's no conditions
        }
        #endregion

        //Called when the activity begins, handles all initialization
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            var myToolbar = FindViewById<Android.Widget.Toolbar>(Resource.Id.my_toolbar); 
            SetActionBar(myToolbar); //Sets the ActionBar to my toolbar so that we don't have to use the default Action bar
            mRecyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            InitializeSearchBars();
            InitializeSpinners();
            InitQuery();
            SetFont();
            Cards = new List<Card>(); //Initializes our list of cards
            mLayoutManager = new LinearLayoutManager(this); 
            mRecyclerView.SetLayoutManager(mLayoutManager);
            mAdapter = new RecyclerAdapter(Cards, mRecyclerView, deckMode, this); 
            mRecyclerView.SetAdapter(mAdapter); //Sets the adapter for the RecyclerView to display our list of cards
        }

        protected override void AttachBaseContext(Context context)
        {
            base.AttachBaseContext(CalligraphyContextWrapper.Wrap(context));
        }

        #region SpinnerFunctionality
        //Called when an item is selected on the Rarity Spinner
        private void raritySpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;

            string currentRarity = spinner.GetItemAtPosition(e.Position).ToString();//Gets the string of the current spinner item to use for our query
            if (currentRarity != null)
            {
                //Checks if the rarity is set to any so that it does not filter by rarity
                if (currentRarity == "Any")
                {
                    mBuildQuery.Rarity = "";

                }
                //Sets the query's rarity statement to the rarity that is selected
                else
                {
                    mBuildQuery.Rarity = string.Format(" AND cr.rarity_name = \"{0}\"", currentRarity);
                }
                mQueryString.Rarity = currentRarity;
            }
            //Either way, we update our query so the user gets immediate feedback
            Query(mQueryString.Name, mQueryString.Rarity, mQueryString.Faction, mQueryString.Type, mQueryString.Cost);
            Console.WriteLine(mBuildQuery.ToString());
            mAdapter.NotifyDataSetChanged();
        }

        //Called when an item is selected on the faction Spinner
        private void factionSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;

            string currentFaction = spinner.GetItemAtPosition(e.Position).ToString();//Gets the string of the current faction item to use for our query
            if (currentFaction != null)
            {
                //Checks if the rarity is set to any so that it does not filter by faction
                if (currentFaction == "Any")
                {
                    mBuildQuery.Faction = "";

                }
                //Sets the query's faction statement to the faction that is selected
                else
                {
                    mBuildQuery.Faction = string.Format(" AND cf.faction_name = \"{0}\"", currentFaction);
                }
                mQueryString.Rarity = currentFaction;
            }
            //Either way, we update our query so the user gets immediate feedback
            Query(mQueryString.Name, mQueryString.Rarity, mQueryString.Faction, mQueryString.Type, mQueryString.Cost);
            mAdapter.NotifyDataSetChanged();
        }

        //Called when an item is selected on the type Spinner
        private void typeSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;

            string currentType = spinner.GetItemAtPosition(e.Position).ToString();//Gets the string of the current faction item to use for our query
            if (currentType != null)
            {
                //Checks if the rarity is set to any so that it does not filter by type
                if (currentType == "Any")
                {
                    mBuildQuery.Cardtype = "";

                }
                //Sets the query's type statement to the type that is selected
                else
                {
                    mBuildQuery.Cardtype = string.Format(" AND ct.type_name = \"{0}\"", currentType);
                }
                mQueryString.Rarity = currentType;
            }
            //Either way, we update our query so the user gets immediate feedback
            Query(mQueryString.Name, mQueryString.Rarity, mQueryString.Faction, mQueryString.Type, mQueryString.Cost);
            mAdapter.NotifyDataSetChanged();
        }
        #endregion

        //This method queries the database in the form of a Card object and adds each card to our list of cards
        void Query(string queryName, string rarityName, string factionName, string typeName, int cost)
        {
            deckMode = false;
            mAdapter = new RecyclerAdapter(Cards, mRecyclerView, deckMode, this);
            mRecyclerView.SetAdapter(mAdapter);
            var db = GetConnection("cardsdb.sqlite"); //Connects to the database
            List<Card> queriedCards = new List<Card>();//Initializes the list of queried cards
            queriedCards = db.Query<Card>(mBuildQuery.ToString(), mQueryString.Name);//Queries the cards into a list Card objects
            int count = 0;
            Cards.Clear();
            //Adds all cards to the list
            foreach (Card card in queriedCards)
            {
                Cards.Add(card);
                Console.WriteLine(card.ToString());
                count += 1;
            }

            TextView matches = FindViewById<TextView>(Resource.Id.txtNumCards);//Finds the textview for matches so we can use it
            matches.Text = string.Format("{0} cards found", count);

            //Completes a low level db transaction to create a deck for the user if one does not already exist
            db.RunInTransaction(() => {
                db.Execute(@"CREATE TABLE IF NOT EXISTS DeckCard (
                             deck_card_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                             card_id VARCHAR(20))");
            });
            db.Close();//Closes the connection with the db (Used as a safeguard incase db.query and db.RunInTransaction don't automatically close their connections)
        }

        //Used to query our deck of cards and populate the RecyclerView with Card objects based on the IDs found in the deck
        void QueryDeck()
        {
            deckMode = true;//Sets the application's mode to deckmode
            mAdapter = new RecyclerAdapter(Cards, mRecyclerView, deckMode, this);//Updates the adapter with the value of deckmode
            mRecyclerView.SetAdapter(mAdapter);
            int count = 0;
            Cards.Clear();
            var db = GetConnection("cardsdb.sqlite");//Connects to the db
            List<DeckCard> deckCards = new List<DeckCard>();//Creates a new list of deckCards
            deckCards = db.Query<DeckCard>(@"SELECT * FROM DeckCard");//Queries the list of cards currently in the deck from the db

            //String that queries Card objects based on the IDs from the deck
            string queryString = @"SELECT c.*, cr.rarity_name, cf.faction_name, ct.type_name
                                                    FROM Card c, CardRarity cr, Rarity r, CardFaction cf, Faction f, CardType ct, TypeName t
                                                    WHERE c.card_id = ?
                                                    AND cr.card_id = c.card_id
                                                    AND cf.card_id = c.card_id
                                                    AND ct.card_id = c.card_id
                                                    GROUP BY c.card_id";

            List<Card> deck = new List<Card>();//Initializes our deck

            //Loops through all the cards in the deck and creates a Card object for each one
            foreach (DeckCard card in deckCards)
            {
                Card newCard = db.Query<Card>(queryString, card.card_id)[0];
                deck.Add(newCard);
                Console.WriteLine(card.ToString());
            }

            //Adds all the card objects to our list of cards
            foreach (Card card in deck)
            {
                Cards.Add(card);
                count += 1;
            }

            TextView matches = FindViewById<TextView>(Resource.Id.txtNumCards);
            matches.Text = string.Format("{0}/30", count);//Displays the number of cards in the deck (Only updates on a new query for now)
            mAdapter.NotifyDataSetChanged();//Updates the adapter with our new list of cards
        }

        //Inflates the options menu on the top right when it is clicked
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.mymenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        //Queries the user's deck when the "View Deck" button is selected (only one button so we don't need to specify)
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            QueryDeck();
            return base.OnOptionsItemSelected(item);
        }
    }


    //Adapter for the RecyclerView, used to populate the view with our list of cards
    public class RecyclerAdapter : RecyclerView.Adapter
    {
        //Any necessary values that need to get passed in are listed here
        private List<Card> Cards;
        private RecyclerView mRecyclerView;
        private bool deckMode;
        private Context mContext;
        private int mCurrentPosition = -1;

        public RecyclerAdapter(List<Card> cards, RecyclerView recyclerView, bool mode, Context context)
        {
            Cards = cards;
            mRecyclerView = recyclerView;
            mContext = context;
            deckMode = mode;
        }

        //Viewholder, contains references to the all of the card's attributes as well as the mainview so we can reference them all later
        public class MyView : RecyclerView.ViewHolder
        {
            public View mMainView { get; set; }
            public TextView mName { get; set; }
            public ImageView mImg { get; set; }
            public TextView mAttack { get; set; }
            public TextView mCost { get; set; }
            public TextView mHealth { get; set; }
            public TextView mDescription { get; set; }
            public CardView mRarity { get; set; }


            public MyView(View view) : base(view)
            {
                mMainView = view;
            }
        }

        //Populates the viewholder with all of the references
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            //First card view
            View card = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.card, parent, false);
            TextView txtName = card.FindViewById<TextView>(Resource.Id.txtName);
            TextView txtCost = card.FindViewById<TextView>(Resource.Id.txtCost);
            TextView txtAttack = card.FindViewById<TextView>(Resource.Id.txtAttack);
            TextView txtHealth = card.FindViewById<TextView>(Resource.Id.txtHealth);
            CardView cardColor = card.FindViewById<CardView>(Resource.Id.cardBG);
            TextView txtDescription = card.FindViewById<TextView>(Resource.Id.txtDescription);

            cardColor.Click += mMainView_Click;//Calls "mMainview_Click" whenever a card is tapped/clicked


            MyView view = new MyView(card) { mName = txtName, mAttack = txtAttack, mCost = txtCost, mHealth = txtHealth, mRarity = cardColor, mDescription = txtDescription };//Populates the CardView with the Card object's info
            return view;
        }

        //Updates the RecyclerViewHolder with the given items
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is MyView)
            {
                MyView myHolder = holder as MyView;

                //Dictionary that contains the colors for each rarity
                Dictionary<String, Color> rarityDict = new Dictionary<String, Color>();
                rarityDict.Add("Free", Color.ParseColor("#424242"));
                rarityDict.Add("Common", Color.ParseColor("#424242"));
                rarityDict.Add("Rare", Color.ParseColor("#2196F3"));
                rarityDict.Add("Epic", Color.ParseColor("#7E57C2"));
                rarityDict.Add("Legendary", Color.ParseColor("#FFB74D"));

                //Fill each card with its data from the card object
                myHolder.mName.SetTypeface(null, TypefaceStyle.Bold);
                myHolder.mName.Text = Cards[position].name;
                myHolder.mAttack.Text = Cards[position].attack.ToString();
                myHolder.mHealth.Text = Cards[position].health.ToString();
                myHolder.mCost.Text = Cards[position].cost.ToString();

                //Checks if the description is null
                if (Cards[position].text != null)
                {   
                    //If not, cardview's description to the card object's description
                    string formattedText = Cards[position].text.Replace("$", "").Replace("[x]", "");
                    myHolder.mDescription.TextFormatted = Html.FromHtml(formattedText);
                }

                //Checks if the description is null
                if (Cards[position].rarity_name != null)
                {
                    //If not, cardview's name color its color determined by the dictionary
                    myHolder.mName.SetTextColor(rarityDict[Cards[position].rarity_name]);
                }


                if (position > mCurrentPosition)
                {
                    mCurrentPosition = position;
                }
            }
        }

        //From SQLite Android Documentation
        public SQLiteConnection GetConnection(string db)
        {
            var sqliteFilename = db;
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal); // Documents folder
            var path = System.IO.Path.Combine(documentsPath, sqliteFilename);

            // This is where we copy in the prepopulated database
            Console.WriteLine(path);
            if (!File.Exists(path))
            {
                Activity activity = (Activity)mContext;
                var s = activity.Assets.Open(db);
                // create a write stream
                FileStream writeStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                // write to the stream
                ReadWriteStream(s, writeStream);
            }

            var conn = new SQLiteConnection(path);

            // Return the database connection 
            return conn;
        }

        //Used for reading in the db file
        void ReadWriteStream(Stream readStream, Stream writeStream)
        {
            int Length = 256;
            Byte[] buffer = new Byte[Length];
            int bytesRead = readStream.Read(buffer, 0, Length);
            // write the required bytes
            while (bytesRead > 0)
            {
                writeStream.Write(buffer, 0, bytesRead);
                bytesRead = readStream.Read(buffer, 0, Length);
            }
            readStream.Close();
            writeStream.Close();
        }

        //Called when a card is tapped or clicked
        void mMainView_Click(object sender, EventArgs e)
        {
            Activity activity = (Activity)mContext;
            var clicked = (View)sender;
            int pos = mRecyclerView.GetChildAdapterPosition(clicked);
            //If the applciation is not in deck mode, we will add the card to the user's deck
            if (!deckMode)
            {

                Console.WriteLine(Cards[pos].ToString());
                mRecyclerView.GetAdapter().NotifyItemChanged(pos);
                Toast.MakeText(activity, Cards[pos].name + " added to deck", ToastLength.Short).Show();
                var db = GetConnection("cardsdb.sqlite");

                //SQL to insert a card into the DeckCard table
                db.RunInTransaction(() =>
                {
                    db.Execute(@"INSERT INTO DeckCard (card_id) VALUES (?)", Cards[pos].card_id);
                });



                db.Close();
            }
            //If the application is in deck mode, we will delete the card from the user's deck
            else if (deckMode)
            {
                Toast.MakeText(activity, Cards[pos].name + " deleted", ToastLength.Short).Show();
                var db = GetConnection("cardsdb.sqlite");

                //SQL to delete a card from the DeckCard table
                db.RunInTransaction(() =>
                {
                    db.Execute(@"DELETE FROM DeckCard
                                WHERE card_id = ?;", Cards[pos].card_id);
                });
                Cards.Remove(Cards[pos]);
                mRecyclerView.GetAdapter().NotifyItemRemoved(pos); //Lets the adapter know that we've removed a card and updates accordingly
            }
        }

        //Gets the number of cards currently on screen
        public override int ItemCount 
        {
            get { return Cards.Count; }
        }


    }
}

