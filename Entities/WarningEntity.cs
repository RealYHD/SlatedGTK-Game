using SkinnerBox.Utilities.Gameplay;
using SlatedGameToolkit.Framework.Graphics.Textures;
using SlatedGameToolkit.Framework.Utilities.Collections.Pooling;
using System;
using System.Drawing;

namespace SkinnerBox.Entities
{
    public class WarningEntity : Entity, IPoolable
    {
        public float LifeTime { get; set; }
        public TransitionValue aliveTime;

        public WarningEntity(ITexture texture) : base(texture) {
            this.Width = 2;
            this.Height = 2;
            Reset();
        }
        public void Reset()
        {
            LifeTime = 0;
            X = 0 - Width;
            mesh.X = X;
            Y = Game.HEIGHT_UNITS - Height;
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