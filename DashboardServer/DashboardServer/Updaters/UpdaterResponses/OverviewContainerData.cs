using System;

namespace DashboardServer.Updaters.UpdaterResponses {
    public struct OverviewContainerData {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        public DateTime CreationTime { get; set; }
    }
}