using System;
using System.Collections.Generic;
using System.Linq;
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
using Newtonsoft.Json;

namespace DashboardServer.CommandServer {
    public static class ContainerAction {
        private static readonly DockerClient client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
        private static string _responseTopic = KafkaHelpers.ResponseTopic;
        public async static Task RunNewContainer(RunNewContainerParameters parameters, IProducer<Null, string> p) {
            try {
                var exposedPorts = new Dictionary<string, EmptyStruct>();
                var hostPortBindings = new Dictionary<string, IList<PortBinding>>();

                if (parameters.Ports != null) {
                    foreach (var portBinding in parameters.Ports) {
                        exposedPorts.Add(portBinding.HostPort, default(EmptyStruct));
                        hostPortBindings.Add(portBinding.ContainerPort, new List<PortBinding> { new PortBinding { HostPort = portBinding.HostPort } });
                    }
                }

                RestartPolicy restartPolicy = new RestartPolicy { Name = RestartPolicyKind.No };
                if (parameters.RestartPolicy != null) {
                    restartPolicy.MaximumRetryCount = parameters.RestartPolicy.MaximumRetryCount ?? 0;
                    switch (parameters.RestartPolicy.RestartPolicy) {
                        case ContainerRestartPolicy.Always:
                            restartPolicy.Name = RestartPolicyKind.Always;
                            break;
                        case ContainerRestartPolicy.OnFailure:
                            restartPolicy.Name = RestartPolicyKind.OnFailure;
                            break;
                        case ContainerRestartPolicy.UnlessStopped:
                            restartPolicy.Name = RestartPolicyKind.UnlessStopped;
                            break;
                    }
                }
                var environment = new List<string>();
                if (parameters.Environment != null) {
                    foreach (var environmentEntry in parameters.Environment) {
                        environment.Add(environmentEntry.Key + "=" + environmentEntry.Value);
                    }
                }
                var volumes = new List<string>();
                if (parameters.Volumes != null) {
                    foreach (var volumeEntry in parameters.Volumes) {
                        volumes.Add(volumeEntry.HostPath + ":" + volumeEntry.ContainerPath);
                    }
                }
                var dockerResponse = await client.Containers.CreateContainerAsync(new CreateContainerParameters {
                    Image = parameters.Image,
                    Cmd = parameters.Command == null ? null : parameters.Command.Split(" "),
                    Name = parameters.Name,
                    ExposedPorts = exposedPorts,
                    HostConfig = new HostConfig {
                        PortBindings = hostPortBindings,
                        PublishAllPorts = true,
                        RestartPolicy = restartPolicy,
                        VolumesFrom = parameters.VolumesFrom,
                        Binds = volumes,
                        NetworkMode = parameters.NetworkMode,
                    },
                    Env = environment,
                });
                // Update general info as quickly as possible and send it out so clients can see the newly created container quickly
                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                // Send response
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 201,
                    Message = ResponseMessageContracts.CONTAINER_CREATED,
                    ContainerIds = new string[] { dockerResponse.ID }
                }, p);

