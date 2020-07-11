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

namespace SkinnerBox.States.Gameplay
{
    public class GamePlayState : IState
    {
        private MeshBatchRenderer renderer;
        private AssetManager assets;
        private StateManager stateManager;
        private BitmapFont font;
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
        private readonly int totalUsage = 3;
        private int usedUsage = 0;
        private RectangleMesh usageMesh;
        private int score;
        private float timeElapsed;
        #endregion

        public GamePlayState(MeshBatchRenderer renderer, AssetManager asset, BitmapFont font)
        {
            this.assets = asset;
            this.renderer = renderer;
            packetPool = new ObjectPool<PacketEntity>(CreatePacket);
            warningPool = new ObjectPool<WarningEntity>(createWarning);
            downloadPool = new ObjectPool<DownloadEntity>(createDownload);
            this.font = font;
            font.PrepareCharacterGroup("score:0123456789timlapsd".ToCharArray());
        }

        public bool Activate()
        {
            Keyboard.keyboardUpdateEvent += KeyInputListener;
            Mouse.mouseUpdateEvent += MouseInput;
            serverTargetPos = 0.5f * Game.WIDTH_UNITS;
            server = new ServerEntity((Texture)assets["serverunit.png"], serverTargetPos, 0.1f);
            usageMesh = new RectangleMesh(new RectangleF(0, Game.HEIGHT_UNITS - 0.75f, 0.5f, 0.5f), (ITexture)assets["usage.png"], Color.White);
            random = new Random();

            packetSpawnInfo = new PacketSpawnInfo(2, 1, (float)(random.NextDouble() * Game.WIDTH_UNITS), 1f, 0.2f, 3f);
            downloadSpawnInfo = new DownloadSpawnInfo(4, 6, 3, 1, 4, 2);
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
                for (int i = 0; i < totalUsage; i++)
                {
                    usageMesh.X = 0.25f + (i * (usageMesh.Width + 0.2f));
                    if (i >= usedUsage) {
                        usageMesh.Color = Color.Yellow;
                    } else {
                        usageMesh.Color = Color.White;
                    }
                    renderer.Draw(usageMesh);
                }

                font.WriteLine(renderer, 0.05f, Game.HEIGHT_UNITS - 2f, "score: " + score, Color.Black);
            #endregion
            renderer.End();
        }

        public void Update(double timeStep)
        {
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
            if (packetSpawnInfo.timeElapsed >= packetSpawnInfo.period) {
                packetSpawnInfo.timeElapsed = 0;
                //do spawning
                for(int i = 0; i < packetSpawnInfo.perSpawn; i++) {
                    PacketEntity packet = packetPool.Retrieve();
                    packet.CenterX = packetSpawnInfo.batchLocation;
                    packet.Y = i * packet.Height + packetSpawnInfo.distanceBetween + Game.HEIGHT_UNITS + packetSpawnInfo.speed * (2/3f);
                    packet.velocity = packetSpawnInfo.speed;
                    packet.Color = Color.Blue;
                    activePackets.Add(packet);
                }

                //Spawn Warning
                WarningEntity warning = warningPool.Retrieve();
                warning.CenterX = packetSpawnInfo.batchLocation;
                warning.LifeTime = packetSpawnInfo.period * (2/3f);
                warning.Y = Game.HEIGHT_UNITS - warning.Height;
                activeWarnings.Add(warning);

                //Prepare next batch
                packetSpawnInfo.batchLocation = (float)(packetSpawnInfo.batchLocation + (random.NextDouble() - 1/2f) * packetSpawnInfo.jumpDistance * 2);
                if (packetSpawnInfo.batchLocation > Game.WIDTH_UNITS - packetSafeMargin) {
                    packetSpawnInfo.batchLocation = Game.WIDTH_UNITS - packetSafeMargin;
                } else if (packetSpawnInfo.batchLocation < packetSafeMargin) packetSpawnInfo.batchLocation = packetSafeMargin;
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
                    packetPool.Release(packet);
                    activePackets.RemoveAt(i);
                    i--;
                    continue;
                }
                if (packet.Y >= Game.HEIGHT_UNITS && packet.velocity < 0) {
                    packetPool.Release(packet);
                    activePackets.RemoveAt(i);
                    i--;
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
                    downloadPool.Release(download);
                    activeDownloads.RemoveAt(i);
                    i--;
                    continue;
                }
                if (download.timeElapsed.Value >= download.upTime)
                {
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
        }

        public void KeyInputListener(SDL.SDL_Keycode keycode, bool down) {
            if (!down) return;
            if (keycode == SDL.SDL_Keycode.SDLK_a) {
                if (usedUsage > 0 && server.Size > 1) {
                    usedUsage--;
                    server.Size--;
                }
            }
            if (keycode == SDL.SDL_Keycode.SDLK_d) {
                if (usedUsage < totalUsage) {
                    usedUsage++;
                    server.Size++;
                }
            }
            if (keycode == SDL.SDL_Keycode.SDLK_s) {
                if (usedUsage > 0 && server.Speed > ServerEntity.MIN_SPEED) {
                    usedUsage--;
                    server.Speed -= ServerEntity.SPEED_STEP;
                }
            }
            if (keycode == SDL.SDL_Keycode.SDLK_w) {
                if (usedUsage < totalUsage) {
                    usedUsage++;
                    server.Speed += ServerEntity.SPEED_STEP;
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