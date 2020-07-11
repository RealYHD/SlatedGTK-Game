using SkinnerBox.States.Main;
using SkinnerBox.Utilities;
using SlatedGameToolkit.Framework;
using SlatedGameToolkit.Framework.Logging;

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
            Logger.AddLogListener(new ConsoleLogger());
            GameEngine.Ignite(new MenuState());
        }

    }
}
