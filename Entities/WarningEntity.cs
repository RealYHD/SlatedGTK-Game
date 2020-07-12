using WebsiteSim.Utilities;
using SlatedGameToolkit.Framework.Graphics.Textures;
using SlatedGameToolkit.Framework.Utilities.Collections.Pooling;
using System;
using System.Drawing;

namespace WebsiteSim.Entities
{
    public class WarningEntity : Entity, IPoolable
    {
        public float LifeTime { get; set; }
        public TransitionValue aliveTime;

        public WarningEntity(ITexture texture) : base(texture) {
            this.Width = 1f;
            this.Height = 1f;
            Reset();
        }
        public void Reset()
        {
            LifeTime = 0;
            X = 0 - Width;
            mesh.X = X;
            Y = - Height;
            mesh.Y = Y;
            aliveTime.HardSet(0);
            this.Color = Color.Red;
        }

        public override void InterpolatePosition(float delta) {
            aliveTime.InterpolatePosition(delta);
            float prog = (aliveTime.Value / LifeTime);
            if (prog > 1) prog = 1;
            this.Color = Color.FromArgb((int)(byte.MaxValue * (1f - prog)), this.Color);
            base.InterpolatePosition(delta);
        }
    }
}