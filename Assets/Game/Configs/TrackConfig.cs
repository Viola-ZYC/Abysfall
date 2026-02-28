using UnityEngine;

namespace EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Track Config")]
    public class TrackConfig : ScriptableObject
    {
        public TrackSegment[] segmentPrefabs;
        public int initialSegments = 6;
        public float recycleDistance = 40f;
        public float spawnAheadDistance = 80f;
    }
}