                await StartContainer(new StartContainerParameters { ContainerId = dockerResponse.ID }, p);
            } catch (DockerApiException ex) {
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 400,
                    Message = ex.Message,
                    ContainerIds = null
                }, p); // TODO: create contract for these error messages
            }
        }

        public async static Task StartContainer(StartContainerParameters parameters, IProducer<Null, string> p) {
            try {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.StartContainerAsync(parameters.ContainerId, new ContainerStartParameters { }, cancellation);

                // Updating the containers with the new configurations right away and sending it out
                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                // Send response
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 200,
                    Message = ResponseMessageContracts.CONTAINER_STARTED,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            } catch (DockerApiException ex) {
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 400,
                    Message = ex.Message,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            }
        }

        public async static Task StopContainer(StopContainerParameters parameters, IProducer<Null, string> p) {
            try {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.StopContainerAsync(parameters.ContainerId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, cancellation);

                // Updating the containers with the new configurations right away and sending it out
                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                // Send response
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 200,
                    Message = ResponseMessageContracts.CONTAINER_STOPPED,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            } catch (DockerApiException ex) {
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 400,
                    Message = ex.Message,
                    ContainerIds = new string[] { parameters.ContainerId }

                }, p);
            }
        }

        public async static Task RemoveContainer(RemoveContainerParameters parameters, IProducer<Null, string> p) {
            try {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.RemoveContainerAsync(parameters.ContainerId, new ContainerRemoveParameters { Force = true, RemoveVolumes = parameters.RemoveVolumes }, cancellation);

                // Updating the containers with the new configurations right away and sending it out
                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                // Send response
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 200,
                    Message = ResponseMessageContracts.CONTAINER_REMOVED,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            } catch (DockerApiException ex) {
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 400,
                    Message = ex.Message,
                    ContainerIds = new string[] { parameters.ContainerId }

                }, p);
            }
        }

        public async static Task RestartContainer(RestartContainerParameters parameters, IProducer<Null, string> p) {
            try {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.RestartContainerAsync(parameters.ContainerId, new ContainerRestartParameters { WaitBeforeKillSeconds = 5 }, cancellation);

                // Updating the containers with the new configurations right away and sending it out
                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                // Send response
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 200,
                    Message = ResponseMessageContracts.CONTAINER_RESTARTED,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            } catch (DockerApiException ex) {
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 400,
                    Message = ex.Message,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            }
        }

        public async static Task RenameContainer(RenameContainerParameter parameters, IProducer<Null, string> p) {
            try {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.RenameContainerAsync(parameters.ContainerId, new ContainerRenameParameters {
                    NewName = parameters.NewName
                }, cancellation);

                // Updating the containers with the new configurations right away and sending it out
                var updatedContainers = await DockerUpdater.FetchOverviewData();
                var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);

                // Send response
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 200,
                    Message = ResponseMessageContracts.CONTAINER_RENAMED,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            } catch (DockerApiException ex) {
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 400,
                    Message = ex.Message,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            }
        }

        public async static Task UpdateConfigContainer(UpdateConfigContainerParameters parameters, IProducer<Null, string> p) {
            try {
                CancellationToken cancellation = new CancellationToken();

                await client.Containers.UpdateContainerAsync(parameters.ContainerId, new ContainerUpdateParameters {
                    BlkioWeight = parameters.BlkioWeight,
                    Memory = parameters.MemoryLimit,
                    MemoryReservation = parameters.MemoryParameters,
                    MemorySwap = parameters.MemorySwapLimit,
                    KernelMemory = parameters.KernelMemory,
                    // RestartPolicy = parameters.RestartPolicy, TODO:
                    CPUShares = parameters.CPUShares,
                    CPUPeriod = parameters.CPUPeriod,
                    CpusetCpus = parameters.CPUSetCPUs,
                    CpusetMems = parameters.CPUSetMems
                }, cancellation);

                // Updating the containers with the new configurations right away and sending it out
                var updatedContainers = await DockerUpdater.FetchStatsData();
                var overviewContainerUpdate = DockerUpdater.CreateStatsData(updatedContainers);
                await KafkaHelpers.SendMessageAsync(DockerUpdater.StatsTopic, overviewContainerUpdate, p);

                // Send response
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 200,
                    Message = ResponseMessageContracts.CONTAINER_CONFIGURATION_UPDATED,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            } catch (DockerApiException ex) {
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 400,
                    Message = ex.Message,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            }
        }

        public async static Task RefetchOverviewData(IProducer<Null, string> p) {
            var updatedContainers = await DockerUpdater.FetchOverviewData();
            var overviewContainerUpdate = DockerUpdater.CreateOverViewData(updatedContainers);
            await KafkaHelpers.SendMessageAsync(DockerUpdater.OverviewTopic, overviewContainerUpdate, p);
            // Send response
            await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                ResponseStatusCode = 200,
                Message = ResponseMessageContracts.OVERVIEW_DATA_REFETCHED,
                ContainerIds = updatedContainers.Select(container => container.Id).ToArray()
            }, p);
        }

        public async static Task RefetchStatsData(IProducer<Null, string> p) {
            // Updating the containers with the new configurations right away and sending it out
            var updatedContainers = await DockerUpdater.FetchStatsData();
            var overviewContainerUpdate = DockerUpdater.CreateStatsData(updatedContainers);
            await KafkaHelpers.SendMessageAsync(DockerUpdater.StatsTopic, overviewContainerUpdate, p);
            // Send response
            await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                ResponseStatusCode = 200,
                Message = ResponseMessageContracts.STATS_DATA_REFETCHED,
                ContainerIds = updatedContainers.Select(container => container.Id).ToArray()
            }, p);
        }

        public async static Task InspectContainer(InspectContainerParameters parameters, IProducer<Null, string> p) {
            try {
                CancellationToken cancellation = new CancellationToken();
                var inspectResponse = await client.Containers.InspectContainerAsync(parameters.ContainerId, cancellation);
                var containerId = inspectResponse.ID.Substring(0, 10);
                var inspectResponseStr = JsonConvert.SerializeObject(inspectResponse, Formatting.Indented);

                var inspectContainerResponse = new InspectContainerResponse
                {
                    ServerName = KafkaHelpers.Servername,
                    ContainerId = containerId,
                    RawData = inspectResponseStr
                };

                await KafkaHelpers.SendMessageAsync(DockerUpdater.InspectTopic, inspectContainerResponse, p);

                // Send response
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 200,
                    Message = ResponseMessageContracts.CONTAINER_INSPECTED,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            } catch (DockerApiException ex) {
                await KafkaHelpers.SendMessageAsync(_responseTopic, new ContainerResponse {
                    ResponseStatusCode = 400,
                    Message = ex.Message,
                    ContainerIds = new string[] { parameters.ContainerId }
                }, p);
            }
        }

        public async static Task LogFromContainer(LogContainerParameters parameters, IProducer<Null, string> p)
        {
            //TODO: Need to add a log function.
            throw new NotImplementedException();
        }
    }
}
