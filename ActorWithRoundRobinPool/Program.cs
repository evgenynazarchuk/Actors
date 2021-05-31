using Akka.Actor;
using Akka.Routing;
using System;
using System.Threading.Tasks;

namespace ActorWithRoundRobinPool
{
    class Program
    {
        static void Main()
        {
            using var actorSystem = ActorSystem.Create($"{nameof(Program)}");
            var actor = actorSystem.ActorOf(
                props: Props
                .Create<SlowlyActor>()
                .WithRouter(new RoundRobinPool(2)),
                name: $"{nameof(SlowlyActor)}");

            for (int i = 0; i < 5; i++)
            {
                actor.Tell(new DoWork());
            }

            Console.ReadKey();
        }
    }

    class SlowlyActor : ReceiveActor
    {
        public SlowlyActor()
        {
            Receive<DoWork>(async _ =>
            {
                await Task.Delay(3000);
                Console.WriteLine("Helo world");
            });
        }
    }

    class DoWork
    {
        public DoWork() { }
    }
}
