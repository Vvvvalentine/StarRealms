using Microsoft.Data.Sqlite;
using StarRealms.Cards;
using StarRealms.Utility;

namespace StarRealms.Game
{
    internal class Game
    {
        public Queue<MasterCard> Deck { get; private set; }
        public List<Player> Players { get; private set; }
        public List<MasterCard> Market { get; private set; }
        public Queue<ShipCard> Researchers { get; private set; }
        public List<MasterCard> Graveyard { get; private set; }
        public int Turn { get; private set; }
        private int currentPlayerIndex;

        public Game(int numberOfPlayers = 2)
        {
            if (numberOfPlayers < 2)
                throw new ArgumentException("Минмум два игрока");

            Players = new();
            Deck = new();
            Market = new();
            Graveyard = new();
            Researchers = new();

            InitPlayers(numberOfPlayers);
            InitDeck(); //инициализация обычной колоды, колоды исследователей и магазина
            //ShowDeck();

            Turn = 1;
            currentPlayerIndex = 0;

            Players[currentPlayerIndex].TakeATurn(2);
            while (IsPlayersAlive())
            {
                Turn++;
                currentPlayerIndex = (currentPlayerIndex + 1) % Players.Count;
                Players[currentPlayerIndex].TakeATurn();
            }
            
            Console.WriteLine($"End");
        }

        public Player GetActivePlayer() { return Players[currentPlayerIndex]; }
        public Player GetEnemy() { return Players[(currentPlayerIndex + 1) % Players.Count]; }
        public Player GetEnemyWithBases()
        {
            foreach (Player player in Players)
            {
                if (player == GetActivePlayer()) continue;
                else if (player.HaveBases())
                    return player;
                else return null;
            }
            return null;
        }
        private bool IsPlayersAlive()
        {
            int aliveCount = 0;
            foreach (Player player in Players)
            {
                if (player.IsAlive())
                    aliveCount++;
            }
            return aliveCount >= 2;
        }


        private void InitPlayers(int numberOfPlayers)
        {
            Players = new List<Player>();
            for (int i = 0; i < numberOfPlayers; i++)
                Players.Add(new Player(this, $"player {i}"));
            Random rnd = new Random();
            Players = Players.OrderBy(p => rnd.Next()).ToList();
        }
        private void InitDeck()
        {
            List<MasterCard> deck = new();
            using (var connection = new SqliteConnection("Data Source=StarEmps.db"))
            {
                //Deck
                string sql = "select Number, Name, Count, Price, Fraction, FracProp, Gold, Damage, Heal, InitProp, UtilProp, Addition, null as Taunt, null as Strength, TypeMarker from Ships UNION SELECT* from Bases";
                connection.Open();
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string type = reader.GetString(reader.GetOrdinal("TypeMarker"));
                            int count = reader.GetInt32(reader.GetOrdinal("Count"));
                            //int a = reader.GetInt32(reader.GetOrdinal("Gold")) != null ? 1 : 0 ;
                            for (int i = 0; i < count; i++)
                            {
                                switch (type)
                                {

                                    case "Ship":
                                        deck.Add(new ShipCard
                                        {
                                            CardType = Guide.CardTypes.Ship,
                                            CardName = reader.GetString(reader.GetOrdinal("Name")),
                                            Price = reader.GetInt32(reader.GetOrdinal("Price")),

                                            Fractions = Guide.GetFractions(reader.GetString(reader.GetOrdinal("Fraction"))),
                                            FracProperty = new Property(reader.GetString(reader.GetOrdinal("FracProp"))),

                                            InitialProperty = new Property(reader.GetString(reader.GetOrdinal("InitProp"))),
                                            UtilProperty = new Property(reader.GetString(reader.GetOrdinal("UtilProp"))),

                                            Gold = reader.GetInt32(reader.GetOrdinal("Gold")),
                                            Damage = reader.GetInt32(reader.GetOrdinal("Damage")),
                                            Heal = reader.GetInt32(reader.GetOrdinal("Heal")),
                                        });
                                        break;
                                    case "Base":
                                        deck.Add(new BaseCard
                                        {
                                            CardType = Guide.CardTypes.Base,
                                            CardName = reader.GetString(reader.GetOrdinal("Name")),
                                            Price = reader.GetInt32(reader.GetOrdinal("Price")),

                                            Fractions = Guide.GetFractions(reader.GetString(reader.GetOrdinal("Fraction"))),
                                            FracProperty = new Property(reader.GetString(reader.GetOrdinal("FracProp"))),

                                            InitialProperty = new Property(reader.GetString(reader.GetOrdinal("InitProp"))),
                                            UtilProperty = new Property(reader.GetString(reader.GetOrdinal("UtilProp"))),

                                            Gold = reader.GetInt32(reader.GetOrdinal("Gold")),
                                            Damage = reader.GetInt32(reader.GetOrdinal("Damage")),
                                            Heal = reader.GetInt32(reader.GetOrdinal("Heal")),

                                            IsTaunt = reader.GetBoolean(reader.GetOrdinal("Taunt")),
                                            Strength = reader.GetInt32(reader.GetOrdinal("Strength"))
                                        });
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }

                //Researchers
                sql = "select * from Researchers";
                connection.Open();
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int count = reader.GetInt32(reader.GetOrdinal("Count"));
                            for (int i = 0; i < count; i++)
                            {
                                Researchers.Enqueue(new ShipCard
                                {
                                    CardName = reader.GetString(reader.GetOrdinal("Name")),
                                    Price = reader.GetInt32(reader.GetOrdinal("Price")),

                                    UtilProperty = new Property(reader.GetString(reader.GetOrdinal("UtilProp"))),

                                    Gold = reader.GetInt32(reader.GetOrdinal("Gold"))
                                });
                            }
                        }
                    }
                }
                connection.Close();
            }
            deck = ShuffleDeck(deck);
            Deck = new Queue<MasterCard>(deck);

            for (int i = 0; i < 5; i++)
                Market.Add(Deck.Dequeue());
        }
        private static List<MasterCard> ShuffleDeck(List<MasterCard> deck)
        {
            Random rnd = new();
            for (var i = deck.Count - 1; i > 0; i--)
            {
                var j = rnd.Next(0, i);
                var temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
            return deck;
        }

        public void RemoveFromMarket(int index)
        {
            if (Deck.Count > 0)
                Market[index] = Deck.Dequeue();
            else
                Market.RemoveAt(index);
        }
    }
}
