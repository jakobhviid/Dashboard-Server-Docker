using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Confluent.Kafka;
using DashboardServer.Helpers;
using DashboardServer.Updaters.UpdaterResponses;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace DashboardServer.Updaters {
    public class DockerUpdater {
        private static readonly Uri DockerSocketUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
        private static readonly DockerClient _client = new DockerClientConfiguration(DockerSocketUri).CreateClient();
        public const string LogTopic = "e9c03edc-40b8-4aa6-a72e-83f9ea0d1cea-log_info";
        public const string OverviewTopic = "f0e1e946-50d0-4a2b-b1a5-f21b92e09ac1-general_info";
        public const string StatsTopic = "33a325ce-b0c0-43a7-a846-4f46acdb367e-stats_info";
        public const string InspectTopic = "8f69fb50-ad11-4a9d-b4ab-21fba03053f2-inspect_info";
        private static int _checkInterval = Convert.ToInt32(Environment.GetEnvironmentVariable("DASHBOARDS_CHECK_INTERVAL_SECONDS")) == 0 ? 15 : Convert.ToInt32(Environment.GetEnvironmentVariable("DASHBOARDS_CHECK_INTERVAL_SECONDS"));
        private static int _sendInterval = 15;
        private static string[] _processesToStart = (Environment.GetEnvironmentVariable("DASHBOARDS_PROCESSES_TO_START") ?? "overviewdata,statsdata,commandserver").Split(",");

        public static void Start() {
            var producerConfig = new ProducerConfig { BootstrapServers = KafkaHelpers.BootstrapServers, Acks = Acks.Leader };
            KafkaHelpers.SetKafkaConfigKerberos(producerConfig);
            
            using var p = new ProducerBuilder<Null, string>(producerConfig).Build();

            Task[] tester = new Task[2];
            if (_processesToStart.Contains("overviewdata")) {
                var overviewTask = SendOverViewData(p);
                tester[0] = overviewTask;
            }
            if (_processesToStart.Contains("statsdata")) {
                var statsTask = SendStatsData(p);
                tester[1] = statsTask;
            }

            Task.WaitAll(tester);
        }

        public struct OverViewData {
            public string Servername { get; set; }
            public IList<OverviewContainerData> Containers { get; set; }
            public string CommandRequestTopic { get; set; }
            public string CommandResponseTopic { get; set; }
            public string Timestamp { get; set; }
        }

        private static async Task SendOverViewData(IProducer<Null, string> p) {
            var latestRead = CreateOverViewData();

            var latestSendTime = DateTime.Now;

            while (true) {
                new Task(async () => {
                    var containerData = await FetchOverviewData();
                    if (ContainerHelpers.OverviewContainersAreDifferent(latestRead.Containers, containerData) ||
                        latestSendTime.AddMinutes(_sendInterval) < DateTime.Now) {
                        latestRead.Containers = containerData;
                        latestSendTime = DateTime.Now;
                        await KafkaHelpers.SendMessageAsync(OverviewTopic, latestRead, p);
                    }
                }).Start();

                await Task.Delay(TimeSpan.FromSeconds(_checkInterval));
            }
        }

        public static OverViewData CreateOverViewData(IList<OverviewContainerData> containers = null) {
            OverViewData overViewData = new OverViewData {
                Servername = KafkaHelpers.Servername,
                Timestamp = DateTime.Now.ToString("HH:mm dd/MM/yy")
            };

            if (_processesToStart.Contains("commandserver")) // If command server is active on this container, provide the relevant topics
            {
                overViewData.CommandRequestTopic = KafkaHelpers.RequestTopic;
                overViewData.CommandResponseTopic = KafkaHelpers.ResponseTopic;
            }
            if (containers != null)
                overViewData.Containers = containers;

            return overViewData;
        }

        public static async Task<IList<OverviewContainerData>> FetchOverviewData() {
            var containers = await _client.Containers.ListContainersAsync(
                new ContainersListParameters {
                    All = true,
                }
            );

            var containerData = new List<OverviewContainerData>();
            foreach (var container in containers) {
                containerData.Add(new OverviewContainerData {
                    Id = container.ID[..10],
                    Name = container.Names[0][1..],
                    Image = container.Image,
                    CreationTime = container.Created,
                    State = container.State,
                    Status = container.Status,
                    UpdateTime = DateTime.Now,
                    Health = ContainerHelpers.ExtractHealthDataFromStatus(container.Status)
                });
            }

            return containerData;
        }

        public struct StatsData {
            public string Servername { get; set; }
            public IList<StatsContainerData> Containers { get; set; }
            public string CommandRequestTopic { get; set; }
            public string CommandResponseTopic { get; set; }
        }

        public static StatsData CreateStatsData(IList<StatsContainerData> containers = null) {
            StatsData latestRead = new StatsData {
                Servername = KafkaHelpers.Servername
            };

            if (_processesToStart.Contains("commandserver")) // If command server is active on this container, provide the relevant topics
            {
                latestRead.CommandRequestTopic = KafkaHelpers.RequestTopic;
                latestRead.CommandResponseTopic = KafkaHelpers.ResponseTopic;
            }
            if (containers != null)
                latestRead.Containers = containers;

            return latestRead;
        }
        private static async Task SendStatsData(IProducer<Null, string> p) {
            StatsData latestRead = CreateStatsData();

            var latestSendTime = DateTime.Now;

            while (true) {
                new Task(async () => {
                    var containerData = await FetchStatsData();
                    if (ContainerHelpers.StatsContainersAreDifferent(latestRead.Containers, containerData) ||
                        latestSendTime.AddMinutes(_sendInterval) < DateTime.Now) {
                        latestRead.Containers = containerData;
                        latestSendTime = DateTime.Now;
                        await KafkaHelpers.SendMessageAsync(StatsTopic, latestRead, p);
                    }
                }).Start();

                await Task.Delay(TimeSpan.FromSeconds(_checkInterval));
            }
        }

        public static async Task<IList<StatsContainerData>> FetchStatsData() {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters());

            var containerData = new List<StatsContainerData>();
            foreach (var container in containers) {
                var responseHandler = new Progress<ContainerStatsResponse>(delegate (ContainerStatsResponse ctr) {
                    try {
                        if (ctr.PreCPUStats.SystemUsage == 0) return; // it should read stats twice before it's possible to read the relevant data
                        else {
                            var numOfCpu = ctr.CPUStats.CPUUsage.PercpuUsage.Count;
                            var currentCpuUsage = ctr.CPUStats.CPUUsage.TotalUsage;
                            var previousCpuUsage = ctr.PreCPUStats.CPUUsage.TotalUsage;
                            var currentSystemCpuUsage = ctr.CPUStats.SystemUsage;
                            var previousSystemCpuUsage = ctr.PreCPUStats.SystemUsage;
                            containerData.Add(new StatsContainerData {
                                Id = container.ID[..10],
                                Name = container.Names[0][1..],
                                Image = container.Image,
                                NumOfCpu = numOfCpu,
                                CpuUsage = currentCpuUsage,
                                SystemCpuUsage = currentSystemCpuUsage,
                                CpuPercentage = CalculateCpuPercentage(numOfCpu, currentCpuUsage, previousCpuUsage, currentSystemCpuUsage, previousSystemCpuUsage),
                                MemoryPercentage = CalculateMemoryPercentage(ctr.MemoryStats.Limit, ctr.MemoryStats.Usage),
                                DiskInputBytes = ctr.StorageStats.ReadSizeBytes, // TODO: Check if this value is correct or if you have to use the commented python code at the bottom of this file
                                DiskOutputBytes = ctr.StorageStats.WriteSizeBytes, // TODO: Same as above
                                NetInputBytes = CalculateNetInputBytes(ctr),
                                NetOutputBytes = CalculateNetOutputBytes(ctr),
                                UpdateTime = DateTime.Now,
                            });
                        }
                    } catch (System.NullReferenceException ex) {
                        // This will be called in case a container is closed down during a stats read.
                        // In this case the stats data should just be ignored
                        Console.WriteLine("Ignored Data for container " + container.ID);
                        Console.WriteLine(ex);
                    }
                });
                try {
                    await _client.Containers.GetContainerStatsAsync(container.ID, new ContainerStatsParameters { Stream = false }, responseHandler);
                } catch (Docker.DotNet.DockerContainerNotFoundException) {
                    // Race condition if containers are removed while it's fetching stats data.
                    // Just ignore the removed container
                }
            }

            return containerData;
        }

        private static double CalculateCpuPercentage(int numOfCpu, ulong currentCpuUsage, ulong previousCpuUsage, ulong currentSystemCpuUsage, ulong previousSystemCpuUsage) {
            float cpuDelta = currentCpuUsage - previousCpuUsage;
            float systemCpuDelta = currentSystemCpuUsage - previousSystemCpuUsage;
            var CpuPercentage = ((cpuDelta / systemCpuDelta) * (ulong)numOfCpu) * 100;

            return Math.Round(CpuPercentage, 2); // round down to two decimals
        }

        private static double CalculateMemoryPercentage(ulong memoryLimit, ulong memoryUsage) {
            double memoryPercentage = ((double)memoryUsage / (double)memoryLimit) * 100;
            return Math.Round(memoryPercentage, 2);
        }

        private static ulong CalculateNetInputBytes(ContainerStatsResponse st) {
            // RX Bytes = total number of bytes recieved over a network
            ulong totalRxBytes = 0;
            if (st.Networks != null) {
                foreach (var network in st.Networks) {
                    totalRxBytes += network.Value.RxBytes;
                }
            }
            return totalRxBytes;
        }

        private static ulong CalculateNetOutputBytes(ContainerStatsResponse st) {
            // TX Bytes = total number of bytes transmitted over a network interface
            ulong totalTxBytes = 0;
            if (st.Networks != null) {
                foreach (var network in st.Networks) {
                    totalTxBytes += network.Value.TxBytes;
                }
            }
            return totalTxBytes;
        }

        // Python disk read write operation
        //total_disk_read_bytes = 0
        //total_disk_write_bytes = 0
        //for io_operation in container_stats['blkio_stats']['io_service_bytes_recursive']:
        //    operation_type = io_operation['op']
        //    operation_value_bytes = io_operation['value']
        //    if operation_type == 'Read':
        //        total_disk_read_bytes = total_disk_read_bytes + operation_value_bytes
        //    elif operation_type == 'Write':
        //        total_disk_write_bytes = total_disk_write_bytes + operation_value_bytes

        //container_stats_data.with_disk_i_o(
        //    total_disk_read_bytes, total_disk_write_bytes)
    }
}
