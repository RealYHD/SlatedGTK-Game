using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using SDL2;
using SkinnerBox.Entities;
using SlatedGameToolkit.Framework.AssetSystem;
using SlatedGameToolkit.Framework.Graphics.Render;
using SlatedGameToolkit.Framework.Graphics.Textures;
using SlatedGameToolkit.Framework.Graphics.Window;
using SlatedGameToolkit.Framework.Input.Devices;
using SlatedGameToolkit.Framework.StateSystem;
using SlatedGameToolkit.Framework.StateSystem.States;
using SlatedGameToolkit.Framework.Utilities.Collections.Pooling;
using SlatedGameToolkit.Framework.Utilities;
using SlatedGameToolkit.Framework.Graphics.Text;
using SkinnerBox.Utilities;

namespace SkinnerBox.States
{
    public class GamePlayState : IState
    {
        private MeshBatchRenderer renderer;
        private AssetManager assets;
        private StateManager stateManager;
        private BitmapFont font;
        private GameOverState gameOverState;
        private Random random;

        #region CursorVars
        private float cursorWidthScale, cursorHeightScale;
        private float serverTargetPos; //Last left click position
        private int viewHeight; // The viewports height for inverting Y value.
        #endregion

        #region EntitiesVariables
        private ServerEntity server; //The player

        // Warning entities
        private ObjectPool<WarningEntity> warningPool;
        private List<WarningEntity> activeWarnings = new List<WarningEntity>();

        // Packet entities
        private ObjectPool<PacketEntity> packetPool;
        private List<PacketEntity> activePackets = new List<PacketEntity>();
        private PacketSpawnInfo packetSpawnInfo;
        private const float packetSafeMargin = 1/2f;

        //Download entities.
        private ObjectPool<DownloadEntity> downloadPool;
        private List<DownloadEntity> activeDownloads = new List<DownloadEntity>();
        private DownloadSpawnInfo downloadSpawnInfo;
        private const float downloadSafeMargin = 1.5f;
        #endregion
        
        #region PlayerStats
        private int speedBoost = 0;
        private int bandwithBoost = 0;
        private RectangleMesh bandwithMesh;
        private RectangleMesh speedMesh;
        private float score;
        private TransitionValue timeElapsed;
        private int downloadsServed;
        private int totalDownloads;
        private int packetsReceived;
        private int totalPackets;
        private readonly float totalStability = 5;
        private float stability;
        private RectangleMesh stabilityMesh;
        #endregion

        public GamePlayState(MeshBatchRenderer renderer, AssetManager asset, BitmapFont font, GameOverState gameOverState)
        {
            this.assets = asset;
            this.renderer = renderer;
            packetPool = new ObjectPool<PacketEntity>(CreatePacket);
            warningPool = new ObjectPool<WarningEntity>(createWarning);
            downloadPool = new ObjectPool<DownloadEntity>(createDownload);
            this.font = font;
            this.gameOverState = gameOverState;
        }

        public bool Activate()
        {
            Keyboard.keyboardUpdateEvent += KeyInputListener;
            Mouse.mouseUpdateEvent += MouseInput;
            serverTargetPos = 0.5f * Game.WIDTH_UNITS;
            server = new ServerEntity((Texture)assets["serverunit.png"], serverTargetPos, 0.1f);
            bandwithMesh = new RectangleMesh(new RectangleF(0, Game.HEIGHT_UNITS - 0.75f, 0.5f, 0.5f), (ITexture)assets["serverunit.png"], Color.White);
            stabilityMesh = new RectangleMesh(new RectangleF(0.05f, Game.HEIGHT_UNITS - 3.2f, 0.5f, 0.5f), (ITexture)assets["health.png"], Color.White);
            speedMesh = new RectangleMesh(bandwithMesh.Bounds, (ITexture)assets["ram.png"], Color.White);
            random = new Random();

            packetSpawnInfo = new PacketSpawnInfo(2, 1, (float)(random.NextDouble() * Game.WIDTH_UNITS), 1f, 0.2f, 2f);
            downloadSpawnInfo = new DownloadSpawnInfo(4, 6, 3, 1, 4, 2);
            score = 0;
            timeElapsed.HardSet(0);
            server.Size = 4;
            bandwithBoost = server.Size;
            speedBoost = 0;
            stability = totalStability;
            packetsReceived = 0;
            totalPackets = 0;
            downloadsServed = 0;
            totalDownloads = 0;

            this.font.PixelHeight = 32;
            font.PrepareCharacterGroup("Score:0123456789Uptim.%".ToCharArray());
            return true;
        }

        public PacketEntity CreatePacket() {
            return new PacketEntity((Texture)assets["packet.png"]);
        }

        public WarningEntity createWarning() {
            return new WarningEntity((Texture)assets["warning.png"]);
        }

        public DownloadEntity createDownload() {
            return new DownloadEntity((Texture)assets["drag.png"], (Texture)assets["downloadbar.png"]);
        }

        public bool Deactivate()
        {
            Keyboard.keyboardUpdateEvent -= KeyInputListener;
            Mouse.mouseUpdateEvent -= MouseInput;
            return true;
        }

