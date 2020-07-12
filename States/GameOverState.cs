using System;
using System.Drawing;
using System.Numerics;
using SlatedGameToolkit.Framework.AssetSystem;
using SlatedGameToolkit.Framework.Graphics.Render;
using SlatedGameToolkit.Framework.Graphics.Text;
using SlatedGameToolkit.Framework.StateSystem;
using SlatedGameToolkit.Framework.StateSystem.States;

namespace SkinnerBox.States
{
    public class GameOverState : IState
    {
        private MeshBatchRenderer renderer;
        private AssetManager assets;
        BitmapFont font;
        BitmapFont titleFont;
        private StateManager manager;
        
        private int score;
        private TimeSpan timeElapsed;
        private int downloadsServed;
        private int totalDownloads;
        private int packetsReceived;
        private int totalPackets;
        public GameOverState(BitmapFont font, BitmapFont titleFont, MeshBatchRenderer renderer, AssetManager assets) {
            this.font = font;
            this.assets = assets;
            this.titleFont = titleFont;
            this.renderer = renderer;
        }
        public bool Activate()
        {
            titleFont.PixelHeight = 120;
            titleFont.PrepareCharacterGroup("GameOvr!".ToCharArray());
            this.font.PixelHeight = 48;
            font.PrepareCharacterGroup("01234567890.Your Stats: Score,ServerUp-timedownloadservedpacketssentWebsitePDRLN%que".ToCharArray());
            return true;
        }

        public bool Deactivate()
        {
            return true;
        }

        public void Deinitialize()
        {
        }

        public string getName()
        {
            return "GameOver";
        }

        public void Initialize(StateManager manager)
        {
            this.manager = manager;
        }

        public void Render(double delta)
        {
            renderer.Begin(Matrix4x4.Identity, delta);
            titleFont.WriteLine(renderer, 1.95f, Game.HEIGHT_UNITS * 0.75f, "Game Over!", Color.Purple);
            font.WriteLine(renderer, 1.95f, Game.HEIGHT_UNITS * 0.6f, "Score: " + score, Color.Black);
            font.WriteLine(renderer, 1.95f, Game.HEIGHT_UNITS * 0.53f, "Website Uptime: " + timeElapsed.ToString("h\\:mm\\:ss"), Color.Black);
            font.WriteLine(renderer, 1.95f, Game.HEIGHT_UNITS * 0.46f, "Packets Received: " + totalPackets + " Packet Loss: " + Math.Round((100f * (1f - ((float) packetsReceived / totalPackets))), 1) + "%", Color.Black);
            font.WriteLine(renderer, 1.95f, Game.HEIGHT_UNITS * 0.39f, "Downloads Served: " + downloadsServed, Color.Black);
            font.WriteLine(renderer, 1.95f, Game.HEIGHT_UNITS * 0.32f, "Downloads Requested: " + totalDownloads, Color.Black);

            renderer.End();
        }

        public void SetStats(int score, float secondsElapsed, int downloads, int totalDownloads, int packetsReceived, int totalPackets) {
            this.score = score;
            this.timeElapsed = TimeSpan.FromSeconds(secondsElapsed);
            this.downloadsServed = downloads;
            this.totalPackets = totalPackets;
            this.packetsReceived = packetsReceived;
            this.totalDownloads = totalDownloads;
        }

        public void Update(double timeStep)
        {
        }
    }
}