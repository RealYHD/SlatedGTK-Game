using SlatedGameToolkit.Framework.Graphics.Render;

namespace SkinnerBox.Utilities.Gameplay
{
    public struct TransitionValue : IPositionInterpolable
    {
        private float current;
        private float value;

        public float Value
        {
            get
            {
                return current;
            }
            set
            {
                this.value = value;
            }
        }

        public float DesignatedValue {
            get {
                return value;
            }
        }
        public void InterpolatePosition(float delta)
        {
            this.current += (value - current) * delta;
        }

        public void HardSet(float value) {
            current = value;
            this.value = value;
        }
    }
}