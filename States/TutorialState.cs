using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using SDL2;
using WebsiteSim.Entities;
using SlatedGameToolkit.Framework.AssetSystem;
using SlatedGameToolkit.Framework.Graphics.Render;
using SlatedGameToolkit.Framework.Graphics.Text;
using SlatedGameToolkit.Framework.Graphics.Textures;
using SlatedGameToolkit.Framework.Graphics.Window;
using SlatedGameToolkit.Framework.Input.Devices;
using SlatedGameToolkit.Framework.StateSystem;
using SlatedGameToolkit.Framework.StateSystem.States;
using SlatedGameToolkit.Framework.Utilities.Collections.Pooling;

namespace WebsiteSim.States
{
    public class TutorialState : IState
    {
        private MeshBatchRenderer renderer;
        private AssetManager assets;
        private StateManager stateManager;
        private ServerEntity serverEntity;

        private int bandwithBoost, speedBoost;

        // Warning entities
        private ObjectPool<WarningEntity> warningPool;
        private List<WarningEntity> activeWarnings = new List<WarningEntity>();

        //Packet entities
        private ObjectPool<PacketEntity> packetPool;
        private List<PacketEntity> activePackets = new List<PacketEntity>();
        private PacketSpawnInfo packetSpawnInfo;

        //Download entity
        private DownloadEntity downloadEntity;

        //Status stuff
        RectangleMesh bandwithMesh, speedMesh;

        //Cursor stuff
        private int viewHeight;
        private float cursorWidthScale;
        private float cursorHeightScale;
        private float serverTargetPos;

        private BitmapFont font;

        public TutorialState(MeshBatchRenderer renderer, AssetManager assets) {
            this.assets = assets;
            packetPool = new ObjectPool<PacketEntity>(CreatePacket);
            warningPool = new ObjectPool<WarningEntity>(createWarning);
            font = new BitmapFont("resources/BigShouldersDisplay-Thin.ttf");
            font.PixelHeight = 48;
            font.PixelsPerUnitHeight = 80;
            font.PixelsPerUnitWidth = 80;
            font.PrepareCharacterGroup("abcdefghijklmnopqrstuvwy.:MSHIP+,DTY\'!DDOSB".ToCharArray());
            this.renderer = renderer;
        }

        public PacketEntity CreatePacket() {
            return new PacketEntity((Texture)assets["packet.png"]);
        }

        public WarningEntity createWarning() {
            return new WarningEntity((Texture)assets["warning.png"]);
        }

        public bool Activate()
        {
            Keyboard.keyboardUpdateEvent += KeyInputListener;

            serverEntity = new ServerEntity((Texture)assets["serverunit.png"], Game.WIDTH_UNITS / 2f, 1/10f);
            downloadEntity = new DownloadEntity((Texture)assets["drag.png"], (Texture)assets["downloadbar.png"]);
            bandwithMesh = new RectangleMesh(new RectangleF(0, Game.HEIGHT_UNITS - 0.75f, 0.5f, 0.5f), (ITexture)assets["serverunit.png"], Color.White);
            speedMesh = new RectangleMesh(bandwithMesh.Bounds, (ITexture)assets["ram.png"], Color.White);

            int vw, vh, vx, vy;
            WindowContextsManager.CurrentGL.GetViewport(out vx, out vy, out vw, out vh);
            CalculateScaleFactors(vw, vh);
            packetSpawnInfo = new PacketSpawnInfo(4, 1, Game.WIDTH_UNITS * 0.8f, 1f, 1, 1);

            serverTargetPos = Game.WIDTH_UNITS / 2f;

            serverEntity.Speed = 4;
            bandwithBoost = 0;
            speedBoost = 3;
            return true;
        }

        public bool Deactivate()
        {
            Keyboard.keyboardUpdateEvent -= KeyInputListener;
            return true;
        }

        public void Deinitialize()
        {
            font.Dispose();
        }

        public string getName()
        {
            return "Tutorial";
        }

        public void Initialize(StateManager manager)
        {
            WindowContextsManager.CurrentWindowContext.resizeEvent += WindowResize;
            this.stateManager = manager;
        }

