using Microsoft.Data.Sqlite;
using StarRealms.Cards;
using StarRealms.Utility;
using static StarRealms.Utility.Guide;

namespace StarRealms.Game
{
    internal class Player
    {
        const int CardsToTake = 5;
        private Game Game { get; set; }
        public string Name { get; private set; }
        public int Health { get; private set; }
        private int Gold { get; set; }
        private List<int> Damage { get; set; }
        public int DropCount { get; set; }
        public int CardsOnTop { get; set; }
        private Queue<MasterCard> Deck { get; set; }
        private List<MasterCard> Hand { get; set; }
        private List<BaseCard> Bases { get; set; }
        private List<MasterCard> DiscardPile { get; set; }
        private Dictionary<Fractions, int> FractionsList { get; set; }
        private Dictionary<Fractions, List<Property>> InactiveFracProperties { get; set; }
        private List<Property> PropertiesToPlay { get; set; }
        private DecisionMaker DecisionMaker { get; set; }

        public Player(Game game, string name = "p1", int R_FracPriority = 100,
                                                    int G_FracPriority = 33,
                                                    int B_FracPriority = 33,
                                                    int Y_FracPriority = 67,
                                                    int G = 100,
                                                    int D = 100,
                                                    int H = 100,
                                                    int Agr = 20)
        {
            Game = game;
            Name = name;
            Health = 50;
            Gold = 0;
            Damage = new List<int>();
            DropCount = 0;
            CardsOnTop = 0;
            Deck = new Queue<MasterCard>();
            Hand = new List<MasterCard>();
            Bases = new List<BaseCard>();
            DiscardPile = new List<MasterCard>();
            FractionsList = new Dictionary<Fractions, int>();
            InactiveFracProperties = new Dictionary<Fractions, List<Property>>();
            PropertiesToPlay = new List<Property>();
            DecisionMaker = new DecisionMaker(R_FracPriority, G_FracPriority, B_FracPriority, Y_FracPriority, G, D, H, Agr);

            InitDictionaries();
            InitStartDeck();
        }

        public bool isAlive()
        {
            return Health > 0;
        }
        public void takeATurn(int startMinusCards = 0)
        {
            //Добор карт
            if (Deck.Count >= CardsToTake)
            {
                for (int i = 0; i < CardsToTake - startMinusCards; i++)
                    Hand.Add(Deck.Dequeue());
            }
            else
            {
                ReshuffleDeck();
                int takeCount = Deck.Count >= CardsToTake ? CardsToTake : Deck.Count;
                for (int i = 0; i < takeCount; i++)
                    Hand.Add(Deck.Dequeue());
            }

            //Сброс
            if (DropCount > 0)
            {
                if (Hand.Count > DropCount)
                {
                    Random rnd = new Random();
                    for (int i = 0; i < DropCount; i++)
                    {
                        int index = rnd.Next(Hand.Count * 100) / 100;
                        DiscardPile.Add(Hand[index]);
                        Hand.RemoveAt(index);
                    }
                }
                else
                {
                    DiscardPile.AddRange(Hand);
                    Hand.Clear();
                }
            }

            //Розыгрыш карт из руки
            foreach (MasterCard card in Hand)
                card.Play(Game);


            //Использование утиль-свойства
            foreach (MasterCard card in Hand)
                if (card.UtilProperty != null)
                    card.Util(Game, 10);


            //Проверка активации фракционных свойств
            CheckFractionsProperies();

            //Розыгрыш свойств
            PlayProperties();

            //Закупка
            while (CanBuyFromMarket(Gold))
                Buy();

            //На остатки закупаем исследователей
            if (Gold >= 2)
                while (Gold > 0 && Game.Researchers.Count > 0)
                    DiscardPile.Add(Game.Researchers.Dequeue());

            Attack();

            EndTurn();
        }

