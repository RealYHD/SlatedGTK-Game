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
        private float leftXPos; //Last left click position


        //Entities
        private ServerEntity server;

        private ObjectPool<WarningEntity> warningPool;
        private List<WarningEntity> activeWarnings = new List<WarningEntity>();

        private ObjectPool<PacketEntity> packetPool;
        private List<PacketEntity> activePackets = new List<PacketEntity>();
        private SpawnInfo packetSpawnInfo;

        public GamePlayState(MeshBatchRenderer renderer, AssetManager asset)
        {
            this.assets = asset;
            this.renderer = renderer;
            packetPool = new ObjectPool<PacketEntity>(CreatePacket);
            warningPool = new ObjectPool<WarningEntity>(createWarning);
        }

        public bool Activate()
        {
            Keyboard.keyboardUpdateEvent += KeyInputListener;
            Mouse.mouseUpdateEvent += MouseInput;
            leftXPos = 0.5f * Game.WIDTH_UNITS;
            server = new ServerEntity((Texture)assets["serverunit.png"], leftXPos, 0.1f);
            random = new Random();

            packetSpawnInfo = new SpawnInfo(2, 1, (float)(random.NextDouble() * Game.WIDTH_UNITS), 1f, 0.2f);
            return true;
        }

        public PacketEntity CreatePacket() {
            return new PacketEntity((Texture)assets["packet.png"]);
        }

        public WarningEntity createWarning() {
            return new WarningEntity((Texture)assets["warning.png"]);
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
            renderer.Draw(server);
            renderer.End();
        }

        public void Update(double timeStep)
        {
            #region ServerUpdate
            if (Mouse.LeftButtonPressed) {
                leftXPos = widthFactor * Mouse.X;
            }
            
            if (leftXPos < server.CenterX)
            {
                server.CenterX -= ((float)timeStep * server.Speed);
                if (server.CenterX < leftXPos) server.CenterX = leftXPos;
            } else if (leftXPos > server.CenterX) 
            {
                server.CenterX += ((float)timeStep * server.Speed);
                if (server.X > leftXPos) server.CenterX = leftXPos;
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
                packetSpawnInfo.batchLocation = (float)(random.NextDouble() * Game.WIDTH_UNITS);
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

        private void CalculateScaleFactors(float width, float height) {
            this.widthFactor = Game.WIDTH_UNITS * (1f / width);
            this.heightFactor = Game.HEIGHT_UNITS * (1f / height);
        }
    }
}