        public void Deinitialize()
        {
            
        }

        public string getName()
        {
            return "GamePlayState";
        }

        public void Initialize(StateManager manager)
        {
            this.stateManager = manager;
            int vw, vh, vx, vy;
            WindowContextsManager.CurrentGL.GetViewport(out vx, out vy, out vw, out vh);
            CalculateScaleFactors(vw, vh);
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
            #region DownloadRender
            foreach(DownloadEntity download in activeDownloads) {
                renderer.Draw(download);
                renderer.Draw(download.progressMesh);
            }                
            #endregion
            renderer.Draw(server);

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
                float normalizedHealth = stability / totalStability;
                int red = (int) (byte.MaxValue * normalizedHealth);
                stabilityMesh.Color = Color.FromArgb(red, 0, 0);
                renderer.Draw(stabilityMesh);
                font.WriteLine(renderer, stabilityMesh.Width + 0.1f, stabilityMesh.Y, Math.Round(100 * normalizedHealth, 1) + "%", Color.Red);

                timeElapsed.InterpolatePosition((float)delta);
                font.WriteLine(renderer, 0.05f, Game.HEIGHT_UNITS - 1.75f, "Score: " + Math.Round(score, 0), Color.Black);
                font.WriteLine(renderer, 0.05f, Game.HEIGHT_UNITS - 2.5f, "Uptime: " + Math.Round(timeElapsed.Value), Color.Black);
            #endregion
            renderer.End();
        }

