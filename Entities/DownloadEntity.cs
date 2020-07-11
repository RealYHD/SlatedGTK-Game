using System.Drawing;
using SkinnerBox.Utilities.Gameplay;
using SlatedGameToolkit.Framework.Graphics.Render;
using SlatedGameToolkit.Framework.Graphics.Textures;
using SlatedGameToolkit.Framework.Utilities.Collections.Pooling;

namespace SkinnerBox.Entities
{
    public class DownloadEntity : Entity, IPoolable
    {
        private readonly float unitSize = 1/2f;
        private readonly float unitPerProgressTexture = 0.5f;
        public float stepSize;
        public TransitionValue progressValue;
        public RectangleMesh progressMesh;
        public TransitionValue timeElapsed;
        public float health;
        int size;
        public int Size {
            get {
                return size;
            }
            set {
                this.size = value;
                mesh.TextureBounds = new RectangleF(0, 0, size, 1f);
                Width = unitSize * size;
            }
        }

        public override RectangleF HitBox {
            get {
                RectangleF rect = base.HitBox;
                rect.Width = Width * size;
                return rect;
            }
        }
        public DownloadEntity(ITexture promptTex, ITexture progressTex) : base(promptTex)
        {
            Size = 1;
            Height = unitSize;
            progressMesh = new RectangleMesh(progressTex, Color.White);
            Reset();
        }

        private void UpdateProgressMesh() {
            progressMesh.Width = progressValue.Value;
            progressMesh.TextureBounds = new RectangleF(0, 0, progressValue.Value * unitPerProgressTexture, 1f);
            progressMesh.Height = Height;
            progressMesh.X = X;
            progressMesh.Y = Y;
        }

        public void Reset()
        {
            Size = 1;
            X = 0;
            Y = 0;
            mesh.X = X;
            mesh.Y = Y;
            progressValue.HardSet(0);
            timeElapsed.HardSet(0);
            stepSize = 0;
            health = 0;
            Color = Color.DarkCyan;
            UpdateProgressMesh();
        }

        public void Input(float x) {
            if (x <= progressValue.DesignatedValue + (1f/stepSize) && x > progressValue.DesignatedValue) {
                progressValue.Value = x;
                if (progressValue.Value > Width) progressValue.HardSet(Width);
            }
        }

        public override void InterpolatePosition(float delta) {
            progressValue.InterpolatePosition(delta);
            timeElapsed.InterpolatePosition(delta);
            float prog = timeElapsed.Value / health;
            if (prog > 1) prog = 1;
            if (prog < 0) prog = 0;
            this.Color = Color.FromArgb((int)(byte.MaxValue * (1f - prog)), Color);
            UpdateProgressMesh();
        }
    }

    public struct DownloadSpawnInfo
    {
        public float period;
        public float elapsedSinceSpawn;
        public float health;
        public float stepSize;
        public int maximumAmount;
        public int generalSize;
        public int sizeRange;

        public DownloadSpawnInfo(float cooldown, float health, float stepSize, int maxAmount, int generalSize, int sizeRange)
        {
            this.period = cooldown;
            this.elapsedSinceSpawn = 0;
            this.health = health;
            this.stepSize = stepSize;
            this.maximumAmount = maxAmount;
            this.sizeRange = sizeRange;
            this.generalSize = generalSize;
        }
    }
}