        public void Render(double delta)
        {
            renderer.Begin(Matrix4x4.Identity, delta);
            #region WarningRender
            foreach (WarningEntity warn in activeWarnings)
            {
                renderer.Draw(warn);
            }
            #endregion
            #region PacketRender
            foreach (PacketEntity packet in activePackets)
            {
                renderer.Draw(packet);
            }
            #endregion
            renderer.Draw(downloadEntity);
            renderer.Draw(downloadEntity.progressMesh);
            renderer.Draw(serverEntity);

            #region StatusRender
            for (int i = 0; i < bandwithBoost + speedBoost; i++)
            {
                if (i < bandwithBoost) {
                    bandwithMesh.X = 0.25f + (i * (bandwithMesh.Width + 0.2f));
                    renderer.Draw(bandwithMesh);
                } else {
                    speedMesh.X = 0.25f + (i * (speedMesh.Width + 0.2f));
                    renderer.Draw(speedMesh);
                }
            }
            #endregion
            #region Text
            font.WriteLine(renderer, 0.1f, Game.HEIGHT_UNITS - 1f, "Move server: left click", Color.Black);
            font.WriteLine(renderer, 0.1f, Game.HEIGHT_UNITS - 1.5f, "Switch between bandwith and units: shift or space", Color.Black);
            font.WriteLine(renderer, 0.1f, Game.HEIGHT_UNITS - 2.0f, "Download bar: right click and drag. You can't go too fast.", Color.Black);
            font.WriteLine(renderer, 0.1f, Game.HEIGHT_UNITS - 2.5f, "Hit the packets and serve the downloads.", Color.Purple);
            font.WriteLine(renderer, 0.1f, Game.HEIGHT_UNITS - 3.0f, "Press shift + space to continue to game!", Color.DarkOrange);
            font.WriteLine(renderer, 0.1f, Game.HEIGHT_UNITS - 5f, "If a download fades, or\n packets pass the server, \nthe users will be disappointed!", Color.Black);
            font.WriteLine(renderer, 0.1f, 1, "Beware of the DDOS...", Color.Gray);            
            #endregion
            renderer.End();
        }

