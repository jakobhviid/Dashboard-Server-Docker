using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
namespace DashboardServer
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                // when CTRL C is pressed, terminate the process. Otherwise keep it going
                e.Cancel = true;
                cts.Cancel();
                System.Environment.Exit(0);
            };
            var processesToStart = (Environment.GetEnvironmentVariable("PROCESSES_TO_START") ?? "overviewdata,statsdata,commandserver").Split(",");

            IList<Task> tasks = new List<Task>();

            if (processesToStart.Contains("commandserver"))
            {
                var commandServerTask = new Task(() => CommandServer.CommandServer.Start(cts));
                tasks.Add(commandServerTask);
                commandServerTask.Start();
                Console.WriteLine("Started Command Server");
            }

            var updatersTask = new Task(() =>  Updaters.DockerUpdater.Start());
            tasks.Add(updatersTask);
            updatersTask.Start();
            Console.WriteLine("Started Docker Updaters");
            await Task.WhenAll(tasks); // Don't exit program
        }
    }
}
