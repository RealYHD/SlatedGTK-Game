using System.Drawing;
using System.Numerics;
using SlatedGameToolkit.Framework.Graphics.Render;
using SlatedGameToolkit.Framework.Graphics.Textures;

namespace WebsiteSim.Entities
{
    public abstract class Entity : IMesh, IPositionInterpolable
    {
        public RectangleMesh mesh;
        public virtual float CenterX {
            get {
                return X + Width / 2f;
            }
            set {
                X = value - Width / 2f;
            }
        }
        public float X  { get; set; }

        public float Y { get; set; }

        public float Width {
            get {
                return mesh.Width;
            }

            set {
                mesh.Width = value;
            }
        }

        public float Height {
            get {
                return mesh.Height;
            }

            set {
                mesh.Height = value;
            }
        }

        public virtual RectangleF HitBox
        {
            get {
                return mesh.Bounds;
            }
        }

        public Entity(ITexture texture)
        {
            this.mesh = new RectangleMesh(texture, Color.White);
        }

        public (Vector3, Vector2)[] Vertices => mesh.Vertices;

        public uint[] Elements => mesh.Elements;

        public ITexture Texture => mesh.Texture;

        public Color Color {
            get {
                return this.mesh.Color;
            }
            set {
                this.mesh.Color = value;
            }
        }

        public virtual void InterpolatePosition(float delta)
        {
            mesh.X += delta * (X - mesh.X);
            mesh.Y += delta * (Y - mesh.Y);
        }
    }
}