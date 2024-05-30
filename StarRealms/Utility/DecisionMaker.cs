using StarRealms.Cards;
using static StarRealms.Utility.Guide;

namespace StarRealms.Utility
{
    internal class DecisionMaker
    {
        //Процент агрессии по отношению к базам (100 - атакует все базы, только потом игрока, 0 - игнорирует базы без таунта)
        int Aggressiveness;
        //Вероятности выбора фракции
        private Dictionary<Fractions, int> FracPriority { get; set; }

        //Вероятности выбора свойства карты
        private Dictionary<string, int> PropertiesPriority { get; set; }

        public DecisionMaker(int Rp, int Gp, int Bp, int Yp, int G, int D, int H, int Agr = 20)
        {
            if(Agr > 100)
                Aggressiveness = 100;
            else if(Agr < 0)
                Aggressiveness = 0;
            else
                Aggressiveness = Agr;
            
            FracPriority = new();
            PropertiesPriority = new();

            FracPriority.Add(Guide.Fractions.Технокульт, Rp);
            FracPriority.Add(Guide.Fractions.Слизь, Gp);
            FracPriority.Add(Guide.Fractions.Торговая_федерация, Bp);
            FracPriority.Add(Guide.Fractions.Звездная_империя, Yp);

            //Стандартные (ресурсные)
            PropertiesPriority.Add("G", G);
            PropertiesPriority.Add("D", D);
            PropertiesPriority.Add("H", H);

            //Особые
            PropertiesPriority.Add("A", 100); // Добор карт
            PropertiesPriority.Add("U", 100); // Утилизация карт
            PropertiesPriority.Add("M", 100); // Удаление карт из маркета
            PropertiesPriority.Add("B", 100); // Уничтожение базы противника
            PropertiesPriority.Add("F", 100); // Бесплатная покупка корабля
            PropertiesPriority.Add("T", 100); // Положить купленную карту наверх колоды
            PropertiesPriority.Add("P", 100); // Противник сбрасывает карту
            PropertiesPriority.Add("S", 100); // Сброс карты
        }

        public int WhatToBuy(Dictionary<int, MasterCard> buyableCards)
        {
            List<int> indexes = new(buyableCards.Keys);
            Dictionary<int, int> probabilities = new();
            foreach (int index in indexes)
            {
                int probSum = 0;
                foreach (Fractions fraction in buyableCards[index].Fractions!)
                {
                    probSum += FracPriority[fraction];
                }
                probabilities.Add(index, probSum);
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
                throw new ArgumentException("Сумма вероятностей равна нулю");
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
        public bool AttackBase()
        {
            Random rnd = new Random();
            int Choice = rnd.Next(101);
            if (Choice < Aggressiveness)
                return true;
            else
                return false;
        }
        public Property? ChooseOneProperty(List<Property> propertiesToChoose)
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

            return null;
        }
    }
}
