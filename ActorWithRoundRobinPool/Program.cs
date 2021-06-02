using Akka.Actor;
using Akka.Routing;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace ActorWithRoundRobinPool
{
    static class Config
    {
        public const int N = 1000000;
        public const int ActorCount = N;
        public const int MessageCount = N;
    }

    class Program
    {
        static void Main()
        {
            using var system = ActorSystem.Create($"{nameof(Program)}");

            var counterActor = system.ActorOf(
                props: CounterProcessIdActor.CreateProps(), name: $"{nameof(CounterProcessIdActor)}");

            var slowlyActors = system.ActorOf(
                props: SlowlyActor.CreateProps(counterActor)
                .WithRouter(new RoundRobinPool(Config.ActorCount)),
                name: $"{nameof(SlowlyActor)}");

            for (int i = 0; i < Config.MessageCount; i++)
            {
                slowlyActors.Tell(new DoWorkMessage());
            }

            Console.ReadKey();
        }
    }

    class SlowlyActor : ReceiveActor
    {
        private readonly IActorRef CounterDuplicateActor;

        public SlowlyActor(IActorRef counterDuplicateActor)
        {
            CounterDuplicateActor = counterDuplicateActor;

            ReceiveAsync<DoWorkMessage>(async _ =>
            {
                await Task.Delay(1000);
                CounterDuplicateActor.Tell(new DoCountMessage(Thread.CurrentThread.ManagedThreadId));
            });
        }

        public static Props CreateProps(IActorRef counterDuplicateActor) => Props.Create<SlowlyActor>(counterDuplicateActor);
    }

    class CounterProcessIdActor : ReceiveActor
    {
        private int MessageCounter = 0;
        private readonly Dictionary<int, int> ProcessIdCounter = new();

        public CounterProcessIdActor()
        {
            Receive<DoCountMessage>(message =>
            {
                MessageCounter++;

                if (ProcessIdCounter.TryGetValue(message.ProcessId, out int currentCounter))
                {
                    currentCounter++;
                    ProcessIdCounter[message.ProcessId] = currentCounter;
                }
                else
                {
                    ProcessIdCounter.Add(message.ProcessId, 1);
                }
                

                if (MessageCounter == Config.MessageCount)
                {
                    Console.WriteLine("---------------------");
                    foreach ((var processId, var processIdCounter) in ProcessIdCounter)
                    {
                        Console.WriteLine($"ProcessId: {processId} = {processIdCounter}");
                    }
                    Console.WriteLine($"Count processId: {ProcessIdCounter.Count}");
                    MessageCounter = 0;
                    ProcessIdCounter.Clear();
                }
            });
        }

        public static Props CreateProps() => Props.Create<CounterProcessIdActor>();
    }

    record DoCountMessage(int ProcessId);
    record DoWorkMessage();
}
