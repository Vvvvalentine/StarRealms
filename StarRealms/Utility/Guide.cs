namespace StarRealms.Utility
{
    static class Guide
    {
        public static readonly Dictionary<string, Fraction> FractionsDict = new()
        {
            {"B", Fraction.Торговая_федерация},
            {"G", Fraction.Слизь},
            {"R", Fraction.Технокульт},
            {"Y", Fraction.Звездная_империя}
        };

        public enum CardType
        {
            Ship,
            Base
        }

        public enum Fraction
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

        public static List<Fraction> GetFractions(string FracCode)
        {
            List<Fraction> fractions = new();
            foreach (char c in FracCode)
            {
                fractions.Add(FractionsDict[c.ToString()]);
            }
            return fractions;
        }

        public static Fraction GetFraction(string FracCode)
        {
            return FractionsDict[FracCode];
        }

        public static string GetReadableFractions(List<Fraction> fractions)
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
