using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using DashboardServer.Updaters.UpdaterResponses;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DashboardServer.Updaters
{
    public class DockerUpdater
    {
        private static readonly DockerClient _client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
        private static string _bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_URL") ?? "kafka1.cfei.dk:9092,kafka2.cfei.dk:9092,kafka3.cfei.dk:9092";
        private const string _overviewTopic = "0913c91b-ec2b-4b69-ad8c-d9c8d25f1464-general_info";
        private const string _statsTopic = "0913c91b-ec2b-4b69-ad8c-d9c8d25f1464-stats_info";
        private static int _intervalDelay = Convert.ToInt32(Environment.GetEnvironmentVariable("INTERVAL")) == 0 ? 5 : Convert.ToInt32(Environment.GetEnvironmentVariable("INTERVAL"));
        private static string _servername = Environment.GetEnvironmentVariable("SERVER_NAME") ?? "PlaceholderServer";
        private static string _selfContainerId = Dns.GetHostName()[..10];
        private static string[] _processesToStart = (Environment.GetEnvironmentVariable("PROCESSES_TO_START") ?? "overviewdata,statsdata,commandserver").Split(",");
        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public async static Task Start()
        {
            var producerConfig = new ProducerConfig { BootstrapServers = _bootstrapServers, Acks = Acks.Leader };
            using var p = new ProducerBuilder<Null, string>(producerConfig).Build();

            List<Task> tester = new List<Task>();
            if (_processesToStart.Contains("overviewdata"))
            {
                var overviewTask = SendOverViewData(p);
                tester.Add(overviewTask);
            }
            if (_processesToStart.Contains("statsdata"))
            {
                var statsTask = SendStatsData(p);
                tester.Add(statsTask);
            }

            await Task.WhenAll(tester);
        }

        private struct OverViewData
        {
            public string Servername { get; set; }
            public IList<OverviewContainerData> Containers { get; set; }
            public string CommandRequestTopic { get; set; }
            public string CommandResponseTopic { get; set; }
        }

        private static async Task SendOverViewData(IProducer<Null, string> p)
        {
            while (true)
            {
                Console.WriteLine("Sending overview data every " + _intervalDelay + " seconds");
                var containers = await _client.Containers.ListContainersAsync(
                    new ContainersListParameters
                    {
                        All = true,
                    }
                );

                var containerData = new List<OverviewContainerData>();

                foreach (var container in containers)
                {
                    // TODO: Change this to an observer pattern where data is only sent if one of the containers have changed (beside uptime)
                    if (container.ID[..10] == _selfContainerId)continue;
                    containerData.Add(new OverviewContainerData
                    {
                        Id = container.ID[..10],
                            Name = container.Names[0],
                            Image = container.Image,
                            CreationTime = container.Created,
                            State = container.State,
                            Status = container.Status
                    });
                }

                var dataToSend = new OverViewData
                {
                    Servername = _servername,
                    Containers = containerData
                };

                if (_processesToStart.Contains("commandserver")) // If command server is active on this container, provice the relevant topics
                {
                    dataToSend.CommandRequestTopic = _servername + _selfContainerId + "command-requests"; // servername plus this specific container id + command-requests
                    dataToSend.CommandResponseTopic = _servername + _selfContainerId + "command-requests";
                }

                SendMessage(_overviewTopic, dataToSend, p);
                await Task.Delay(TimeSpan.FromSeconds(_intervalDelay));
            }
        }

        private struct StatsData
        {
            public string Servername { get; set; }
            public IList<StatsContainerData> Containers { get; set; }
            public string CommandRequestTopic { get; set; }
            public string CommandResponseTopic { get; set; }
        }
        private static async Task SendStatsData(IProducer<Null, string> p)
        {
            while (true)
            {
                Console.WriteLine("Sending stats data every " + _intervalDelay + " seconds");
                var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters());
                CancellationTokenSource cancellation = new CancellationTokenSource();

                var containerData = new List<StatsContainerData>();

                foreach (var container in containers)
                {
                    if (container.ID[..10] == _selfContainerId)continue;
                    var responseHandler = new Progress<ContainerStatsResponse>(delegate(ContainerStatsResponse ctr)
                    {
                        if (ctr.PreCPUStats.SystemUsage == 0)return; // it should read stats twice before it's possible to read the relevant data
                        else
                        {
                            var numOfCpu = ctr.CPUStats.CPUUsage.PercpuUsage.Count;
                            var currentCpuUsage = ctr.CPUStats.CPUUsage.TotalUsage;
                            var previousCpuUsage = ctr.PreCPUStats.CPUUsage.TotalUsage;
                            var currentSystemCpuUsage = ctr.CPUStats.SystemUsage;
                            var previousSystemCpuUsage = ctr.PreCPUStats.SystemUsage;
                            containerData.Add(new StatsContainerData
                            {
                                Id = container.ID[..10],
                                    Name = container.Names[0],
                                    NumOfCpu = numOfCpu,
                                    CpuUsage = currentCpuUsage,
                                    SystemCpuUsage = currentSystemCpuUsage,
                                    CpuPercentage = CalculateCpuPercentage(numOfCpu, currentCpuUsage, previousCpuUsage, currentSystemCpuUsage, previousSystemCpuUsage),
                                    MemoryPercentage = CalculateMemoryPercentage(ctr.MemoryStats.Limit, ctr.MemoryStats.Usage),
                                    DiskInputBytes = ctr.StorageStats.ReadSizeBytes, // TODO: Check if this value is correct or if you have to use the commented python code at the bottom of this file
                                    DiskOutputBytes = ctr.StorageStats.WriteSizeBytes, // TODO: Same as above
                                    NetInputBytes = CalculateNetInputBytes(ctr),
                                    NetOutputBytes = CalculateNetOutputBytes(ctr),
                            });
                            cancellation.Cancel(); // Only read twice every interval for cpu usage calculation
                        }
                    });
                    // Start a stream of stats which will get closed after reading twice
                    await _client.Containers.GetContainerStatsAsync(container.ID, new ContainerStatsParameters { }, responseHandler, cancellation.Token);

                    var dataToSend = new StatsData
                    {
                        Servername = _servername,
                        Containers = containerData,
                    };

                    if (_processesToStart.Contains("commandserver")) // If command server is active on this container, provice the relevant topics
                    {
                        dataToSend.CommandRequestTopic = _servername + _selfContainerId + "command-requests"; // servername plus this specific container id + command-requests
                        dataToSend.CommandResponseTopic = _servername + _selfContainerId + "command-requests";
                    }

                    SendMessage(_statsTopic, dataToSend, p);
                    await Task.Delay(TimeSpan.FromSeconds(_intervalDelay));
                }
            }
        }

        private static async void SendMessage(string topic, object messageToSerialize, IProducer<Null, string> p)
        {
            try
            {
                var deliveryReport = await p.ProduceAsync(
                    topic, new Message<Null, string> { Value = JsonConvert.SerializeObject(messageToSerialize, _jsonSettings) });

                Console.WriteLine($"delivered to: {deliveryReport.TopicPartitionOffset}");
            }
            catch (ProduceException<string, string> e)
            {
                Console.WriteLine($"Failed to deliver message: {e.Message} [{e.Error.Code}]");
            }
        }

        private static double CalculateCpuPercentage(int numOfCpu, ulong currentCpuUsage, ulong previousCpuUsage, ulong currentSystemCpuUsage, ulong previousSystemCpuUsage)
        {
            float cpuDelta = currentCpuUsage - previousCpuUsage;
            float systemCpuDelta = currentSystemCpuUsage - previousSystemCpuUsage;
            var CpuPercentage = ((cpuDelta / systemCpuDelta) * (ulong)numOfCpu) * 100;

            return Math.Round(CpuPercentage, 2); // round down to two decimals
        }

        private static double CalculateMemoryPercentage(ulong memoryLimit, ulong memoryUsage)
        {
            double memoryPercentage = memoryUsage / memoryLimit;
            return Math.Round(memoryPercentage, 2);
        }

        private static ulong CalculateNetInputBytes(ContainerStatsResponse st)
        {
            // RX Bytes = total number of bytes recieved over a network
            ulong totalRxBytes = 0;
            foreach (var network in st.Networks)
            {
                totalRxBytes += network.Value.RxBytes;
            }
            return totalRxBytes;
        }

        private static ulong CalculateNetOutputBytes(ContainerStatsResponse st)
        {
            // TX Bytes = total number of bytes transmitted over a network interface
            ulong totalTxBytes = 0;
            foreach (var network in st.Networks)
            {
                totalTxBytes += network.Value.TxBytes;
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