        public void Update(double timeStep)
        {
            #region PacketUpdate
            packetSpawnInfo.timeElapsed += (float) timeStep;
            if (packetSpawnInfo.timeElapsed >= packetSpawnInfo.interval) {
                packetSpawnInfo.timeElapsed = 0;
                //do spawning
                for(int i = 0; i < packetSpawnInfo.perSpawn; i++) {
                    PacketEntity packet = packetPool.Retrieve();
                    packet.CenterX = packetSpawnInfo.batchLocation;
                    packet.Y = i * packet.Height + packetSpawnInfo.range + Game.HEIGHT_UNITS + packetSpawnInfo.speed;
                    packet.velocity = packetSpawnInfo.speed;
                    activePackets.Add(packet);
                }

                //Spawn Warning
                WarningEntity warning = warningPool.Retrieve();
                warning.CenterX = packetSpawnInfo.batchLocation;
                warning.LifeTime = 1f;
                warning.Y = Game.HEIGHT_UNITS - warning.Height;
                warning.mesh.Y = warning.Y;
                warning.mesh.X = warning.X;
                activeWarnings.Add(warning);

            }

            for (int i = 0; i < activePackets.Count; i++)
            {
                PacketEntity packet = activePackets[i];
                packet.Update(timeStep);
                if (packet.HitBox.IntersectsWith(serverEntity.HitBox) && packet.velocity > 0) {
                    packet.velocity *= -2.5f;
                    packet.Color = Color.Gray;
                }
                if (packet.Y <= 0 - packet.Height) {
                    packetPool.Release(packet);
                    activePackets.RemoveAt(i);
                    i--;
                    continue;
                }
                if (packet.Y >= Game.HEIGHT_UNITS && packet.velocity < 0) {
                    packetPool.Release(packet);
                    activePackets.RemoveAt(i);
                    i--;
                    continue;
                }
            }
            #endregion
            #region DownloadUpdate
            downloadEntity.timeElapsed.Value += (float)timeStep;
            if (downloadEntity.upTime == 0 || downloadEntity.timeElapsed.Value > downloadEntity.upTime || downloadEntity.progressValue.Value >= downloadEntity.Width) {
                downloadEntity.Reset();
                downloadEntity.Size = 5;
                downloadEntity.CenterX = Game.WIDTH_UNITS / 2f;
                downloadEntity.Y = Game.HEIGHT_UNITS / 2f;
                downloadEntity.stepSize = 3;
                downloadEntity.upTime = 6;

                WarningEntity warning = warningPool.Retrieve();
                warning.CenterX = downloadEntity.CenterX;
                warning.LifeTime = downloadEntity.upTime * (1/3f);
                warning.Y = downloadEntity.Y - warning.Height;
                warning.mesh.Y = warning.Y;
                warning.mesh.X = warning.X;
                activeWarnings.Add(warning);
            }
            if (Mouse.RightButtonPressed) {
                PointF rightMousePos = new PointF();
                rightMousePos.X = cursorWidthScale * Mouse.X;
                rightMousePos.Y = cursorHeightScale * (viewHeight - Mouse.Y);
                if (downloadEntity.HitBox.Contains(rightMousePos)) {
                    downloadEntity.Input(rightMousePos.X - downloadEntity.X);
                }
            }
            #endregion
            #region ServerUpdate
            if (Mouse.LeftButtonPressed) {
                serverTargetPos = cursorWidthScale * Mouse.X;
            }
            
            if (serverTargetPos < server.CenterX - 0.02f)
            {
                server.CenterX -= ((float)timeStep * server.Speed);
                if (server.CenterX < serverTargetPos - 0.02f) server.CenterX = serverTargetPos - 0.02f;
            } else if (serverTargetPos > server.CenterX + 0.02f)
            {
                server.CenterX += ((float)timeStep * server.Speed);
                if (server.CenterX > serverTargetPos + 0.02f) server.CenterX = serverTargetPos + 0.02f;
            }                
            #endregion
            #region WarningCleanup
            for (int i = 0; i < activeWarnings.Count; i++)
            {
                WarningEntity warn = activeWarnings[i];
                warn.aliveTime.Value += (float) timeStep;
                if (warn.aliveTime.Value >= warn.LifeTime) {
                    warningPool.Release(warn);
                    activeWarnings.RemoveAt(i);
                    i--;
                }
            }
            #endregion
            if (Keyboard.IsKeyPressed(SDL.SDL_Keycode.SDLK_LSHIFT) && Keyboard.IsKeyPressed(SDL.SDL_Keycode.SDLK_SPACE)) {
                stateManager.ChangeState("GamePlayState");
            }
        }

        public void KeyInputListener(SDL.SDL_Keycode keycode, bool down) {
            if (!down) return;
            int currentUsage = bandwithBoost + speedBoost;
            if (keycode == SDL.SDL_Keycode.SDLK_SPACE) {
                if (serverEntity.Size > 1) {
                    serverEntity.Size--;
                    bandwithBoost--;

                    speedBoost++;
                    serverEntity.Speed += ServerEntity.SPEED_STEP;
                }
            }
            if (keycode == SDL.SDL_Keycode.SDLK_LSHIFT) {
                if (serverEntity.Speed > ServerEntity.MIN_SPEED) {
                    serverEntity.Speed -= ServerEntity.SPEED_STEP;
                    speedBoost--;

                    serverEntity.Size++;
                    bandwithBoost++;
                }
            }
        }

        public void WindowResize(int width, int height) {
            WindowContextsManager.CurrentWindowContext.GetDrawableDimensions();
            int vw, vh, vx, vy;
            WindowContextsManager.CurrentGL.GetViewport(out vx, out vy, out vw, out vh);
            CalculateScaleFactors(vw, vh);
        }

        private void CalculateScaleFactors(int width, int height) {
            viewHeight = height;
            this.cursorWidthScale = Game.WIDTH_UNITS * (1f / width);
            this.cursorHeightScale = Game.HEIGHT_UNITS * (1f / height);
        }
    }
}