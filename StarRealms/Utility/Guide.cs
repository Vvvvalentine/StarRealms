namespace StarRealms.Utility
{
    static class Guide
    {
        public static readonly Dictionary<string, Fractions> FractionsDict = new()
        {
            {"G", Fractions.Слизь},
            {"B", Fractions.Торговая_федерация},
            {"R", Fractions.Технокульт},
            {"Y", Fractions.Звездная_империя}
        };

        public enum CardTypes
        {
            Ship,
            Base
        }

        public enum Fractions
        {
            Слизь,
            Торговая_федерация,
            Технокульт,
            Звездная_империя
        }

        public enum Adds
        {
            Стандарт
        }

        public static List<Fractions> GetFractions(string FracCode)
        {
            List<Fractions> fractions = new();
            foreach (char c in FracCode)
            {
                fractions.Add(FractionsDict[c.ToString()]);
            }
            return fractions;
        }

        public static string GetReadableFractions(List<Fractions> fractions)
        {
            string stringFractions = "";
            if (fractions != null)
            {
                foreach (var fraction in fractions)
                {
                    stringFractions += fraction.ToString().Replace("_", " ") + " ";
                }
                return stringFractions;
            }
            return stringFractions;
        }

    }
}