        //методы для колоды
        private void ReshuffleDeck()
        {
            foreach (var card in ShuffleDeck(DiscardPile))
            {
                Deck.Enqueue(card);
            }
            DiscardPile.Clear();
        }
        private void InitStartDeck()
        {
            List<MasterCard> deck = new();
            using (var connection = new SqliteConnection("Data Source=StarEmps.db"))
            {
                //Start hand
                string sql = "select * from StartCards";
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
                                deck.Add(new ShipCard
                                {
                                    CardName = reader.GetString(reader.GetOrdinal("Name")),
                                    Gold = reader.GetInt32(reader.GetOrdinal("Gold")),
                                    Damage = reader.GetInt32(reader.GetOrdinal("Damage"))
                                });
                            }
                        }
                    }
                }
                connection.Close();
            }
            Deck = new(ShuffleDeck(deck));
        }
        private List<MasterCard> ShuffleDeck(List<MasterCard> deck)
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

        //инициализация вспомогательных словарей
        private void InitDictionaries()
        {
            FractionsList.Add(Guide.Fractions.Слизь, 0);
            FractionsList.Add(Guide.Fractions.Технокульт, 0);
            FractionsList.Add(Guide.Fractions.Торговая_федерация, 0);
            FractionsList.Add(Guide.Fractions.Звездная_империя, 0);

            InactiveFracProperties.Add(Guide.Fractions.Слизь, new());
            InactiveFracProperties.Add(Guide.Fractions.Технокульт, new());
            InactiveFracProperties.Add(Guide.Fractions.Торговая_федерация, new());
            InactiveFracProperties.Add(Guide.Fractions.Звездная_империя, new());
        }

        //utility-методы
        public void ShowDeck()
        {
            foreach (var card in Deck)
                Console.WriteLine($"{card.CardName} {card.Gold}|{card.Damage}|{card.Heal} {Guide.GetReadableFractions(card.Fractions)}");
        }
        public void GetInfo()
        {
            Console.WriteLine($"{Name}");
            Console.WriteLine($"{Health}|50 HP");
            Console.WriteLine($"{Gold}Gold");
            Console.WriteLine($"Bases:{Bases.Count}");
        }


        //Добавление базы
        public void AddBase(BaseCard Base)
        {
            Bases.Add(Base);
        }

        // Добавление стандартных ресурсов
        public void AddGold(int gold)
        {
            Gold += gold;
        }
        public void AddDamage(int damage)
        {
            Damage.Add(damage);
            Damage.Sort();
        }
        public void Heal(int heal)
        {
            Health += heal;
        }
        public void TakeDamage(List<int> damage)
        {
            foreach (var d in damage)
                Health -= d;
        }

        //Проверка фркационных свойств
        private void CheckFractionsProperies()
        {
            foreach (var fraction in FractionsList)
            {
                if (fraction.Value >= 2)
                    PropertiesToPlay.AddRange(InactiveFracProperties[fraction.Key]);
            }
        }

        //Добавление свойств
        public void AddFractionsAndProperties(List<Fractions> cardFractions, Property FracProperty)
        {
            foreach (var cardFraction in cardFractions)
            {
                FractionsList[cardFraction]++;
                InactiveFracProperties[cardFraction].Add(FracProperty);
            }
        }
        public void AddInitialProperties(Property property)
        {
            PropertiesToPlay.Add(property);
        }

        //Розыгрыш свойств
        private void PlayProperties()
        {
            foreach (var property in PropertiesToPlay)
                property.ActivateProperty(Game);
        }


        //Для покупок из маркета
        private bool CanBuyFromMarket(int gold)
        {
            if (gold > 0)
                foreach (MasterCard card in Game.Market)
                {
                    if (card.Price <= gold)
                        return true;
                }
            return false;
        }
        private Dictionary<int, MasterCard> getBuyableCardFromMarket()
        {
            Dictionary<int, MasterCard> buyable = new();
            for (int i = 0; i < Game.Market.Count; i++)
                if (Game.Market[i].Price <= Gold)
                    buyable.Add(i, Game.Market[i]);
            return buyable;
        }
        private void Buy()
        {
            Dictionary<int, MasterCard> buyable = getBuyableCardFromMarket();
            int buyFrom = DecisionMaker.WhatToBuy(getBuyableCardFromMarket());

            DiscardPile.Add(buyable[buyFrom]);
            Game.removeFromMarket(buyFrom);
            Gold -= buyable[buyFrom].Price;
        }

        //Атака соперника (и/или его баз)
        private void Attack()
        {
            if (Damage.Count > 0)
            {
                Player Enemy = Game.getEnemy();
                if (Enemy.HaveBases())
                {
                    List<BaseCard> EnemyBases = Enemy.GetActiveBases();
                    Dictionary<int, BaseCard> Outposts = new Dictionary<int, BaseCard>();
                    for (int i = 0; i < EnemyBases.Count; i++)
                        if (EnemyBases[i].IsTaunt) Outposts.Add(i, EnemyBases[i]);

                    foreach (var Base in Outposts)
                    {
                        if (FindMinSumGreaterOrEqual(Damage, Base.Value.Strength) != null)
                        {
                            Damage = FindMinSumGreaterOrEqual(Damage, Base.Value.Strength);
                            Enemy.DestroyBaseAt(Base.Key);
                        }
                    }
                    if (Outposts.Count == 0)
                    {
                        for (int i = 0; i < EnemyBases.Count; i++)
                        {
                            if (DecisionMaker.AttackBase() && FindMinSumGreaterOrEqual(Damage, EnemyBases[i].Strength) != null)
                            {
                                Damage = FindMinSumGreaterOrEqual(Damage, EnemyBases[i].Strength);
                                Enemy.DestroyBaseAt(i);
                            }
                        }
                        Enemy.TakeDamage(Damage);
                    }
                }
                else
                    Enemy.TakeDamage(Damage);
            }
        }

        //Конец хода - обнуление свойств, фракций, золота, урона, сброс карт
        private void EndTurn()
        {
            DiscardPile.AddRange(Hand);
            Hand.Clear();
            Damage.Clear();
            CardsOnTop = 0;
            Gold = 0;
            foreach (var kvp in FractionsList)
                FractionsList[kvp.Key] = 0;
            foreach (var kvp in InactiveFracProperties)
                InactiveFracProperties[kvp.Key].Clear();
            PropertiesToPlay.Clear();
        }

        //Базы
        public bool HaveBases()
        {
            return Bases.Count > 0;
        }
        public List<BaseCard> GetActiveBases()
        {
            return Bases;
        }
        public void DestroyBaseAt(int index)
        {
            Game.Graveyard.Add(Bases[index]);
            Bases.RemoveAt(index);
        }

        //Проверка на количество урона для уничтожения базы. Возвращает доступный после уничтожения базы урон или null, если урона не хватает
        private List<int> FindMinSumGreaterOrEqual(List<int> numbers, int target)
        {
            List<int> selectedNumbers = new List<int>();
            int currentSum = 0;

            // Идем по отсортированному списку и добавляем числа, пока не достигнем или не превысим цель
            foreach (int number in numbers)
            {
                if (currentSum >= target)
                {
                    break;
                }
                selectedNumbers.Add(number);
                currentSum += number;
            }

            // Если сумма меньше цели, то невозможно достичь цель с данным списком чисел
            if (currentSum < target)
            {
                return null;
            }

            // Вычисляем оставшиеся числа, которые не были использованы
            List<int> remainingNumbers = numbers.Except(selectedNumbers).ToList();

            return remainingNumbers;
        }


        //Активация свойств
        //Выбор из двух и более
        public Property? ChooseOne(List<Property> propertiesToChoose) => DecisionMaker.ChooseOneProperty(propertiesToChoose);

        //А - добор карт
        public void TakeAdditionalCard(int count)
        {
            if (Deck.Count >= count)
            {
                for (int i = 0; i < count; i++)
                {
                    MasterCard TakenCard = Deck.Dequeue();
                    TakenCard.Play(Game);
                    Hand.Add(TakenCard);
                }
            }
            else
            {
                ReshuffleDeck();
                int takeCount = Deck.Count >= count ? count : Deck.Count;
                for (int i = 0; i < takeCount; i++)
                {
                    MasterCard TakenCard = Deck.Dequeue();
                    TakenCard.Play(Game);
                    Hand.Add(TakenCard);
                }
            }
        }

        //U - утилизация (стартовые карты в приоритете)
        public void UtilCard(int count)
        {

        }

    }
}
