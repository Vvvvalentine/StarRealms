using StarRealms.Cards;
using StarRealms.Game;
using static StarRealms.Utility.Guide;

namespace StarRealms.Utility
{
    internal class StaticDecisionMaker
    {
        //Процент агрессии по отношению к базам (100 - атакует все базы, только потом игрока, 0 - игнорирует базы без таунта)
        int Aggressiveness;
        
        //Вероятности выбора фракции
        private Dictionary<Fraction, int> FracPriority { get; set; }

        private Dictionary<CardType, int> CardTypePriority { get; set; }

        //Вероятности выбора свойства карты
        private Dictionary<string, int> PropertiesPriority { get; set; }

        public StaticDecisionMaker(int ShPr, int BPr, int Bp, int Gp, int Rp, int Yp, int G, int D, int H, int Agr = 20)
        {
            if (Agr > 100)
                Aggressiveness = 100;
            else if (Agr < 0)
                Aggressiveness = 0;
            else
                Aggressiveness = Agr;

            CardTypePriority = new();
            FracPriority = new();
            PropertiesPriority = new();

            CardTypePriority.Add(CardType.Ship, ShPr);
            CardTypePriority.Add(CardType.Base,BPr);

            FracPriority.Add(Fraction.Торговая_федерация, Bp);
            FracPriority.Add(Fraction.Слизь, Gp);
            FracPriority.Add(Fraction.Технокульт, Rp);
            FracPriority.Add(Fraction.Звездная_империя, Yp);

            //Стандартные (ресурсные)
            PropertiesPriority.Add("G", G); // Золото
            PropertiesPriority.Add("D", D); // Урон
            PropertiesPriority.Add("H", H); // Хилл

            //Особые
            PropertiesPriority.Add("A", 100); // Добор карт
            PropertiesPriority.Add("B", 100); // Уничтожение базы противника
            PropertiesPriority.Add("F", 100); // Бесплатная покупка корабля
            PropertiesPriority.Add("I", 100); // Полностью копировать свойства корабля
            PropertiesPriority.Add("M", 100); // Удаление карт из маркета
            PropertiesPriority.Add("P", 100); // Противник сбрасывает карту
            PropertiesPriority.Add("S", 100); // Сброс карты
            PropertiesPriority.Add("T", 100); // Положить купленную карту наверх колоды
            PropertiesPriority.Add("U", 100); // Утилизация карт
        }

        public List<int> StrategyOut()
        {
            List<int> Strategy = new List<int>();
            Strategy.Add(CardTypePriority[Guide.CardType.Ship]);
            Strategy.Add(CardTypePriority[Guide.CardType.Base]);

            Strategy.Add(FracPriority[Guide.Fraction.Технокульт]);
            Strategy.Add(FracPriority[Guide.Fraction.Слизь]);
            Strategy.Add(FracPriority[Guide.Fraction.Торговая_федерация]);
            Strategy.Add(FracPriority[Guide.Fraction.Звездная_империя]);

            Strategy.Add(PropertiesPriority["G"]);
            Strategy.Add(PropertiesPriority["D"]);
            Strategy.Add(PropertiesPriority["H"]);

            Strategy.Add(Aggressiveness);

            return Strategy;
        }

