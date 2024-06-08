using StarRealms.Game;
using System.Text.RegularExpressions;

namespace StarRealms.Utility
{
    internal class Property(string code)
    {
        public string Code { get; set; } = code;

        public void ActivateProperty(Game.Game game)
        {
            Player ActivePlayer = game.GetActivePlayer();

            switch (MatchPattern(Code))
            {
                // Ошибка, не найдено ни одного совпадения
                case 0:
                    Console.WriteLine("Ошибка, не найдено ни одного совпадения");
                    break;
                // X - нет свойства, скип
                case 1:
                    break;
                // AN - простое свойство с количеством срабатываний
                case 2:
                    ActivateSimpleProperty(Code, game);
                    break;
                // ...,... - два свойства, оба активировать
                case 3:
                    foreach (string code in Code.Split(","))
                    {
                        Property NewProperty = new Property(code);
                        NewProperty.ActivateProperty(game);
                    }
                    break;
                // .../... (свойство на выбор - вилка)
                case 4:
                    List<Property> PropertiesToChoose = new List<Property>();
                    foreach (string code in Code.Split("/"))
                    {
                        PropertiesToChoose.Add(new Property(code));
                    }
                    game.GetActivePlayer().ChooseOne(PropertiesToChoose).ActivateProperty(game);
                    break;
                // ...N(...) (определённое количество срабатываний свойства)
                case 5:
                    Property PropertyForActivation;
                    Match CounterMatch = new Regex(@"(.+)N\((.+)\)").Match(Code);
                    string ConditionInBrackets = CounterMatch.Groups[2].Value; // выделяет условие из скобок
                    switch (MatchConditionPattern(ConditionInBrackets, out Match match))
                    {
                        case 0: // Ошибка
                            break;
                        case 1: // Сложный условный (?AN; N+; N-)
                            string Condition = match.Groups[1].Value;
                            string Success = match.Groups[2].Value;
                            string Defeat = match.Groups[3].Value;
                            if (ConditionHandler(Condition, game))
                                Condition = CounterMatch.Groups[1].Value + Success;
                            else
                                Condition = CounterMatch.Groups[1].Value + Defeat;
                            PropertyForActivation = new Property(Condition);
                            PropertyForActivation.ActivateProperty(game);
                            break;
                        case 2: // Простой условный ?Prop
                            string Prop = match.Groups[1].Value;
                            if ("RGBY".Contains(Prop))
                            {
                                int ActivationCount = ActivePlayer.FractionsList[Guide.GetFraction(Prop)];
                                PropertyForActivation = new Property(CounterMatch.Groups[1].Value + ActivationCount);
                                PropertyForActivation.ActivateProperty(game);
                            }
                            break;
                        case 3: // Количественный
                            int Count = ActivateSimpleProperty(ConditionInBrackets, game);
                            PropertyForActivation = new Property(CounterMatch.Groups[1].Value + Count);
                            PropertyForActivation.ActivateProperty(game);
                            break;
                    }
                    break;
                // +S/B(...) (прибавка характеристик кораблям / базам)
                case 6:
                    Match PlusMatch = new Regex(@"\+(.+)\((.+)\)").Match(Code);
                    string ShipOrBase = PlusMatch.Groups[1].Value; // выделяет строку из скобок
                    switch (ShipOrBase)
                    {
                        case "S":
                            string ShipBonusType = PlusMatch.Groups[2].Value[0].ToString();
                            int ShipBonusValue = Convert.ToInt32(PlusMatch.Groups[2].Value[1].ToString());
                            switch (ShipBonusType)
                            {
                                case "D":
                                    ActivePlayer.AddExtraDamageToShips(ShipBonusValue);
                                    break;
                                case "G":
                                    ActivePlayer.AddExtraGoldToShips(ShipBonusValue);
                                    break;
                            }
                            break;
                        case "B":
                            string BaseBonusType = PlusMatch.Groups[2].Value[0].ToString();
                            int BaseBonusValue = Convert.ToInt32(PlusMatch.Groups[2].Value[1]);
                            switch (BaseBonusType)
                            {
                                case "D":
                                    ActivePlayer.AddExtraDamageToBases(BaseBonusValue);
                                    break;
                                case "G":
                                    ActivePlayer.AddExtraGoldToBases(BaseBonusValue);
                                    break;
                            }
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        private static int MatchPattern(string input)
        {
            // Паттерн 1: X (в строке только символ "X") - нет свойств
            if (Regex.IsMatch(input, @"^X$"))
            {
                return 1;
            }

            // Паттерн 2: AN, где первый символ - буква (обозначение свойства), а второй - число (количество срабатываний)
            if (Regex.IsMatch(input, @"^[A-Z](?:[1-9][0-9]?|0)$"))
            {
                return 2;
            }

            // Паттерн 3: ...,... (два свойства)
            if (Regex.IsMatch(input, @"^.+,.+$"))
            {
                return 3;
            }

            // Паттерн 4: .../... (свойство на выбор - вилка)
            if (Regex.IsMatch(input, @"^.+/.+$"))
            {
                return 4;
            }

            // Паттерн 5: ...N(...) (определённое количество срабатываний свойства)
            if (Regex.IsMatch(input, @".+N\(.+\)"))
            {
                return 5;
            }

            // Паттерн 6: +S(...) (прибавка характеристик кораблям / базам) (тут после плюса могут быть только буквы S и B)
            if (Regex.IsMatch(input, @"^\+[SsBb]\(.+\)$"))
            {
                return 6;
            }

            // Если ни один паттерн не совпадает
            return 0;
        }

        private int ActivateSimpleProperty(string Code, Game.Game game)
        {
            Player activePlayer = game.GetActivePlayer();
            Player Enemy = game.GetEnemy();
            string Prop = Code[0].ToString();
            int Counter = Convert.ToInt32(Code[1].ToString());
            switch (Prop)
            {
                case "A": // Добор карт
                    activePlayer.TakeAdditionalCard(Counter);
                    break;
                case "B": // Уничтожение базы противника
                    activePlayer.DestroyEnemyBase(Counter);
                    break;
                case "F": // Бесплатная покупка корабля
                    activePlayer.FreeBuy(Counter);
                    break;
                case "I": // Полностью копировать свойства корабля
                    activePlayer.ShipImitation();
                    break;
                case "M": // Удаление карт из маркета
                    activePlayer.RemoveFromMarket(Counter);
                    break;
                case "P": // Противник сбрасывает карту на своём ходу
                    Enemy.DropCount++;
                    break;
                case "S": // Сброс карты
                    return activePlayer.DropCard(Counter);
                case "T": // Положить купленную карту наверх колоды
                    activePlayer.AddCardsOnTopCount();
                    break;
                case "U": // Утилизация карт
                    return activePlayer.UtilCard(Counter);

                case "G": // Золото
                    activePlayer.AddGold(Counter);
                    break;
                case "D": // Урон
                    activePlayer.AddDamage(Counter);
                    break;
                case "H": // Лечение
                    activePlayer.Heal(Counter);
                    break;
            }
            return 0;
        }

        private bool ConditionHandler(string FullCondition, Game.Game game)
        {
            Match Match = new Regex(@"^([A-Z]+)(\d+)$").Match(FullCondition);
            string Condition = Match.Groups[1].Value; // свойство
            int Count = Convert.ToInt32(Match.Groups[2].Value); // количество активаций

            switch (Condition[0])
            {
                case 'O':
                    return game.GetActivePlayer().GetActiveBases().Count >= Count;
            }
            return false;
        }
        private static int MatchConditionPattern(string input, out Match match)
        {
            // Паттерн 1: сложный условный (?AN; N+; N-)
            if (Regex.IsMatch(input, @"^\?[A-Z]\d;\d;\d$"))
            {
                match = new Regex(@"^\?([A-Z]\d);(\d);(\d)$").Match(input);
                return 1;
            }

            // Паттерн 2: простой условный ?Prop
            if (Regex.IsMatch(input, @"^\?[A-Z]$"))
            {
                match = new Regex(@"^\?([A-Z])$").Match(input);
                return 2;
            }

            // Паттерн 3: по количеству срабатываний
            if (Regex.IsMatch(input, @"^[A-Z]\d$"))
            {
                match = new Regex(@"^([A-Z])(\d)$").Match(input);
                return 3;
            }
            match = null;
            return 0;
        }
            
    }
}
