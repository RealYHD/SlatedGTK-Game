using SkinnerBox.States.Main;
using SlatedGameToolkit.Framework;

namespace SkinnerBox
{
    class Game
    {
        public const int WIDTH_UNITS = 8;
        public const int HEIGHT_UNITS = 8;
        static void Main(string[] args)
        {
            GameEngine.targetFPS = 0;
            GameEngine.UpdatesPerSecond = 20;
            GameEngine.Ignite(new MenuState());
        }

    }
}
