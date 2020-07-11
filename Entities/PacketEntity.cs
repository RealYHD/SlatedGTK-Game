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
}