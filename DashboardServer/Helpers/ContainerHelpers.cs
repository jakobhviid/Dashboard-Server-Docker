using System;
using System.Collections.Generic;
using DashboardServer.Updaters.UpdaterResponses;

namespace DashboardServer.Helpers
{
    public static class ContainerHelpers
    {
        /*
        CompareOverViewContainers compare two containers on all relevant parameters
        These parameters are for example if the container has stopped or started sinde last read
        Igonored parameters are status (e.g. Up 2 hours / Up 4 hours) and updatetime
        */
        public static bool OverviewContainersAreDifferent(IList<OverviewContainerData> lastContainerReads, IList<OverviewContainerData> currentContainerReads)
        {
            if (lastContainerReads == null || lastContainerReads.Count == 0 || currentContainerReads == null || currentContainerReads.Count == 0)
            {
                return true;
            }
            if (!lastContainerReads.Count.Equals(currentContainerReads.Count))
            {
                return true;
            }
            for (int i = 0; i < lastContainerReads.Count; i++)
            {
                // The Docker API always returns the containers in the same order, so this is possible
                var lastRead = lastContainerReads[i];
                var currentRead = currentContainerReads[i];
                if (!lastRead.Id.Equals(currentRead.Id))return true;
                if (!lastRead.Image.Equals(currentRead.Image))return true;
                if (!lastRead.Name.Equals(currentRead.Name))return true;
                if (!lastRead.State.Equals(currentRead.State))return true;
                var currentHealthData = ExtractHealthDataFromStatus(currentRead.Status);
                if (lastRead.Health != null && !lastRead.Health.Equals(currentHealthData)) return true;
                if (!lastRead.CreationTime.Equals(currentRead.CreationTime))return true;

            }

            return false;
        }

        // TODO: environment variables for these tolerances?
        private readonly static int _percentageTolerance = Convert.ToInt32(Environment.GetEnvironmentVariable("CPU_MEM_TOLERANCE_PERCENT")) == 0 ? 15 : Convert.ToInt32(Environment.GetEnvironmentVariable("CPU_MEM_TOLERANCE_PERCENT"));
        private readonly static int _netInputBytesTolerance = Convert.ToInt32(Environment.GetEnvironmentVariable("NET_INPUT_TOLERANCE_BYTES")) == 0 ? 100 : Convert.ToInt32(Environment.GetEnvironmentVariable("NET_INPUT_TOLERANCE_BYTES"));
        private readonly static int _netOutputBytesTolerance = Convert.ToInt32(Environment.GetEnvironmentVariable("NET_OUTPUT_TOLERANCE_BYTES")) == 0 ? 100 : Convert.ToInt32(Environment.GetEnvironmentVariable("NET_OUTPUT_TOLERANCE_BYTES"));
        private readonly static int _diskInputBytesTolerance = Convert.ToInt32(Environment.GetEnvironmentVariable("DISK_INPUT_TOLERANCE_BYTES")) == 0 ? 100 : Convert.ToInt32(Environment.GetEnvironmentVariable("DISK_INPUT_TOLERANCE_BYTES"));
        private readonly static int _diskOutputBytesTolerance = Convert.ToInt32(Environment.GetEnvironmentVariable("DISK_OUTPUT_TOLERANCE_BYTES")) == 0 ? 100 : Convert.ToInt32(Environment.GetEnvironmentVariable("DISK_INPUT_TOLERANCE_BYTES"));

        public static string ExtractHealthDataFromStatus(string containerStatus)
        {
            // The container does not have health data
            if (!containerStatus.ToLower().Contains("health"))return null;
            var indexOfHealthSubString = containerStatus.IndexOf("health");
            var indexOfLastParentheses = containerStatus.LastIndexOf(")");
            return containerStatus[indexOfHealthSubString..indexOfLastParentheses];
        }
        public static bool StatsContainersAreDifferent(IList<StatsContainerData> lastContainerReads, IList<StatsContainerData> currentContainerReads)
        {
            if (lastContainerReads == null || lastContainerReads.Count == 0 || currentContainerReads == null || currentContainerReads.Count == 0)return true;
            if (!lastContainerReads.Count.Equals(currentContainerReads.Count))return true;

            bool containerIsDifferent = false;
            for (int i = 0; i < lastContainerReads.Count; i++)
            {
                // The Docker API always returns the containers in the same order as long as new containers haven't been added. In which case information should be sent again
                StatsContainerData lastRead = lastContainerReads[i];
                StatsContainerData currentRead = currentContainerReads[i];
                if (!lastRead.Id.Equals(currentRead.Id))containerIsDifferent = true;

                if (!lastRead.Name.Equals(currentRead.Name))containerIsDifferent = true;

                if (currentRead.CpuPercentage > lastRead.CpuPercentage + _percentageTolerance ||
                    currentRead.CpuPercentage < lastRead.CpuPercentage - _percentageTolerance)containerIsDifferent = true;

                if (currentRead.MemoryPercentage > lastRead.MemoryPercentage + _percentageTolerance ||
                    currentRead.MemoryPercentage < lastRead.MemoryPercentage - _percentageTolerance)containerIsDifferent = true;

                // The amount of bytes can only go up, if the it is the same container, which we check for when we check the ID. 
                // So we only need to check if it has increased above the tolerance
                if (currentRead.NetInputBytes > lastRead.NetInputBytes + (ulong)_netInputBytesTolerance)containerIsDifferent = true;

                if (currentRead.NetOutputBytes > lastRead.NetOutputBytes + (ulong)_netOutputBytesTolerance)containerIsDifferent = true;

                if (currentRead.DiskInputBytes > lastRead.DiskInputBytes + (ulong)_diskInputBytesTolerance)containerIsDifferent = true;

                if (currentRead.DiskOutputBytes > lastRead.DiskOutputBytes + (ulong)_diskOutputBytesTolerance)containerIsDifferent = true;

                if (containerIsDifferent)break;
            }

            return containerIsDifferent;
        }
    }
}
