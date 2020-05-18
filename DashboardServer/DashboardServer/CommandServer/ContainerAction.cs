using System.Threading;
using System.Threading.Tasks;
using DashboardServer.CommandServer.ContainerRequests;
using Confluent.Kafka;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Text.Json;
using DashboardServer.CommandServer.ContainerResponses;

namespace DashboardServer.CommandServer
{
    public static class ContainerAction
    {
        private static readonly DockerClient client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
        private static readonly string responseTopic = "container1-commands-responses";
        
        public async static void RunNewContainer(RunNewContainerParameters parameters, IProducer<Null, string> p)
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
            }
            catch (DockerApiException ex)
            {
                Console.Error.WriteLine(ex.Message);
                await p.ProduceAsync(responseTopic, new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(new ContainerResponse
                    {
                        ResponseStatusCode = 400,
                        Message = ex.Message
                    })
                });
            }
        }

        public async static void RenameContainer(RenameContainerParameter parameters, IProducer<Null, string> p)
        {
            try
            {
                CancellationToken cancellation = new CancellationToken();
                await client.Containers.RenameContainerAsync(parameters.ContainerId, new ContainerRenameParameters
                {
                    NewName = parameters.NewName
                }, cancellation);
            }
            catch (DockerApiException ex)
            {
                Console.Error.WriteLine(ex.Message);
                await p.ProduceAsync(responseTopic, new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(new ContainerResponse
                    {
                        ResponseStatusCode = 400,
                        Message = ex.Message
                    })
                });
            }


        }
    }
}