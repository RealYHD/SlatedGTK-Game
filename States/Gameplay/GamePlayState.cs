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

namespace SkinnerBox.States.Gameplay
{
    public class GamePlayState : IState
    {
        private MeshBatchRenderer renderer;
        private AssetManager assets;
        private StateManager stateManager;
        private Random random;

        //Cursor information
        private float widthFactor, heightFactor;
        private float serverTargetPos; //Last left click position

        //Entities
        private ServerEntity server;

        private ObjectPool<WarningEntity> warningPool;
        private List<WarningEntity> activeWarnings = new List<WarningEntity>();

        private ObjectPool<PacketEntity> packetPool;
        private List<PacketEntity> activePackets = new List<PacketEntity>();
        private PacketSpawnInfo packetSpawnInfo;
        private const float packetSafeMargin = 1/2f;

        private ObjectPool<DownloadEntity> downloadPool;
        private List<DownloadEntity> activeDownloads = new List<DownloadEntity>();
        private DownloadSpawnInfo downloadSpawnInfo;
        private const float downloadSafeMargin = 1.5f;
        private int viewHeight;
        

        public GamePlayState(MeshBatchRenderer renderer, AssetManager asset)
        {
            this.assets = asset;
            this.renderer = renderer;
            packetPool = new ObjectPool<PacketEntity>(CreatePacket);
            warningPool = new ObjectPool<WarningEntity>(createWarning);
            downloadPool = new ObjectPool<DownloadEntity>(createDownload);
        }

        public bool Activate()
        {
            Keyboard.keyboardUpdateEvent += KeyInputListener;
            Mouse.mouseUpdateEvent += MouseInput;
            serverTargetPos = 0.5f * Game.WIDTH_UNITS;
            server = new ServerEntity((Texture)assets["serverunit.png"], serverTargetPos, 0.1f);
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
            foreach (WarningEntity warn in activeWarnings)
            {
                renderer.Draw(warn);
            }
            foreach (PacketEntity packet in activePackets)
            {
                renderer.Draw(packet);
            }
            foreach(DownloadEntity download in activeDownloads) {
                renderer.Draw(download);
                renderer.Draw(download.progressMesh);
            }
            renderer.Draw(server);
            renderer.End();
        }

        public void Update(double timeStep)
        {
            #region ServerUpdate
            if (Mouse.LeftButtonPressed) {
                serverTargetPos = widthFactor * Mouse.X;
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
                    packet.velocity *= -2f;
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
                    download.mesh.X = download.X;
                    download.mesh.Y = download.Y;
                    download.stepSize = downloadSpawnInfo.stepSize;
                    download.health = downloadSpawnInfo.health;
                    activeDownloads.Add(download);
                }
            }


            for (int i = 0; i < activeDownloads.Count; i++)
            {
                DownloadEntity download = activeDownloads[i];

                download.timeElapsed.Value += (float)timeStep;
                if (Mouse.RightButtonPressed) {
                    Vector2 rightMousePos;
                    rightMousePos.X = widthFactor * Mouse.X;
                    rightMousePos.Y = heightFactor * (viewHeight - Mouse.Y);
                    if (download.HitBox.Contains(rightMousePos)) {
                        download.Input(rightMousePos.X - download.X);
                    }
                }
                
                if (download.progressValue.Value >= download.Width)
                {
                    downloadPool.Release(download);
                    activeDownloads.RemoveAt(i);
                    i--;
                    Console.WriteLine("YAY");
                    continue;
                }
                if (download.timeElapsed.Value >= download.health)
                {
                    downloadPool.Release(download);
                    activeDownloads.RemoveAt(i);
                    i--;
                    Console.WriteLine("AW");
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
            this.widthFactor = Game.WIDTH_UNITS * (1f / width);
            this.heightFactor = Game.HEIGHT_UNITS * (1f / height);
        }
    }
}