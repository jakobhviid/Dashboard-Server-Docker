namespace DashboardServer.Updaters.UpdaterResponses
{
    public struct StatsContainerData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ulong CpuUsage { get; set; }
        public int NumOfCpu { get; set; }
        public ulong SystemCpuUsage { get; set; }
        public double CpuPercentage { get; set; }
        public double MemoryPercentage { get; set; }
        public ulong NetInputBytes { get; set; }
        public ulong NetOutputBytes { get; set; }
        public ulong DiskInputBytes { get; set; }
        public ulong DiskOutputBytes { get; set; }
    }
}