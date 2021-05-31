using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ManyActors
{
    class Program
    {
        static void Main(string[] args)
        {
            const int n = 100_000;

            var actorSystem = ActorSystem.Create($"{nameof(Program)}");

            var actorProps = Props.Create<LongWorkActor>().WithDispatcher("akka.io.pinned-dispatcher");
            var actor = actorSystem.ActorOf(actorProps, name: $"{nameof(Program)}");

            var tasks = new List<Task<EmptyCompletedMessage>>();

            Stopwatch watch = new();

            watch.Start();
            for (int i = 0; i < n; i++)
            {
                tasks.Add(actor.Ask<EmptyCompletedMessage>(new EmptyMessage()));
            }
            watch.Stop();
            Console.WriteLine($"Ask time: {watch.ElapsedMilliseconds} ms");

            watch.Restart();
            Task.WaitAll(tasks.ToArray());
            watch.Stop();
            Console.WriteLine($"Wait time: {watch.ElapsedMilliseconds} ms"); ;
        }
    }

    class LongWorkActor : ReceiveActor
    {
        public LongWorkActor()
        {
            Receive<EmptyMessage>(message =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    return new EmptyCompletedMessage();
                }).PipeTo(Sender);
            });
        }
    }

    class EmptyMessage
    { }

    class EmptyCompletedMessage
    { }

}
