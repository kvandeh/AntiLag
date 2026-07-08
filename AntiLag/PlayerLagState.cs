using System.Collections.Generic;
using UnityEngine;

namespace AntiLag
{
    public class PlayerLagState
    {
        public float LastInputTime = -1f;
        public float LastGapEnd = float.NegativeInfinity;
        public float LastGapDuration;
        public int Strikes;
        public float LastNotifyTime = float.NegativeInfinity;

        public bool HasRecordedGap => !float.IsNegativeInfinity(LastGapEnd);

        private const float HistorySeconds = 12f;

        private readonly struct Snapshot
        {
            public readonly float Time;
            public readonly Vector3 Position;

            public Snapshot(float time, Vector3 position)
            {
                Time = time;
                Position = position;
            }
        }

        private readonly Queue<Snapshot> positions = new Queue<Snapshot>();

        public void RecordPosition(float time, Vector3 position)
        {
            positions.Enqueue(new Snapshot(time, position));
            while (time - positions.Peek().Time > HistorySeconds)
            {
                positions.Dequeue();
            }
        }

        public Vector3? PositionAt(float time)
        {
            Snapshot? nearest = null;
            foreach (Snapshot snapshot in positions)
            {
                bool isCloser = nearest == null ||
                    Mathf.Abs(snapshot.Time - time) < Mathf.Abs(nearest.Value.Time - time);
                if (isCloser)
                {
                    nearest = snapshot;
                }
            }
            return nearest?.Position;
        }
    }
}
