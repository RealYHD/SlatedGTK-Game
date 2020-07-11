namespace SkinnerBox.States.Gameplay
{
    public struct SpawnInfo
    {
        public float timeElapsed;
        public int period;
        public int perSpawn;
        public float batchLocation;
        public float distanceBetween;
        public float speed;

        public SpawnInfo(int period, int perSpawn, float location, float speed, float distance) {
            timeElapsed = 0;
            this.period = period;
            this.perSpawn = perSpawn;
            this.batchLocation = location;
            this.distanceBetween = distance;
            this.speed = speed;
        }
    }
}