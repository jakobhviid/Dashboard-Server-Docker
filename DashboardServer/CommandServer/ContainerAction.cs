using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using DashboardServer.CommandServer.ContainerRequests;
using DashboardServer.CommandServer.ContainerResponses;
using DashboardServer.CommandServer.Contracts;
using DashboardServer.Helpers;
using DashboardServer.Updaters;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace DashboardServer.CommandServer
{
    public static class ContainerAction
    {
        private static readonly DockerClient client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
        private static string _responseTopic = KafkaHelpers.ResponseTopic;
        public async static Task RunNewContainer(RunNewContainerParameters parameters, IProducer<Null, string> p)
        {
            try
            {
                await client.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = parameters.Image,
                        Cmd = parameters.Command,
                        Name = parameters.Name,
                        HostConfig = new HostConfig
                        {
                            PortBindings = parameters.Ports,
                                RestartPolicy = parameters.RestartPolicy,
                                VolumesFrom = parameters.VolumesFrom
                        },
                        Env = parameters.Environment,
                        Volumes = parameters.Volumes,
                });

                // Update general info as quickly as possible and send it out so clients can see the newly created container quickly
                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 201, Message = ResponseMessageContracts.CONTAINER_CREATED }, p);

            }
            catch (DockerApiException ex)
            {
                Console.Error.WriteLine(ex.Message);
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 400, Message = ex.Message }, p); // TODO: create contract for these error messages
            }
        }

        public async static Task StartContainer(StartContainerParameters parameters, IProducer<Null, string> p)
        {
            try
            {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.StartContainerAsync(parameters.ContainerId, new ContainerStartParameters { }, cancellation);

                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 200, Message = ResponseMessageContracts.CONTAINER_STARTED }, p);
            }
            catch (DockerApiException ex)
            {
                Console.Error.WriteLine(ex.Message);
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 400, Message = ex.Message }, p);
            }
        }

        public async static Task StopContainer(StopContainerParameters parameters, IProducer<Null, string> p)
        {
            try
            {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.StopContainerAsync(parameters.ContainerId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, cancellation);

                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 200, Message = ResponseMessageContracts.CONTAINER_STOPPED }, p);
            }
            catch (DockerApiException ex)
            {
                Console.Error.WriteLine(ex.Message);
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 400, Message = ex.Message }, p);
            }
        }

        public async static Task RemoveContainer(RemoveContainerParameters parameters, IProducer<Null, string> p)
        {
            try
            {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.RemoveContainerAsync(parameters.ContainerId, new ContainerRemoveParameters { Force = true, RemoveVolumes = parameters.RemoveVolumes }, cancellation);

                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 200, Message = ResponseMessageContracts.CONTAINER_REMOVED }, p);
            }
            catch (DockerApiException ex)
            {
                Console.Error.WriteLine(ex.Message);
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 400, Message = ex.Message }, p);
            }
        }

        public async static Task RestartContainer(RestartContainerParameters parameters, IProducer<Null, string> p)
        {
            try
            {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.RestartContainerAsync(parameters.ContainerId, new ContainerRestartParameters { WaitBeforeKillSeconds = 5 }, cancellation);

                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 200, Message = ResponseMessageContracts.CONTAINER_RESTARTED }, p);
            }
            catch (DockerApiException ex)
            {
                Console.Error.WriteLine(ex.Message);
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 400, Message = ex.Message }, p);
            }
        }

        public async static Task RenameContainer(RenameContainerParameter parameters, IProducer<Null, string> p)
        {
            try
            {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.RenameContainerAsync(parameters.ContainerId, new ContainerRenameParameters
                {
                    NewName = parameters.NewName
                }, cancellation);

                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 200, Message = ResponseMessageContracts.CONTAINER_RENAMED }, p);
            }
            catch (DockerApiException ex)
            {
                Console.Error.WriteLine(ex.Message);
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 400, Message = ex.Message }, p);
            }
        }

        public async static Task UpdateConfigContainer(UpdateConfigContainerParameters parameters, IProducer<Null, string> p)
        {
            try
            {
                CancellationToken cancellation = new CancellationToken();

                await client.Containers.UpdateContainerAsync(parameters.ContainerId, new ContainerUpdateParameters
                {
                    BlkioWeight = parameters.BlkioWeight,
                        Memory = parameters.MemoryLimit,
                        MemoryReservation = parameters.MemoryParameters,
                        MemorySwap = parameters.MemorySwapLimit,
                        KernelMemory = parameters.KernelMemory,
                        RestartPolicy = parameters.RestartPolicy,
                        CPUShares = parameters.CPUShares,
                        CPUPeriod = parameters.CPUPeriod,
                        CpusetCpus = parameters.CPUSetCPUs,
                        CpusetMems = parameters.CPUSetMems
                }, cancellation);

                // Updating the containers with the new configurations right away and sending it out
                var updatedContainers = await DockerUpdater.FetchStatsData();
                var overviewContainerUpdate = DockerUpdater.CreateStatsData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                // Notifying that all went successfully
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 200, Message = ResponseMessageContracts.CONTAINER_CONFIGURATION_UPDATED }, p);
            }
            catch (DockerApiException ex)
            {
                Console.Error.WriteLine(ex.Message);
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse { ResponseStatusCode = 400, Message = ex.Message }, p);
            }
        }
    }
}
