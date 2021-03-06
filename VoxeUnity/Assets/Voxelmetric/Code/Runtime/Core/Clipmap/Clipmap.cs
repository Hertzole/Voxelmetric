﻿namespace Voxelmetric
{
    public class Clipmap
    {
        private readonly AxisInfo[] axes;

        public int VisibleRange { get; private set; }
        public int RangeYMin { get; private set; }
        public int RangeYMax { get; private set; }

        public Clipmap(int visibleRange, int rangeYMin, int rangeYMax)
        {
            VisibleRange = visibleRange;

            if (rangeYMin < -visibleRange)
            {
                rangeYMin = -visibleRange;
            }

            if (rangeYMax > visibleRange)
            {
                rangeYMax = visibleRange;
            }

            RangeYMin = rangeYMin;
            RangeYMax = rangeYMax;

            axes = new[]
            {
                new AxisInfo
                {
                    map = new ClipmapItem[2*visibleRange+1],
                    offset = 0,
                    rangeMin = -visibleRange,
                    rangeMax = visibleRange
                },
                new AxisInfo
                {
                    map = new ClipmapItem[2*visibleRange+1],
                    offset = 0,
                    rangeMin = -rangeYMin,
                    rangeMax = rangeYMax
                },
                new AxisInfo
                {
                    map = new ClipmapItem[2*visibleRange+1],
                    offset = 0,
                    rangeMin = -visibleRange,
                    rangeMax = visibleRange
                }
            };
        }

        public ClipmapItem this[int x, int y, int z]
        {
            get
            {
                int tx = TransformX(x);
                int ty = TransformY(y);
                int tz = TransformZ(z);
                return Get_Transformed(tx, ty, tz);
            }
        }

        public ClipmapItem Get_Transformed(int tx, int ty, int tz)
        {
            // Clamp coordinates to the array range
            int xx = tx.Clamp(0, axes[0].map.Length - 1);
            int yy = ty.Clamp(0, axes[1].map.Length - 1);
            int zz = tz.Clamp(0, axes[2].map.Length - 1);

            // Pick the furthest one
            int absX = Helpers.Abs(xx);
            int absY = Helpers.Abs(yy);
            int absZ = Helpers.Abs(zz);

            /*if (absX > absZ)
                return (absX > absY) ? m_axes[0].Map[xx] : m_axes[1].Map[yy];

            return absZ > absY ? m_axes[2].Map[zz] : m_axes[1].Map[yy];*/
            int index = 0;
            int value = xx;

            if (absY > absX && absY > absZ)
            {
                index = 1;
                value = yy;
            }
            else if (absZ > absX && absZ > absY)
            {
                index = 2;
                value = zz;
            }

            return axes[index].map[value];
        }

        private void InitAxis(int axis, int forceLOD, float coefLOD)
        {
            AxisInfo axisInfo = axes[axis];
            for (int distance = axisInfo.rangeMin; distance <= axisInfo.rangeMax; distance++)
            {
                int lod = DetermineLOD(distance, forceLOD, coefLOD);
                bool isInVisibilityRange = IsInVisibilityRange(axisInfo, distance);

                axisInfo.map[distance + VisibleRange] = new ClipmapItem
                {
                    lod = lod,
                    isInVisibleRange = isInVisibilityRange
                };
            }
        }

        public void Init(int forceLOD, float coefLOD)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                InitAxis(axis, forceLOD, coefLOD);
            }
        }

        public void SetOffset(int x, int y, int z)
        {
            axes[0].offset = -x;
            axes[1].offset = -y;
            axes[2].offset = -z;
        }

        public int TransformX(int x)
        {
            return
            // Adjust the coordinate depending on the offset
            x + axes[0].offset
            // Center them out
            + VisibleRange;
        }

        public int TransformY(int y)
        {
            return
            // Adjust the coordinate depending on the offset
            y + axes[1].offset
            // Center them out
            + VisibleRange;
        }

        public int TransformZ(int z)
        {
            return
            // Adjust the coordinate depending on the offset
            z + axes[2].offset
            // Center them out
            + VisibleRange;
        }

        public bool IsInsideBounds_Transformed(int tx, int ty, int tz)
        {
            // Clamp coordinates to the array range
            return tx >= 0 && ty >= 0 && tz >= 00 &&
                   tx < axes[0].map.Length &&
                   ty < axes[1].map.Length &&
                   tz < axes[2].map.Length;
        }

        private static int DetermineLOD(int distance, int forceLOD, float coefLOD)
        {
            int lod;
            if (forceLOD >= 0)
            {
                lod = forceLOD;
            }
            else
            {
                if (coefLOD <= 0)
                {
                    return 0;
                }

                // Pick the greater distance and choose a proper LOD
                int dist = Helpers.Abs(distance);
                lod = (int)(dist / (coefLOD * Env.CHUNK_POW));
            }

            // LOD can't be bigger than chunk size
            if (lod < 0)
            {
                lod = 0;
            }

            if (lod > Env.CHUNK_POW)
            {
                lod = Env.CHUNK_POW;
            }

            return lod;
        }

        private bool IsInVisibilityRange(AxisInfo axis, int distance)
        {
            return distance >= axis.rangeMin && distance <= axis.rangeMax;
        }

        private struct AxisInfo
        {
            public ClipmapItem[] map; // -N ... 0 ... N
            public int offset;
            public int rangeMax;
            public int rangeMin;
        }
    }
}
