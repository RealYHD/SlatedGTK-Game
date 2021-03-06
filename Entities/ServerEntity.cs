using System.Drawing;
using SlatedGameToolkit.Framework.Graphics.Render;
using SlatedGameToolkit.Framework.Graphics.Textures;
using SlatedGameToolkit.Framework.Graphics.Window;

namespace WebsiteSim.Entities
{
    public class ServerEntity : Entity
    {
        public const float MIN_SPEED = 1f;
        public const float SPEED_STEP = 1f;
        private readonly float length = 4/8f;
        public float Speed { get; set; }
        public override float CenterX {
            get {
                return base.X + (length * size) / 2f;
            }

            set {
                base.X = value - (length * size) / 2f;
            }
        }
        private int size;
        public int Size {
            get {
                return size;
            }

            set {
                if (this.size != value) {
                    this.size = value;
                    this.mesh.TextureBounds = new RectangleF(0, 0, size, 1);
                    this.Width = value * length;
                }
            }
        }
        public override RectangleF HitBox {
            get {
                RectangleF hitbox = base.HitBox;
                hitbox.Width = Width;
                return hitbox;
            }
        }
        public ServerEntity(Texture texture, float initialX, float Y) : base(texture)
        {
            this.X = (Game.WIDTH_UNITS - this.Width) / 2f;
            this.Height = length;

            Size = 1;
            
            this.Speed = MIN_SPEED;

            CenterX = initialX;
            mesh.X = X;
            this.Y = Y;
            mesh.Y = this.Y;
        }

    }

}