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
        public Dictionary<Fraction, int> FractionsList { get; private set; }
        private Dictionary<Fraction, List<Property>> InactiveFracProperties { get; set; }
        private List<Property> PropertiesToPlay { get; set; }
        private StaticDecisionMaker DecisionMaker { get; set; }
        public StatisticHolder StaticticHolder { get; set; }

        public Player(Game game, StaticDecisionMaker decisionMaker, string name = "player")
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
            FractionsList = new Dictionary<Fraction, int>();
            InactiveFracProperties = new Dictionary<Fraction, List<Property>>();
            PropertiesToPlay = new List<Property>();

            DecisionMaker = decisionMaker;
            StaticticHolder = new();

            InitDictionaries();
            InitStartDeck();
        }


        public void Restart()
        {
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
            FractionsList = new Dictionary<Fraction, int>();
            InactiveFracProperties = new Dictionary<Fraction, List<Property>>();
            PropertiesToPlay = new List<Property>();

            StaticticHolder = new();

            InitDictionaries();
            InitStartDeck();
        }


        /// <summary>
        /// Жив ли игрок?
        /// </summary>
        /// <returns>Статус игрока</returns>
        public bool IsAlive()
        {
            return Health > 0;
        }

        /// <summary>
        /// Ход игрока
        /// </summary>
        /// <param name="startMinusCards">параметр для начала игры, отвечающий за количество добираемых карт (чтобы первый игрок ходил тремя картами)</param>
        public void TakeATurn(int startMinusCards = 0)
        {
            //Добор карт
            DrawCards(startMinusCards);

            //Сброс карт
            DiscardCards();

            //Активация баз
            BaseActivation();

            //Розыгрыш карт из руки
            PlayCardsFromHand();

            //Проверка активации фракционных свойств
            CheckFractionsProperies();

            //Розыгрыш свойств
            PlayProperties();

            //Использование утиль-свойств
            UseUtilProps();

            //Закупка
            while (CanBuyFromMarket(Gold))
                Buy();

            //На остатки закупаем исследователей
            if (Game.Researchers.Count > 0)
            {
                int researcherPrice = Game.Researchers.FirstOrDefault().Price;
                while (Gold > researcherPrice && Game.Researchers.Count > 0)
                {
                    DiscardPile.Add(Game.Researchers.Dequeue());
                    Gold -= researcherPrice;

                    StaticticHolder.SpendGold(researcherPrice);
                    StaticticHolder.CardPurchased();
                }
            }

            Attack();

            EndTurn();
        }

        // Этапы хода игрока ↓
        /// <summary>
        /// Взятие карт в "руку"
        /// </summary>
        /// <param name="startMinusCards">параметр для начала игры, отвечающий за количество добираемых карт (чтобы первый игрок ходил тремя картами)</param>
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
                    Hand = DecisionMaker.DiscardCards(DropCount, Hand, out List<MasterCard> DropedCards);
                    DiscardPile.AddRange(DropedCards);
                }
                else
                {
                    DiscardPile.AddRange(Hand);
                    Hand.Clear();
                }
            }
        }

        /// <summary>
        /// Активация баз
        /// </summary>
        private void BaseActivation()
        {
            foreach (BaseCard Base in Bases)
            {
                // добавление золота
                if (Base.Gold != null && Base.Gold > 0)
                    this.AddGold((int)Gold);

                // добавление урона
                if (Base.Damage != null && Base.Damage > 0)
                    this.AddDamage((int)Base.Damage);

                // хил
                if (Base.Heal != null && Base.Heal > 0)
                    this.Heal((int)Base.Heal);

                // обработка фракции и свойств, если такие имеются
                if (Base.Fractions != null && Base.Fractions.Count > 0)
                {
                    if (Base.FracProperty != null)
                        this.AddFractionsAndProperties(Base.Fractions, Base.FracProperty);
                }

                if (Base.InitialProperty != null)
                    this.AddInitialProperties(Base.InitialProperty);
            }
        }

        /// <summary>
        /// Розыгрыш карт из руки
        /// </summary>
        private void PlayCardsFromHand()
        {
            int startHandCount = Hand.Count;
            int i = 0;
            while (i < Hand.Count)
            {
                StaticticHolder.PlayCard(Hand[i]);
                Hand[i].Play(this);
                if (Hand.Count == startHandCount)
                    i++;
                else
                    startHandCount = Hand.Count;
            }
        }

        /// <summary>
        /// Проверка фркационных свойств
        /// </summary>
        private void CheckFractionsProperies()
        {
            foreach (var fraction in FractionsList)
            {
                if (fraction.Value >= 2)
                    PropertiesToPlay.AddRange(InactiveFracProperties[fraction.Key]);
            }
        }

        /// <summary>
        /// Розыгрыш свойств
        /// </summary>
        private void PlayProperties()
        {
            //TODO Возможно стоит сортировать свойства?
            while (PropertiesToPlay.Count > 0)
            {
                PropertiesToPlay[0].ActivateProperty(Game);
                PropertiesToPlay.RemoveAt(0);
            }
        }

        //Работа со свойствами ↓
        /// <summary>
        /// Добавление свойств
        /// </summary>
        /// <param name="cardFractions">Какой(-им) фракции(-ям) добавляется свойство</param>
        /// <param name="FracProperty">Добавляемое свойство</param>
        public void AddFractionsAndProperties(List<Fraction> cardFractions, Property FracProperty)
        {
            foreach (var cardFraction in cardFractions)
            {
                FractionsList[cardFraction]++;
                InactiveFracProperties[cardFraction].Add(FracProperty);
            }
        }

        /// <summary>
        /// Добавление врождённого свойства карты в свойства, которые должны быть сыграны
        /// </summary>
        /// <param name="property">Свойство для добавления</param>
        public void AddInitialProperties(Property property)
        {
            if (property.Code != "X")
                PropertiesToPlay.Add(property);
        }
        //Работа со свойствами ↑

        /// <summary>
        /// Использование утиль-свойств
        /// </summary>
        private void UseUtilProps()
        {
            //foreach (MasterCard card in Hand)
            //    if (DecisionMaker.UtilCard(card, this))
            //        card.UtilProperty!.ActivateProperty(Game);

            //foreach (BaseCard Base in Bases)
            //    if (DecisionMaker.UtilCard(Base, this))
            //        Base.UtilProperty!.ActivateProperty(Game);


            var processedCards = new HashSet<MasterCard>();
            while (true)
            {
                // Создаем копию текущего списка Hand
                var cardsToActivate = Hand.ToList();

                // Флаг, указывающий, были ли активированы какие-либо свойства
                bool anyActivated = false;

                foreach (MasterCard card in cardsToActivate)
                {
                    // Проверяем, если карта уже не в руке (например, была удалена или изменена)
                    // или если карта уже была обработана
                    if (!Hand.Contains(card) || processedCards.Contains(card)) continue;

                    if (DecisionMaker.UtilCard(card, this))
                    {
                        card.UtilProperty?.ActivateProperty(Game);
                        anyActivated = true;
                    }

                    // Добавляем карту в список обработанных
                    processedCards.Add(card);
                }

                // Если не было активировано ни одного свойства, выходим из цикла
                if (!anyActivated)
                    break;
            }

            if(Bases.Count > 0)
            {
                var processedBases = new HashSet<BaseCard>();
                while (true)
                {
                    // Создаем копию текущего списка Hand
                    var basesToActivate = Bases.ToList();

                    // Флаг, указывающий, были ли активированы какие-либо свойства
                    bool anyActivated = false;

                    foreach (BaseCard Base in basesToActivate)
                    {
                        // Проверяем, если карта уже не в руке (например, была удалена или изменена)
                        // или если карта уже была обработана
                        if (!Bases.Contains(Base) || processedBases.Contains(Base)) continue;

                        if (DecisionMaker.UtilCard(Base, this))
                        {
                            Base.UtilProperty?.ActivateProperty(Game);
                            anyActivated = true;
                        }

                        // Добавляем карту в список обработанных
                        processedBases.Add(Base);
                    }

                    // Если не было активировано ни одного свойства, выходим из цикла
                    if (!anyActivated)
                        break;
                }
            }
        }

        //Для покупок из маркета ↓
        /// <summary>
        /// Проверка на возможность покупки чего-нибудь из магазина
        /// </summary>
        /// <param name="gold">Количество валюты, которой располагает игрок</param>
        /// <returns>возвращает true в случае, если есть что купить, иначе false</returns>
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

        /// <summary>
        /// Получение списка карт, которых игрок может приобрести (без ограничений)
        /// </summary>
        /// <returns>Возвращает словарь индекса карты в магазине и саму карту</returns>
        private Dictionary<int, MasterCard> GetBuyableCardFromMarket()
        {
            Dictionary<int, MasterCard> buyable = new();
            for (int i = 0; i < Game.Market.Count; i++)
                if (Game.Market[i].Price <= Gold)
                    buyable.Add(i, Game.Market[i]);
            return buyable;
        }

        /// <summary>
        /// Получение списка карт, которых игрок может приобрести (с заданный максимальной стоимостью карты и ограничением на тип карты)
        /// </summary>
        /// <param name="MaxCost">Максимальная стоимость карты</param>
        /// <returns>Возвращает словарь индекса карты в магазине и саму карту</returns>
        private Dictionary<int, MasterCard> GetBuyableCardFromMarket(int MaxCost, CardType? cardType)
        {
            Dictionary<int, MasterCard> buyable = new();
            for (int i = 0; i < Game.Market.Count; i++)
                if (Game.Market[i].Price <= MaxCost && (cardType == null || (cardType != null && Game.Market[i].CardType == cardType)))
                {
                    buyable.Add(i, Game.Market[i]);
                }
            return buyable;
        }

        /// <summary>
        /// Покупка карты
        /// </summary>
        private void Buy()
        {
            int PreBuy_Gold = Gold;

            if (ExtraGoldPerBases > 0)
                Gold += ExtraGoldPerBases * Bases.Count;
            if (ExtraGoldPerShips > 0)
                Gold += ExtraGoldPerShips * Hand.Count;

            Dictionary<int, MasterCard> buyable = GetBuyableCardFromMarket();
            int buyFrom = DecisionMaker.WhatToBuy(buyable);
            if (buyFrom != -1)
            {
                if (CardsOnTop > 0)
                {
                    CardsOnTop--;
                    List<MasterCard> newDeck = [buyable[buyFrom]];
                    newDeck.AddRange(Deck);
                    Deck = new(newDeck);

                    Game.RemoveFromMarket(buyFrom);
                    Gold -= buyable[buyFrom].Price;

                    StaticticHolder.CardPurchased();
                }
                else
                {
                    DiscardPile.Add(buyable[buyFrom]);
                    Game.RemoveFromMarket(buyFrom);
                    Gold -= buyable[buyFrom].Price;

                    StaticticHolder.CardPurchased();
                }
            }

            StaticticHolder.SpendGold(PreBuy_Gold - Gold);
        }
        //Для покупок из маркета ↑

        /// <summary>
        /// Атака соперника и/или его баз(-ы)
        /// </summary>
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
                StaticticHolder.SetAllAvailableDamage(Damage.Sum());

                Player Enemy = Game.GetEnemy();
                if (Enemy.HaveBases())
                {
                    List<BaseCard> EnemyBases = Enemy.GetActiveBases();
                    Dictionary<int, BaseCard> Outposts = new Dictionary<int, BaseCard>();
                    for (int i = 0; i < EnemyBases.Count; i++)
                        if (EnemyBases[i].IsTaunt)
                            Outposts.Add(i, EnemyBases[i]);


                    //Атака аванпостов
                    if (Outposts.Count > 0)
                    {
                        List<int> outpostKeys = Outposts.Keys.ToList();

                        // Проход по списку outpostKeys в обратном порядке
                        for (int OutpostIndex = outpostKeys.Count - 1; OutpostIndex >= 0; OutpostIndex--)
                        {
                            int key = outpostKeys[OutpostIndex];

                            if (FindMinDamageToDefeat(Damage, Outposts[key].Strength) != null)
                            {
                                Damage = FindMinDamageToDefeat(Damage, Outposts[key].Strength)!;
                                Enemy.DestroyBaseAt(key);
                                Outposts.Remove(key);
                            }
                        }
                    }

                    //Если нет аванпостов - атака? обычных баз (решает агрессивность)
                    if (Outposts.Count == 0)
                    {
                        int i = 0;
                        while (EnemyBases.Count > 0 && i < EnemyBases.Count)
                        {
                            if (DecisionMaker.AttackBase() && FindMinDamageToDefeat(Damage, EnemyBases[i].Strength) != null)
                            {
                                Damage = FindMinDamageToDefeat(Damage, EnemyBases[i].Strength)!;
                                Enemy.DestroyBaseAt(i);
                                EnemyBases = Enemy.GetActiveBases();
                            }
                            else i++;
                        }
                        StaticticHolder.SetDamageToEnemy(Damage.Sum());
                        Enemy.TakeDamage(Damage.Sum());
                    }
                }
                else
                {
                    StaticticHolder.SetDamageToEnemy(Damage.Sum());
                    Enemy.TakeDamage(Damage.Sum());
                }
            }
        }

        /// <summary>
        /// Конец хода - обнуление свойств, фракций, золота, урона, сброс карт и прочее
        /// </summary>
        private void EndTurn()
        {
            DiscardPile.AddRange(Hand);
            Hand.Clear();
            Damage.Clear();
            DropCount = 0;
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

            //GetInfo();
            //ShowCards();

            StaticticHolder.EndTurn(Game.excelManager, this);
        }
        // Этапы хода игрока ↑

        //Манипуляции с колодами ↓
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

        /// <summary>
        /// Взятие стартовой руки
        /// </summary>
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

        /// <summary>
        /// Возвращает перемешаную "колоду"
        /// </summary>
        /// <param name="deck">Колода для перемешивания</param>
        /// <returns>Перемешанная колода</returns>
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

        /// <summary>
        /// Инициализация вспомогательных словарей
        /// </summary>
        private void InitDictionaries()
        {
            FractionsList.Add(Guide.Fraction.Слизь, 0);
            FractionsList.Add(Guide.Fraction.Технокульт, 0);
            FractionsList.Add(Guide.Fraction.Торговая_федерация, 0);
            FractionsList.Add(Guide.Fraction.Звездная_империя, 0);

            InactiveFracProperties.Add(Guide.Fraction.Слизь, new());
            InactiveFracProperties.Add(Guide.Fraction.Технокульт, new());
            InactiveFracProperties.Add(Guide.Fraction.Торговая_федерация, new());
            InactiveFracProperties.Add(Guide.Fraction.Звездная_империя, new());
        }
        //Манипуляции с колодами ↑



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
            Console.WriteLine("======================");
        }
        public List<int> GetStrategy()
        {
            return DecisionMaker.StrategyOut();
        }


        // Добавление стандартных ресурсов ↓
        /// <summary>
        /// Добавление валюты
        /// </summary>
        /// <param name="gold">Количество добавляемой валюты</param>
        public void AddGold(int gold)
        {
            Gold += gold;
        }

        /// <summary>
        /// Добавление урона
        /// </summary>
        /// <param name="damage">Величина добавляемого урона</param>
        public void AddDamage(int damage)
        {
            Damage.Add(damage);
            Damage.Sort();
        }

        /// <summary>
        /// Лечение
        /// </summary>
        /// <param name="heal">Величина лечения</param>
        public void Heal(int heal)
        {
            Health += heal;
            StaticticHolder.HealedBy(heal);
        }
        // Добавление стандартных ресурсов ↑

        /// <summary>
        /// Получение урона
        /// </summary>
        /// <param name="damage">Величина получаемого урона</param>
        public void TakeDamage(int damage)
        {
            Health -= damage;
        }


        // Базы и всё, что с ними связано ↓
        /// <summary>
        /// Добавление базы
        /// </summary>
        /// <param name="Base">База, которую игрок добавит себе</param>
        public void AddBase(BaseCard Base)
        {
            Bases.Add(Base);
            Bases = [.. Bases.OrderByDescending(b => b.Strength)];
            Hand.Remove(Base);
        }

        /// <summary>
        /// Проверка на наличие у игрока активных баз
        /// </summary>
        /// <returns>Возвращает true, если есть активные базы, иначе false</returns>
        public bool HaveBases()
        {
            return Bases.Count > 0;
        }

        /// <summary>
        /// Получить список активных баз игрока
        /// </summary>
        /// <returns>Список активных баз игрока</returns>
        public List<BaseCard> GetActiveBases()
        {
            return Bases;
        }

        /// <summary>
        /// Уничтожает базу игрока
        /// </summary>
        /// <param name="index">Индекс для уничтожения базы</param>
        public void DestroyBaseAt(int index)
        {
            Game.Graveyard.Add(Bases[index]);
            StaticticHolder.SetDamageTakenByBases(Bases[index].Strength);
            Bases.RemoveAt(index);
        }

        /// <summary>
        /// Проверка на количество урона для уничтожения базы
        /// </summary>
        /// <param name="AvailableDamage">Урон, которым располагает игрок</param>
        /// <param name="target">Стойкость базы</param>
        /// <returns>Возвращает доступный после уничтожения базы урон или null, если урона не хватает</returns>
        private List<int>? FindMinDamageToDefeat(List<int> AvailableDamage, int target)
        {
            List<int> selectedNumbers = new List<int>();
            int currentSum = 0;

            // Идем по отсортированному списку и добавляем числа, пока не достигнем или не превысим цель
            foreach (int number in AvailableDamage)
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
            List<int> remainingNumbers = AvailableDamage.Except(selectedNumbers).ToList();

            return remainingNumbers;
        }
        // Базы и всё, что с ними связано ↑


        //Для свойств карт ↓
        /// <summary>
        /// Выбор из двух и более свойств
        /// </summary>
        /// <param name="propertiesToChoose">Список свойств, из которых нужно "выбрать" одно</param>
        /// <returns>Выбранное свойство</returns>
        public Property ChooseOne(List<Property> propertiesToChoose) => DecisionMaker.ChooseOneProperty(propertiesToChoose);

        /// <summary>
        /// А-свойство - добор карт
        /// </summary>
        /// <param name="count">Количество активаций свойства</param>
        public void TakeAdditionalCard(int count)
        {
            if (Deck.Count >= count)
            {
                for (int i = 0; i < count; i++)
                {
                    MasterCard TakenCard = Deck.Dequeue();
                    TakenCard.Play(this);
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
                    TakenCard.Play(this);
                    Hand.Add(TakenCard);
                }
            }
        }

        /// <summary>
        /// B-свойство - уничтожение базы противника
        /// </summary>
        /// <param name="Counter">Количество срабатываний свойства</param>
        public void DestroyEnemyBase(int Counter)
        {
            for (int i = 0; i < Counter; i++)
                StaticDecisionMaker.DestroyBase(Game.GetEnemy());
        }

        /// <summary>
        /// F-свойство - покупка из магазина без траты валюты
        /// </summary>
        /// <param name="MaxCost">Максимальная стоимость корабля для покупки</param>
        public void FreeBuy(int MaxCost)
        {
            Dictionary<int, MasterCard> buyable = GetBuyableCardFromMarket(MaxCost, CardType.Ship);
            int buyFrom = DecisionMaker.WhatToBuy(buyable);
            if(buyFrom != -1)
            {
                if (CardsOnTop > 0)
                {
                    CardsOnTop--;
                    List<MasterCard> newDeck = [buyable[buyFrom]];
                    newDeck.AddRange(Deck);
                    Deck = new(newDeck);

                    Game.RemoveFromMarket(buyFrom);
                }
                else
                {
                    DiscardPile.Add(buyable[buyFrom]);
                }
                Game.RemoveFromMarket(buyFrom);

                StaticticHolder.CardPurchased();
            }
        }

        /// <summary>
        /// I-свойство - копирование (читай "повторный розыгрыш") корабля из руки
        /// </summary>
        public void ShipImitation()
        {
            MasterCard CardForImitation = DecisionMaker.ShipImitation(Hand)!; //выбрал карту для копирования
            if (CardForImitation != null)
            {
                CardForImitation.Play(this); //разыграл копию
                CheckFractionsProperies(); //проверил сработо ли какое-то ранее недоступное
            }
        }

        /// <summary>
        /// M-свойство - удаление из маркета
        /// </summary>
        /// <param name="Counter">Количество срабатываний свойства</param>
        public void RemoveFromMarket(int Counter)
        {
            for (int i = 0; i < Counter; i++)
            {
                int indexToRemove = DecisionMaker.RemoveFromMarket(Game);
                if (indexToRemove != -1)
                    Game.RemoveFromMarket(indexToRemove);
            }
        }

        /// <summary>
        /// S-свойство - Сброс карт (стартовых)
        /// </summary>
        /// <param name="Counter">Количество срабатываний свойства</param>
        /// <returns>Количество срабатываний свойства (количество сбросов)</returns>
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

        /// <summary>
        /// T-свойтство - купленную карту можно положить поверх колоды
        /// </summary>
        public void AddCardsOnTopCount()
        {
            CardsOnTop++;
        }

        /// <summary>
        /// U-свойство - утилизация карты
        /// </summary>
        /// <param name="Counter">Количество срабатываний свойства</param>
        /// <returns>Количество срабатываний свойства (утилизированных карт)</returns>
        public int UtilCard(int Counter)
        {
            int utilCount = 0;
            int i = 0;
            //Сначала удалить из стопки сброса, если там есть
            if (DiscardPile.Count > 0)
            {
                while (utilCount < Counter && i < DiscardPile.Count)
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
            //Если можно утилизировать еще, то убираем и из руки
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

        /// <summary>
        /// +S(D1) - Добавление 1 урона каждому кораблю
        /// </summary>
        /// <param name="Counter"></param>
        public void AddExtraDamageToShips(int Counter)
        {
            ExtraDamageToShips += Counter;
        }


        // Свойств ниже нет в игре, но добавлены с расчетом на будущее

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
