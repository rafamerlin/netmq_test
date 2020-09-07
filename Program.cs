using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace netmq_example
{
    class Program
    {
        static void Main(string[] args)
        {
            var ports = new[] { 5555, 5556, 5557 };
            var numberOfTasks = ports.Length;

            var workerTasks = new Task[numberOfTasks];

            for (int i = 0; i < numberOfTasks; i++)
            {
                //If running on WSL2 use "python3" here instead of just "python"
                ProcessStartInfo startInfo = new ProcessStartInfo("python3");
                startInfo.WindowStyle = ProcessWindowStyle.Minimized;
                startInfo.Arguments = $"test.py tcp://127.0.0.1:{ports[i]} {i}";

                workerTasks[i] = (ProcessAsyncHelper.RunAsync(startInfo));

                Console.WriteLine("Started another process");
            }

            var payload = @"!Payload!";

            var clientRuns = 3333;
            var clientTasks = new List<Task>();

            var sw = new Stopwatch();
            sw.Start();


            for (int i = 0; i < numberOfTasks; i++)
            {
                var internalI = i;
                clientTasks.Add(
                    Task.Factory.StartNew(() =>
                    {
                        using (var client = new RequestSocket($">tcp://127.0.0.1:{ports[internalI]}")) // connect
                        {
                            var baseJ = internalI * clientRuns;
                            for (int j = 0; j < clientRuns; j++)
                            {
                                var calculatedJ = baseJ + j;
                                // Send a message from the client socket
                                client.SendFrame($"Hello from {calculatedJ}, {payload}");

                                // Receive the response from the client socket
                                string m2 = client.ReceiveFrameString();
                                Console.WriteLine($"{calculatedJ} -> From Server: {m2}");
                            }
                        }
                    })
                );
            }

            Task.WaitAll(clientTasks.ToArray());

            sw.Stop();
            Console.WriteLine("Time to process {0}ms", sw.ElapsedMilliseconds);
        }
    }
}
