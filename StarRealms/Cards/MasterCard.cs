using StarRealms.Game;
using StarRealms.Utility;

namespace StarRealms.Cards
{
    abstract class MasterCard
    {
        /// <summary>
        /// название карты (взято из оригинальной игры),
        /// </summary>
        public string? CardName { get; set; }

        /// <summary>
        /// цена карты
        /// </summary>
        public int Price { get; set; }

        /// <summary>
        /// список фракций (зачастую, состоит из единственного элемента)
        /// </summary>
        public List<Guide.Fraction>? Fractions { get; set; }

        /// <summary>
        /// фракционное свойство
        /// </summary>
        public Property? FracProperty { get; set; }

        /// <summary>
        /// встроенное свойство (активируется без условий)
        /// </summary>
        public Property? InitialProperty { get; set; }

        /// <summary>
        /// утилизационное свойство
        /// </summary>
        public Property? UtilProperty { get; set; }

        /// <summary>
        /// количество очков торговли, которое даёт данная карта
        /// </summary>
        public int? Gold { get; set; }

        /// <summary>
        /// количество очков боя, которое даёт данная карта
        /// </summary>
        public int? Damage { get; set; }

        /// <summary>
        /// количество очков влияния, которое карта может вернуть игроку
        /// </summary>
        public int? Heal { get; set; }

        /// <summary>
        /// Тип карты (корабль/база)
        /// </summary>
        public Guide.CardType CardType { get; set; }
        /// <summary>
        /// Из какого дополнения карта
        /// </summary>
        public Guide.Adds AddFrom { get; set; }

        /// <summary>
        /// Разыгрыш карты
        /// </summary>
        /// <param name="actPlayer">Игрок, разыгрывающий карту</param>
        public void Play(Player actPlayer)
        {
            if (CardType == Guide.CardType.Base)
            {
                actPlayer.AddBase((BaseCard)this);
            }

            // Добавление золота
            if (Gold != null && Gold > 0)
                actPlayer.AddGold((int)Gold);

            // Добавление урона
            if (Damage != null && Damage > 0)
                actPlayer.AddDamage((int)Damage);

            // Лечение
            if (Heal != null && Heal > 0)
                actPlayer.Heal((int)Heal);

            // Обработка фракции и свойств
            if (Fractions != null && Fractions.Count > 0)
            {
                if (FracProperty != null) //если у карты имеются фракции - добавить их в счетчик игрока
                    actPlayer.AddFractionsAndProperties(Fractions, FracProperty);
            }
            
            // Обработка врожденных свойств
            if (InitialProperty != null)
                actPlayer.AddInitialProperties(InitialProperty);
        }
    }
}