        public List<MasterCard> DiscardCards(int numberOfDiscards, List<MasterCard> Hand, out List<MasterCard> discardedCards)
        {
            // Удаляем "Штурмовик" и "Разведчик"
            discardedCards = Hand
                .Where(card => card.CardName == "Штурмовик" || card.CardName == "Разведчик")
                .Take(numberOfDiscards)
                .ToList();

            numberOfDiscards -= discardedCards.Count;

            // Если после удаления "Штурмовик" и "Разведчик" необходимо еще сбросить карты
            if (numberOfDiscards > 0)
            {
                // Подсчитываем количество карт каждой фракции
                var fractionCounts = Hand
                    .Where(card => card.Fractions != null)
                    .SelectMany(card => card.Fractions)
                    .GroupBy(fraction => fraction)
                    .ToDictionary(group => group.Key, group => group.Count());

                // Находим фракции, которые имеют больше одной карты (для срабатывания фракционных свойств)
                var activeFractions = fractionCounts
                    .Where(pair => pair.Value > 1)
                    .Select(pair => pair.Key)
                    .ToHashSet();

                // Отсортируем оставшиеся карты с учетом приоритетов фракций
                var remainingCards = Hand
                    .Except(discardedCards)
                    .OrderBy(card => card.Fractions != null && card.Fractions.Any(f => activeFractions.Contains(f)) ? 1 : 0)
                    .ThenByDescending(card => card.Fractions != null ? card.Fractions.Max(f => FracPriority.TryGetValue(f, out var priority) ? priority : int.MinValue) : int.MinValue)
                    .ThenBy(card => card.CardName) // Вторичная сортировка по имени (на случай, если нужна дополнительная логика)
                    .ToList();


                // Добавляем к списку карт для сброса нужное количество карт
                discardedCards.AddRange(remainingCards.Take(numberOfDiscards));
            }

            // Удаляем карты из руки
            Hand = Hand.Except(discardedCards).ToList();

            return Hand;
        }

        public int WhatToBuy(Dictionary<int, MasterCard> buyableCards)
        {
            List<int> indexes = new(buyableCards.Keys);
            Dictionary<int, int> probabilities = new();
            foreach (int index in indexes)
            {
                int probSum = 0;
                foreach (Fraction fraction in buyableCards[index].Fractions!)
                {
                    probSum += FracPriority[fraction];
                }
                probabilities.Add(index, probSum * CardTypePriority[buyableCards[index].CardType]);
            }

            // Суммируем все вероятности
            int totalProbability = 0;
            foreach (var kvp in probabilities)
            {
                if (kvp.Value < 0)
                {
                    throw new ArgumentException("Вероятность не может быть отрицательной");
                }
                totalProbability += kvp.Value;
            }

            if (totalProbability == 0)
            {
                return -1;
            }

            // Генерируем случайное число от 0 до totalProbability - 1
            Random random = new Random();
            int randomValue = random.Next(totalProbability);

            // Определяем индекс исхода на основе случайного значения
            int cumulativeProbability = 0;
            foreach (var kvp in probabilities)
            {
                cumulativeProbability += kvp.Value;
                if (randomValue < cumulativeProbability)
                {
                    return kvp.Key;
                }
            }

            return -1;
        }
        public bool AttackBase() //TODO?
        {
            Random rnd = new Random();
            int Choice = rnd.Next(101);
            if (Choice < Aggressiveness)
                return true;
            else
                return false;
        }

        public Property ChooseOneProperty(List<Property> propertiesToChoose)
        {
            // Фиксация вероятностей
            List<int> probabilities = new();
            foreach (var property in propertiesToChoose)
            {
                if (PropertiesPriority.TryGetValue(property.Code[0].ToString(), out int PropertyProbability))
                {
                    probabilities.Add(PropertyProbability);
                }
                else
                    probabilities.Add(0);
            }

            // Суммируем все вероятности
            int totalProbability = probabilities.Sum();

            if (totalProbability == 0)
            {
                throw new ArgumentException("Сумма вероятностей равна нулю");
            }

            // Генерируем случайное число от 0 до totalProbability - 1
            Random random = new Random();
            int randomValue = random.Next(totalProbability);

            // Определяем индекс исхода на основе случайного значения
            int cumulativeProbability = 0;
            for (int i = 0; i < probabilities.Count; i++)
            {
                cumulativeProbability += probabilities[i];
                if (randomValue < cumulativeProbability)
                {
                    return propertiesToChoose[i];
                }
            }

            return new Property("X");
        }



        //Для свойств
        //Для B-свойства (уничтожение базы противника)
        public static void DestroyBase(Player Enemy) //TODO? пока уничтожается самая жирная база
        {
            if (Enemy.GetActiveBases().Count > 0)
            {
                int indexOfBaseWithMaximumStrength = 0;
                int max = 0;
                List<BaseCard> EnemyBases = Enemy.GetActiveBases();
                for (int i = 0; i < EnemyBases.Count; i++)
                {
                    max = EnemyBases[i].Strength > max ? EnemyBases[i].Strength : max;
                    indexOfBaseWithMaximumStrength = max < EnemyBases[i].Strength ? indexOfBaseWithMaximumStrength : i;
                }

                Enemy.DestroyBaseAt(indexOfBaseWithMaximumStrength);
            }
        }

