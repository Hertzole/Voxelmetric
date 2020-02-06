﻿using System.Collections.Generic;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Common.Threading.Managers
{
    public static class IOPoolManager
    {
        private static readonly List<ITaskPoolItem> workItems = new List<ITaskPoolItem>(2048);

        private static readonly TimeBudgetHandler timeBudget = Features.useThreadedIO ? null : new TimeBudgetHandler(10);

        public static void Add(ITaskPoolItem action)
        {
            workItems.Add(action);
        }

        public static void Commit()
        {
            if (workItems.Count <= 0)
            {
                return;
            }

            // Commit all the work we have
            if (Features.useThreadedIO)
            {
                TaskPool pool = Globals.IOPool;

                for (int i = 0; i < workItems.Count; i++)
                {
                    pool.AddItem(workItems[i]);
                }
                pool.Commit();
            }
            else
            {
                for (int i = 0; i < workItems.Count; i++)
                {
                    timeBudget.StartMeasurement();
                    workItems[i].Run();
                    timeBudget.StopMeasurement();

                    // If the tasks take too much time to finish, spread them out over multiple
                    // frames to avoid performance spikes
                    if (!timeBudget.HasTimeBudget)
                    {
                        workItems.RemoveRange(0, i + 1);
                        return;
                    }
                }
            }

            // Remove processed work items
            workItems.Clear();
        }

        public new static string ToString()
        {
            return Features.useThreadedIO ? Globals.IOPool.ToString() : workItems.Count.ToString();
        }
    }
}
