using System;
using System.Numerics;
using SlatedGameToolkit.Framework.Graphics.Render;
using SlatedGameToolkit.Framework.Graphics.Textures;
using SlatedGameToolkit.Framework.Utilities.Collections.Pooling;

namespace SkinnerBox.Entities
{
    public class PacketEntity : Entity, IPositionInterpolable, IPoolable
    {
        public float velocity;
        public PacketEntity(ITexture texture) : base(texture)
        {
            this.Width = 0.5f;
            this.Height = 1f;
            Reset();
        }

        public void Reset()
        {
            this.Y = Game.HEIGHT_UNITS;
            this.mesh.Y = this.Y;
            this.velocity = 0;
            this.X = 0;
            this.mesh.X = this.X;
        }

        public void Update(double delta) {
            this.Y -= (float)(velocity * delta);
        }
    }

    public struct PacketSpawnInfo
    {
        public float timeElapsed;
        public int period;
        public int perSpawn;
        public float batchLocation;
        public float distanceBetween;
        public float jumpDistance;
        public float speed;
        public float lastSpawnLocation;

        public PacketSpawnInfo(int period, int perSpawn, float location, float speed, float distance, float jump) {
            timeElapsed = 0;
            lastSpawnLocation = 0;
            this.period = period;
            this.perSpawn = perSpawn;
            this.batchLocation = location;
            this.distanceBetween = distance;
            this.speed = speed;
            this.jumpDistance = jump;
        }
    }
}