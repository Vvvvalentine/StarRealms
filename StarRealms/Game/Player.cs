using Microsoft.Data.Sqlite;
using StarRealms.Cards;
using StarRealms.Utility;
using System;
using static StarRealms.Utility.Guide;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private int CardsOnTop { get; set; }
        private int ExtraDamageToShips { get; set; }
        private int ExtraDamageToBases { get; set; }
        private int ExtraGoldPerShips { get; set; }
        private int ExtraGoldPerBases { get; set; }
        private Queue<MasterCard> Deck { get; set; }
        private List<MasterCard> Hand { get; set; }
        private List<BaseCard> Bases { get; set; }
        private List<MasterCard> DiscardPile { get; set; }
        public Dictionary<Fractions, int> FractionsList { get; private set; }
        private Dictionary<Fractions, List<Property>> InactiveFracProperties { get; set; }
        private List<Property> PropertiesToPlay { get; set; }
        private DecisionMaker DecisionMaker { get; set; }

        private int DealtDamage {  get; set; }

        public Player(Game game, string name = "p1", int R_FracPriority = 100,
                                                    int G_FracPriority = 100,
                                                    int B_FracPriority = 100,
                                                    int Y_FracPriority = 100,
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
            ExtraDamageToShips = 0;
            ExtraDamageToBases = 0;
            ExtraGoldPerShips = 0;
            ExtraGoldPerBases = 0;
            Deck = new Queue<MasterCard>();
            Hand = new List<MasterCard>();
            Bases = new List<BaseCard>();
            DiscardPile = new List<MasterCard>();
            FractionsList = new Dictionary<Fractions, int>();
            InactiveFracProperties = new Dictionary<Fractions, List<Property>>();
            PropertiesToPlay = new List<Property>();
            DecisionMaker = new DecisionMaker(R_FracPriority, G_FracPriority, B_FracPriority, Y_FracPriority, G, D, H, Agr);

            DealtDamage = 0;

            InitDictionaries();
            InitStartDeck();
        }

        public bool IsAlive()
        {
            return Health > 0;
        }
        public void TakeATurn(int startMinusCards = 0)
        {
            //Добор карт
            DrawCards(startMinusCards);

            //Сброс карт
            DiscardCards();

            //Розыгрыш карт из руки
            PlayCardsFromHand();

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

        //Конец хода - обнуление свойств, фракций, золота, урона, сброс карт
        private void EndTurn()
        {
            DiscardPile.AddRange(Hand);
            Hand.Clear();
            Damage.Clear();
            CardsOnTop = 0;
            ExtraDamageToShips = 0;
            ExtraDamageToBases = 0;
            ExtraGoldPerShips = 0;
            ExtraGoldPerBases = 0;
            Gold = 0;
            foreach (var key in FractionsList.Keys)
                FractionsList[key] = 0;
            foreach (var key in InactiveFracProperties.Keys)
                InactiveFracProperties[key].Clear();
            PropertiesToPlay.Clear();

            GetInfo();
            ShowCards();
        }

        //методы для колоды
        //Добор карт
        private void DrawCards(int startMinusCards)
        {
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
        }

        /// <summary>
        /// Сброс карт
        /// </summary>
        private void DiscardCards()
        {
            if (DropCount > 0)
            {
                if (Hand.Count > DropCount)
                {
                    Random rnd = new Random();//TODO
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
        }

        private void PlayCardsFromHand()
        {
            int startHandCount = Hand.Count;
            int i = 0;
            while(i < Hand.Count)
            {
                Hand[i].Play(Game);
                if (Hand.Count == startHandCount)
                    i++;
                else
                    startHandCount = Hand.Count;
            }
        }

        /// <summary>
        /// Перезамешивание карт из сброса в колоду
        /// </summary>
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
        public void ShowCards()
        {
            Console.WriteLine("Deck:");
            foreach (var card in Deck)
                Console.WriteLine($"{card.CardName} | D{(card.Damage != null ? card.Damage : 0)}");
            Console.WriteLine();
            Console.WriteLine("Discard Pile:");
            foreach (var card in DiscardPile)
                Console.WriteLine($"{card.CardName} | D{(card.Damage != null ? card.Damage : 0)}");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("................");
        }
        public void GetInfo()
        {
            Console.WriteLine($"{Name}");
            Console.WriteLine($"{Health}|50 HP");
            Console.WriteLine($"{Gold}Gold");
            Console.WriteLine($"Bases:{Bases.Count}");
            Console.WriteLine($"DealtDamage:{DealtDamage}");
            Console.WriteLine("======================");
        }


        //Добавление базы
        public void AddBase(BaseCard Base)
        {
            Bases.Add(Base);
            Bases = [.. Bases.OrderByDescending(b => b.Strength)];
            Hand.Remove(Base);
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
            //TODO Возможно стоит сортировать свойства?
            while (PropertiesToPlay.Count > 0)
            {
                PropertiesToPlay[0].ActivateProperty(Game);
                PropertiesToPlay.RemoveAt(0);
            }
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
        private Dictionary<int, MasterCard> GetBuyableCardFromMarket()
        {
            Dictionary<int, MasterCard> buyable = new();
            for (int i = 0; i < Game.Market.Count; i++)
                if (Game.Market[i].Price <= Gold)
                    buyable.Add(i, Game.Market[i]);
            return buyable;
        }
        private Dictionary<int, MasterCard> GetBuyableCardFromMarket(int MaxCost)
        {
            Dictionary<int, MasterCard> buyable = new();
            for (int i = 0; i < Game.Market.Count; i++)
                if (Game.Market[i].Price <= MaxCost)
                    buyable.Add(i, Game.Market[i]);
            return buyable;
        }
        private void Buy()
        {
            if (ExtraGoldPerBases > 0)
                Gold += ExtraGoldPerBases * Bases.Count;
            if (ExtraGoldPerShips > 0)
                Gold += ExtraGoldPerShips * Hand.Count;

            Dictionary<int, MasterCard> buyable = GetBuyableCardFromMarket();
            int buyFrom = DecisionMaker.WhatToBuy(buyable);

            if (CardsOnTop > 0)
            {
                CardsOnTop--;
                List<MasterCard> newDeck = [buyable[buyFrom]];
                newDeck.AddRange(Deck);
                Deck = new(newDeck);
            }
            else
            {
                DiscardPile.Add(buyable[buyFrom]);
            }
            Game.RemoveFromMarket(buyFrom);
            Gold -= buyable[buyFrom].Price;
        }

        //Атаковать соперника (и/или его базы)
        private void Attack()
        {
            if (ExtraDamageToShips > 0)
                for (int i = 0; i < Hand.Count; i++)
                    Damage.Add(ExtraDamageToShips);

            if (ExtraDamageToBases > 0)
                for (int i = 0; i < Bases.Count; i++)
                    Damage.Add(ExtraDamageToBases);

            if (Damage.Count > 0)
            {
                Player Enemy = Game.GetEnemy();
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
                DealtDamage = Damage.Sum();
            }
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
        public Property ChooseOne(List<Property> propertiesToChoose) => DecisionMaker.ChooseOneProperty(propertiesToChoose);

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

        //B - уничтожение базы противника
        public void DestroyEnemyBase(Game game, int Counter)
        {
            for (int i = 0; i < Counter; i++)
                DecisionMaker.DestroyBase(game);
        }

        //F - покупка из магазина без траты валюты
        public void FreeBuy(int MaxCost)
        {
            Dictionary<int, MasterCard> buyable = GetBuyableCardFromMarket(MaxCost);
            int buyFrom = DecisionMaker.WhatToBuy(buyable);

            if (CardsOnTop > 0)
            {
                CardsOnTop--;
                List<MasterCard> newDeck = [buyable[buyFrom]];
                newDeck.AddRange(Deck);
                Deck = new(newDeck);
            }
            else
            {
                DiscardPile.Add(buyable[buyFrom]);
            }
            Game.RemoveFromMarket(buyFrom);
        }

        //I - копирование (читай "повторный розыгрыш") корабля из руки
        public void ShipImitation()
        {
            MasterCard CardForImitation = DecisionMaker.ShipImitation(Hand)!; //выбрал карту для копирования
            CardForImitation.Play(Game); //разыграл копию
            CheckFractionsProperies(); //проверил сработо ли какое-то ранее недоступное свойство
        }

        //M - удаление из маркета
        public void RemoveFromMarket(int Counter)
        {
            for (int i = 0; i < Counter; i++)
                Game.RemoveFromMarket(DecisionMaker.RemoveFromMarket(Game));
        }

        //S - Сброс карт (стартовых)
        public int DropCard(int Counter)
        {
            int DropCount = 0;
            int i = 0;
            while (Counter > 0 && i < Hand.Count && DropCount < Counter)
            {
                if (Hand[i].CardName == "Разведчик" || Hand[i].CardName == "Штурмовик")
                {
                    DiscardPile.Add(Hand[i]);
                    Hand.RemoveAt(i);
                    DropCount++;
                }
                else i++;
            }
            return DropCount;
        }

        //T - купленную карту можно положить поверх колоды
        public void AddCardsOnTopCount()
        {
            CardsOnTop++;
        }

        //U - утилизация карты
        public int UtilCard(int Counter)
        {
            int utilCount = 0;
            int i = 0;
            if (DiscardPile.Count > 0)
            {
                while (Counter > 0 && i < DiscardPile.Count)
                {
                    if (DiscardPile[i].CardName == "Разведчик" || DiscardPile[i].CardName == "Штурмовик")
                    {
                        Game.Graveyard.Add(DiscardPile[i]);
                        DiscardPile.RemoveAt(i);
                        utilCount++;
                    }
                    else i++;
                }
            }

            i = 0;
            while (Counter > 0 && i < Hand.Count)
            {
                if (Hand[i].CardName == "Разведчик" || Hand[i].CardName == "Штурмовик")
                {
                    Game.Graveyard.Add(Hand[i]);
                    Hand.RemoveAt(i);
                    utilCount++;
                }
                else i++;
            }
            return utilCount;
        }

        //+S(D1) - Добавление 1 урона каждому кораблю
        public void AddExtraDamageToShips(int Counter)
        {
            ExtraDamageToShips += Counter;
        }

        //+S(G1) - Добавление 1 золотого каждому кораблю
        public void AddExtraGoldToShips(int Counter)
        {
            ExtraGoldPerShips += Counter;
        }

        //+B(D1) - Добавление 1 урона каждой базе
        public void AddExtraDamageToBases(int Counter)
        {
            ExtraDamageToBases += Counter;
        }

        //+B(G1) - Добавление 1 золотого каждомй базе
        public void AddExtraGoldToBases(int Counter)
        {
            ExtraGoldPerBases += Counter;
        }
    }
}
