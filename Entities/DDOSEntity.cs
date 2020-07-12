using SlatedGameToolkit.Framework.Graphics.Textures;
using SlatedGameToolkit.Framework.Utilities.Collections.Pooling;
using System.Drawing;
namespace WebsiteSim.Entities
{
    public class DDOSEntity : Entity, IPoolable
    {
        private float speed;
        public DDOSEntity(ITexture texture) : base(texture)
        {
            Width = 0.5f;
            this.Color = Color.Red;
        }

        public void Initialize(float xPos, float length, float speed, float delay)
        {
            Height = length;
            X = xPos;
            this.mesh.X = X;
            this.speed = speed;
            this.Y += delay * speed;
            mesh.Y = Y;
        }

        public void Update(float delta) {
            Y -= speed * delta;
        }
        public void Reset()
        {
            Height = 0;
            Y = Game.HEIGHT_UNITS;
            mesh.Y = Y;
            speed = 0;
        }
    }

    public struct DDOSSPawnInfo
    {
        public float speed;
        public float interval;
        public float intervalDeviation;
        public float timeRemaining;

        public DDOSSPawnInfo(float speed, float interval, float intervalDeviation, float timeRemaining)
        {
            this.speed = speed;
            this.interval = interval;
            this.intervalDeviation = intervalDeviation;
            this.timeRemaining = timeRemaining;
        }
    }
}