using Microsoft.Data.Sqlite;
using StarRealms.Cards;
using StarRealms.Game;
using static StarRealms.Utility.Guide;

namespace StarRealms.Utility
{
    internal class StatisticHolder
    {
        public List<int> GoldSpent { get; private set; }
        public List<int> PurchasedCardsCounter { get; private set; }
        public List<int> AllAvailableDamage {  get; private set; }
        public List<int> DamageToEnemy { get; private set; }
        public List<int> DamageTakenByBases {  get; private set; }
        public List<int> Healed {  get; private set; }
        public List<int> PlayedCardsCounter {  get; private set; }
        public int Turn { get; private set; }

        public List<Dictionary<Fraction, int>> FractionsPlayed { get; private set; }
        public Dictionary<string, int> CardsStatistic { get; private set; } // название карты, количество её использования

        public StatisticHolder()
        {
            GoldSpent = new();//////////
            PurchasedCardsCounter = new();
            
            AllAvailableDamage = new();

            DamageToEnemy = new();

            DamageTakenByBases = new();

            Healed = new();
            
            PlayedCardsCounter = new();
            Turn = 1; // по сути - итератор для списков
            
            FractionsPlayed = new();
            FractionsPlayed.Add(InitFractionsPlayedDict());
            
            CardsStatistic = new();
            InitCardsStatistic();
        }
        
        private void InitCardsStatistic()
        {
            using (var connection = new SqliteConnection("Data Source=StarEmps.db"))
            {
                string sql = "select Name from CardNames";
                connection.Open();
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CardsStatistic.Add(reader.GetString(reader.GetOrdinal("Name")), 0);
                        }
                    }
                }

                sql = "select Name from Researchers";
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CardsStatistic.Add(reader.GetString(reader.GetOrdinal("Name")), 0);
                        }
                    }
                }

                sql = "select Name from StartCards";
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CardsStatistic.Add(reader.GetString(reader.GetOrdinal("Name")), 0);
                        }
                    }
                }
                connection.Close();
            }
            
        }
        private Dictionary<Fraction, int> InitFractionsPlayedDict()
        {
            Dictionary<Fraction, int> fractionsPlayed = new();

            fractionsPlayed.Add(Guide.Fraction.Технокульт, 0);
            fractionsPlayed.Add(Guide.Fraction.Слизь, 0);
            fractionsPlayed.Add(Guide.Fraction.Торговая_федерация, 0);
            fractionsPlayed.Add(Guide.Fraction.Звездная_империя, 0);

            return fractionsPlayed;
        }

        public void PlayCard(MasterCard playedCard)
        {
            if (PlayedCardsCounter.Count == Turn) // Если на этом ходу уже были записи
                PlayedCardsCounter[Turn-1]++;
            else
                PlayedCardsCounter.Add(1);

            if (playedCard.Fractions != null)
            {
                if (FractionsPlayed.Count == Turn) // Если на этом ходу уже были записи
                {
                    foreach (Fraction frac in playedCard.Fractions)
                    {
                        FractionsPlayed[Turn-1][frac]++;
                    }
                }
                else // Нужно добавить новый словарь перед добавлением данных
                {
                    FractionsPlayed.Add(InitFractionsPlayedDict());
                    foreach (Fraction frac in playedCard.Fractions)
                    {
                        FractionsPlayed[Turn-1][frac]++;
                    }
                }
            }

            CardsStatistic[playedCard.CardName]++;
        }

        public void EndTurn()
        {
            if (AllAvailableDamage.Count != Turn)
                AllAvailableDamage.Add(0);

            if (DamageToEnemy.Count != Turn)
                DamageToEnemy.Add(0);

            if (DamageTakenByBases.Count != Turn)
                DamageTakenByBases.Add(0);

            if (Healed.Count != Turn)
                Healed.Add(0);

            if (FractionsPlayed.Count != Turn)
                FractionsPlayed.Add(InitFractionsPlayedDict());

            if (PurchasedCardsCounter.Count != Turn)
                PurchasedCardsCounter.Add(0);

            Turn++;
        }

        public void EndTurn(ExcelManager excelManager, Player player)
        {
            if (GoldSpent.Count != Turn)
                GoldSpent.Add(0);

            if (PurchasedCardsCounter.Count != Turn)
                PurchasedCardsCounter.Add(0);

            if (AllAvailableDamage.Count != Turn)
                AllAvailableDamage.Add(0);

            if (DamageToEnemy.Count != Turn)
                DamageToEnemy.Add(0);

            if (DamageTakenByBases.Count != Turn)
                DamageTakenByBases.Add(0);

            if (Healed.Count != Turn)
                Healed.Add(0);

            if (PlayedCardsCounter.Count != Turn)
                PlayedCardsCounter.Add(0);

            if (FractionsPlayed.Count != Turn)
                FractionsPlayed.Add(InitFractionsPlayedDict());

            Turn++;
        }

        public List<int> GetTurnStats(int turn)
        {
            List<int> stats = new List<int>();
            if (turn + 1 < Turn)
            {
                stats.Add(GoldSpent[turn]);
                stats.Add(PurchasedCardsCounter[turn]);
                stats.Add(AllAvailableDamage[turn]);
                stats.Add(DamageToEnemy[turn]);
                stats.Add(DamageTakenByBases[turn]);
                stats.Add(Healed[turn]);
                stats.Add(PlayedCardsCounter[turn]);
                stats.Add(1);
                stats.Add(FractionsPlayed[turn][Fraction.Технокульт]);
                stats.Add(FractionsPlayed[turn][Fraction.Слизь]);
                stats.Add(FractionsPlayed[turn][Fraction.Торговая_федерация]);
                stats.Add(FractionsPlayed[turn][Fraction.Звездная_империя]);
            }
            return stats;
        }

        public void SpendGold(int gold)
        {
            if (GoldSpent.Count == Turn) // Если на этом ходу уже были записи
                GoldSpent[Turn-1] += gold;
            else
                GoldSpent.Add(gold);
        }

        public void HealedBy(int Heal)
        {
            if (Healed.Count == Turn) // Если на этом ходу уже были записи
                Healed[Turn-1] += Heal;
            else
                Healed.Add(Heal);
        }

        public void SetAllAvailableDamage(int Damage)
        {
            AllAvailableDamage.Add(Damage);
        }

        public void SetDamageToEnemy(int Damage)
        {
            if (DamageToEnemy.Count == Turn) // Если на этом ходу уже были записи
                DamageToEnemy[Turn-1] += Damage;
            else
                DamageToEnemy.Add(Damage);
        }

        public void SetDamageTakenByBases(int Damage)
        {
            if (DamageTakenByBases.Count == Turn) // Если на этом ходу уже были записи
                DamageTakenByBases[Turn-1] += Damage;
            else
                DamageTakenByBases.Add(Damage);
        }

        public void CardPurchased()
        {
            if (PurchasedCardsCounter.Count == Turn) // Если на этом ходу уже были записи
                PurchasedCardsCounter[Turn-1]++;
            else
                PurchasedCardsCounter.Add(1);
        }




    }
}
