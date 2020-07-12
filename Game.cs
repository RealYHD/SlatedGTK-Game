using SlatedGameToolkit.Framework;
using SlatedGameToolkit.Framework.Logging;
using WebsiteSim.States;
using WebsiteSim.Utilities;

namespace WebsiteSim
{
    class Game
    {
        public const int WIDTH_UNITS = 8;
        public const int HEIGHT_UNITS = 8;
        static void Main(string[] args)
        {
            GameEngine.targetFPS = 120;
            GameEngine.UpdatesPerSecond = 40;
            Logger.AddLogListener(new ConsoleLogger());
            GameEngine.Ignite(new MenuState());
        }

    }
}
