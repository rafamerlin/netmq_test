using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace netmq_example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            var pool = new WorkerClientPool();
            pool.StartWorkerClientPair(2);

            var requests = Enumerable.Range(0, 100).Select(x => x.ToString()).ToList();
            var responses = await pool.SendRequests(requests);

            sw.Stop();
            Console.WriteLine("Time to process {0}ms", sw.ElapsedMilliseconds);

            foreach (var response in responses)
            {
                Console.WriteLine(response);
            }
        }
    }
}