        public void Update(double timeStep)
        {
            timeElapsed.Value = (float)timeStep + timeElapsed.DesignatedValue;
            score += (float) timeStep * 0.5f;
            #region ServerUpdate
            if (Mouse.LeftButtonPressed) {
                serverTargetPos = cursorWidthScale * Mouse.X;
            }
            
            if (serverTargetPos < server.CenterX)
            {
                server.CenterX -= ((float)timeStep * server.Speed);
                if (server.CenterX < serverTargetPos) server.CenterX = serverTargetPos;
            } else if (serverTargetPos > server.CenterX) 
            {
                server.CenterX += ((float)timeStep * server.Speed);
                if (server.X > serverTargetPos) server.CenterX = serverTargetPos;
            }                
            #endregion
            #region PacketUpdate
            packetSpawnInfo.timeElapsed += (float) timeStep;
            if (packetSpawnInfo.timeElapsed >= packetSpawnInfo.interval) {
                packetSpawnInfo.timeElapsed = 0;
                //do spawning
                for(int i = 0; i < packetSpawnInfo.perSpawn; i++) {
                    PacketEntity packet = packetPool.Retrieve();
                    packet.CenterX = packetSpawnInfo.batchLocation;
                    packet.Y = i * packet.Height + packetSpawnInfo.range + Game.HEIGHT_UNITS + packetSpawnInfo.speed * (2/3f);
                    packet.velocity = packetSpawnInfo.speed;
                    packet.Color = Color.Blue;
                    totalPackets++;
                    activePackets.Add(packet);
                }

                //Spawn Warning
                WarningEntity warning = warningPool.Retrieve();
                warning.CenterX = packetSpawnInfo.batchLocation;
                warning.LifeTime = packetSpawnInfo.interval * (2/3f);
                warning.Y = Game.HEIGHT_UNITS - warning.Height;
                activeWarnings.Add(warning);

                //Prepare next batch
                float change = (float)((float)(random.NextDouble() - 1/2f) * packetSpawnInfo.jumpDistance * 2);
                if (packetSpawnInfo.batchLocation + change > Game.WIDTH_UNITS - packetSafeMargin || packetSpawnInfo.batchLocation + change < packetSafeMargin) {
                    packetSpawnInfo.batchLocation -= change;
                } else {
                    packetSpawnInfo.batchLocation += change;
                }
            }

            for (int i = 0; i < activePackets.Count; i++)
            {
                PacketEntity packet = activePackets[i];
                packet.Update(timeStep);
                if (packet.HitBox.IntersectsWith(server.HitBox) && packet.velocity > 0) {
                    packet.velocity *= -2.5f;
                    packet.Color = Color.Cyan;
                }
                if (packet.Y <= 0 - packet.Height) {
                    stability -= 0.5f;
                    packetPool.Release(packet);
                    activePackets.RemoveAt(i);
                    i--;
                    continue;
                }
                if (packet.Y >= Game.HEIGHT_UNITS && packet.velocity < 0) {
                    score += -2 * packet.velocity;
                    packetsReceived++;
                    stability += 0.05f;
                    packetPool.Release(packet);
                    activePackets.RemoveAt(i);
                    i--;
                    continue;
                }
            }
            #endregion
            #region DownloadUpdate
            downloadSpawnInfo.elapsedSinceSpawn += (float)timeStep;
            if (downloadSpawnInfo.elapsedSinceSpawn >= downloadSpawnInfo.period) {
                downloadSpawnInfo.elapsedSinceSpawn = 0;
                if (activeDownloads.Count < downloadSpawnInfo.maximumAmount) {
                    DownloadEntity download = downloadPool.Retrieve();
                    download.Size = (int)(downloadSpawnInfo.generalSize + ((random.NextDouble() - 1/2f) * 2f * downloadSpawnInfo.sizeRange));
                    download.X = (float)(random.NextDouble() * (Game.WIDTH_UNITS - download.Width));
                    download.Y = (float)(downloadSafeMargin + random.NextDouble() * (Game.HEIGHT_UNITS - 2 * downloadSafeMargin));
                    download.stepSize = downloadSpawnInfo.stepSize;
                    download.upTime = downloadSpawnInfo.upTime;
                    totalDownloads++;
                    activeDownloads.Add(download);

                    WarningEntity warning = warningPool.Retrieve();
                    warning.CenterX = download.CenterX;
                    warning.LifeTime = download.upTime * (1/3f);
                    warning.Y = download.Y - warning.Height;
                    warning.mesh.Y = warning.Y;
                    activeWarnings.Add(warning);
                }
            }


            for (int i = 0; i < activeDownloads.Count; i++)
            {
                DownloadEntity download = activeDownloads[i];

                download.timeElapsed.Value += (float)timeStep;
                if (Mouse.RightButtonPressed) {
                    Vector2 rightMousePos;
                    rightMousePos.X = cursorWidthScale * Mouse.X;
                    rightMousePos.Y = cursorHeightScale * (viewHeight - Mouse.Y);
                    if (download.HitBox.Contains(rightMousePos)) {
                        download.Input(rightMousePos.X - download.X);
                    }
                }
                
                if (download.progressValue.Value >= download.Width)
                {
                    score += downloadSpawnInfo.maximumAmount * 2 + downloadSpawnInfo.sizeRange + 1f / downloadSpawnInfo.period + 2 * downloadSpawnInfo.stepSize;
                    downloadsServed++;
                    downloadPool.Release(download);
                    activeDownloads.RemoveAt(i);
                    i--;
                    continue;
                }
                if (download.timeElapsed.Value >= download.upTime)
                {
                    stability -= 1.5f;
                    downloadPool.Release(download);
                    activeDownloads.RemoveAt(i);
                    i--;
                    continue;
                }
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
            #region DifficultyUpdate
            //packet curve
            packetSpawnInfo.perSpawn = (int)(0.5f * (Math.Pow(timeElapsed.Value, 0.5f) + 1));
            packetSpawnInfo.speed = (float)((0.025f * Math.Pow(timeElapsed.Value, 1.1f)) + 1f);
            if (packetSpawnInfo.range < 4) {
                packetSpawnInfo.range = (float)(0.1f * (Math.Pow(timeElapsed.Value, 1.15f)) + 2f);
                if (packetSpawnInfo.range > 4) packetSpawnInfo.range = 4;
            }
            if (packetSpawnInfo.interval > 0.3f) {
                packetSpawnInfo.interval = (float) (-0.0055 * timeElapsed.Value) + 2f;
                if (packetSpawnInfo.interval < 0.3f) packetSpawnInfo.interval = 0.3f;
            }

            //download curve
            if (downloadSpawnInfo.maximumAmount < 4) {
                downloadSpawnInfo.maximumAmount = (int)(0.02f * timeElapsed.Value + 1);
            }
            if (downloadSpawnInfo.upTime > 3) {
                downloadSpawnInfo.upTime = (float)(8 + (-0.1f * Math.Pow(timeElapsed.Value, 0.8f)));
                if (downloadSpawnInfo.upTime < 3) downloadSpawnInfo.upTime = 3;
            }
            if (downloadSpawnInfo.period > 1.5f) {
                downloadSpawnInfo.period = (float) (-0.006 * timeElapsed.Value) + 4;
                if (packetSpawnInfo.interval < 1.5f) packetSpawnInfo.interval = 1.5f;
            }
            #endregion
            #region BoundaryChecking
            if (stability > totalStability) {
                stability = totalStability;
            } else if (stability <= 0)
            {
                stability = 0;
                gameOverState.SetStats((int)Math.Round(this.score), this.timeElapsed.Value, downloadsServed, totalDownloads, packetsReceived, totalPackets);
                stateManager.ChangeState("GameOver");
            }
            #endregion
        }

        public void KeyInputListener(SDL.SDL_Keycode keycode, bool down) {
            if (!down) return;
            int currentUsage = bandwithBoost + speedBoost;
            if (keycode == SDL.SDL_Keycode.SDLK_SPACE) {
                if (server.Size > 1) {
                    server.Size--;
                    bandwithBoost--;

                    speedBoost++;
                    server.Speed += ServerEntity.SPEED_STEP;
                }
            }
            if (keycode == SDL.SDL_Keycode.SDLK_LSHIFT) {
                if (server.Speed > ServerEntity.MIN_SPEED) {
                    server.Speed -= ServerEntity.SPEED_STEP;
                    speedBoost--;

                    server.Size++;
                    bandwithBoost++;
                }
            }
        }

        public void MouseInput(bool leftDown, bool rightDown, bool middle, int x, int y, int scrollX, int scrollY) {

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