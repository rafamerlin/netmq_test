using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;


namespace netmq_example
{
    public class WorkerClientPair
    {
        private readonly int _portNumber;
        private readonly BlockingCollection<RequestWrapper> _queue;
        private Task _consumeQueueTask;

        public WorkerClientPair(int portNumber, BlockingCollection<RequestWrapper> queue)
        {
            _portNumber = portNumber;
            _queue = queue;
        }

        private void ConsumeQueue()
        {
            using (var client = new RequestSocket($"tcp://127.0.0.1:{_portNumber}"))
            {
                //var address = ;

                //client.Connect(address);

                var sw = new Stopwatch();
                sw.Start();
                Console.WriteLine($"{_portNumber} sending hello");
                client.SendFrame("HELLO");
                Console.WriteLine($"{_portNumber} sent hello in {sw.ElapsedMilliseconds}ms");

                var helloResponse = client.ReceiveFrameString();
                Console.WriteLine($"{_portNumber} received ready in {sw.ElapsedMilliseconds}ms");
                sw.Stop();

                if (helloResponse != "READY")
                    throw new Exception("Response not expected!");

                Console.WriteLine($"Received READY on Client {_portNumber}");

                while (true)
                {
                    if (!_queue.TryTake(out var wrap, 1000))
                        continue;

                    client.SendFrame(wrap.Request);
                    wrap.Response = client.ReceiveFrameString();

                    Console.WriteLine($"Worker {_portNumber} has processed Request {wrap.Request}");
                }
            }
        }

        public void StartWorker()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("python3");
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            startInfo.Arguments = $"test.py tcp://127.0.0.1:{_portNumber} {_portNumber}";

            ProcessAsyncHelper.RunAsync(startInfo);
            _consumeQueueTask = Task.Run(ConsumeQueue);
        }
    }

    public class RequestWrapper
    {
        private string _response;
        public string Request { get; set; }
        public ManualResetEventSlim Finished { get; set; }

        public string Response
        {
            get => _response;
            set
            {
                _response = value;
                Finished.Set();
            }
        }
    }
}