        //Возвращает отсортированный по убыванию список фракций
        public List<Fraction> GetSortedFractions()
        {
            return FracPriority.OrderByDescending(kvp => kvp.Value)
                               .Select(kvp => kvp.Key)
                               .ToList();
        }

        //Для I-свойства (копирования корабля)
        public MasterCard? ShipImitation(List<MasterCard> playedCards)
        {
            return playedCards
                .Where(card => card.CardName != "Игла") // Исключение "Иглы", чтобы нельзя было копировать её же
                .Select(card => new
                {
                    Card = card,
                    MaxFractionPriority = card.Fractions?
                                              .Select(f => FracPriority.TryGetValue(f, out var priority) ? priority : int.MinValue)
                                              .DefaultIfEmpty(int.MinValue)
                                              .Max()
                })
                .OrderByDescending(x => x.MaxFractionPriority)
                .ThenByDescending(x => x.Card.Price)
                .Select(x => x.Card)
                .FirstOrDefault();
        }

        //Для M-свойства (удаления карты из магазина)
        public int RemoveFromMarket(Game.Game game) //TODO? убирает из магазина карту с наименьшим приоритетом по фракциям
        {
            List<MasterCard> market = game.Market;
            if (market.Count > 0)
                return market
                    .Select((card, index) => new
                    {
                        Index = index,
                        MinFractionPriority = card.Fractions!
                                                  .Select(f => FracPriority.TryGetValue(f, out var priority) ? priority : int.MaxValue)
                                                  .DefaultIfEmpty(int.MaxValue)
                                                  .Min()
                    })
                    .OrderBy(x => x.MinFractionPriority)
                    .First().Index;
            else return -1;
        }


        //Для утилизации карт
        public bool UtilCard(MasterCard card, Player player)
        {
            // Проверяем, что свойство UtilProperty не равно "X"
            if (card.UtilProperty != null && card.UtilProperty.Code == "X" || card.UtilProperty == null)
                return false;

            // Собираем все значимые свойства карты
            List<string> cardProperties = new List<string>();
            if(card.FracProperty != null)
                cardProperties.AddRange(GetPropertyCodes(card.FracProperty));
            if (card.InitialProperty != null)
                cardProperties.AddRange(GetPropertyCodes(card.InitialProperty));

            if (card.Damage.HasValue && card.Damage.Value != 0)
                cardProperties.Add("D" + card.Damage.Value);

            if (card.Gold.HasValue && card.Gold.Value != 0)
                cardProperties.Add("G" + card.Gold.Value);

            if (card.Heal.HasValue && card.Heal.Value != 0)
                cardProperties.Add("H" + card.Heal.Value);

            // Определяем утилизационное свойство
            string utilProp = card.UtilProperty!.Code;

            char utilPropType = utilProp[0];
            int utilPropCount = int.Parse(utilProp[1].ToString());
            int utilPropWeight = PropertiesPriority.GetValueOrDefault(utilPropType.ToString(), 0) * utilPropCount;

            // Вычисляем суммарный вес всех свойств карты
            int totalWeight = cardProperties.Sum(prop =>
            {
                char propType = prop[0];
                int propCount = int.Parse(prop.Substring(1));
                return PropertiesPriority.GetValueOrDefault(propType.ToString(), 0) * propCount;
            });

            return utilPropWeight > totalWeight;
        }
        List<string> GetPropertyCodes(Property property)
        {
            if (property.Code.Contains(','))
                return property.Code.Split(',').Where(code => code[0] != 'X').ToList();

            if (property.Code.Contains('/'))
                return property.Code.Split('/').Where(code => code[0] != 'X').ToList();

            if (property.Code[0] != 'X')
                return new List<string> { property.Code };

            return new List<string>();
        }
    }
}
