using StarRealms.Game;
using System.Reflection.Emit;

namespace StarRealms.Utility
{
    internal class Property(string code)
    {
        public string Code { get; set; } = code;

        public void ActivateProperty(Game.Game game)
        {
            Player ActualPlayer = game.getActivePlayer();

            if (Code.Contains('/'))
            {
                List<string> propCode = [.. Code.Split('/')];
                List<Property> props = new List<Property>();
                for (int i = 0; i < propCode.Count; i++)
                {
                    props.Add(new Property(propCode[i]));
                }

                ActualPlayer.ChooseOne(props);
            }
            else if (Code.Contains(','))
            {
                List<string> propertyCodes = [.. Code.Split(',')];
                List<Property> myClassList = propertyCodes.Select(code => new Property(code)).ToList();
            }


        }

        private static int Counter(string countCode)
        {

            Convert.ToInt32(countCode);
            return 0;
        }
    }
}
