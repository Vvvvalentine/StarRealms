using StarRealms.Game;
using StarRealms.Utility;

namespace StarRealms.Cards
{
    abstract class MasterCard
    {
        public string? CardName { get; set; }
        public int Price { get; set; }
        public List<Guide.Fraction>? Fractions { get; set; }
        public Property? FracProperty { get; set; }
        public Property? InitialProperty { get; set; }
        public Property? UtilProperty { get; set; }
        public int? Gold { get; set; }
        public int? Damage { get; set; }
        public int? Heal { get; set; }
        public Guide.CardType CardType { get; set; }
        public Guide.Adds AddFrom { get; set; }

        // разыгрыш карты
        public void Play(Player actPlayer)
        {
            if (CardType == Guide.CardType.Base)
            {
                actPlayer.AddBase((BaseCard)this);
            }

            // добавление золота
            if (Gold != null && Gold > 0)
                actPlayer.AddGold((int)Gold);

            // добавление урона
            if (Damage != null && Damage > 0)
                actPlayer.AddDamage((int)Damage);

            // хил
            if (Heal != null && Heal > 0)
                actPlayer.Heal((int)Heal);

            // обработка фракции и свойств, если такие имеются
            if (Fractions != null && Fractions.Count > 0)
            {
                if (FracProperty != null)
                    actPlayer.AddFractionsAndProperties(Fractions, FracProperty);
            }

            if (InitialProperty != null)
                actPlayer.AddInitialProperties(InitialProperty);
        }

        /// <summary>
        /// Использование утиль-свойства с процентом срабатывания
        /// </summary>
        /// <param name="game">ссылка на игру</param>
        /// <param name="percent">процент использования утилизации</param>
        public void Util(Game.Game game, int percent)
        {
            Random rnd = new Random();
            if (percent > 0 && percent < 100)
            {
                Player actPlayer = game.GetActivePlayer();
                if (rnd.Next() * 100 < percent)
                    actPlayer.AddInitialProperties(UtilProperty!);
            }
        }
    }
}
