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
                Props
                .Create<SlowlyActor>()
                .WithRouter(new RoundRobinPool(2)
                ), name: $"{nameof(SlowlyActor)}");

            actor.Tell(new DoWork());
            actor.Tell(new DoWork());
            actor.Tell(new DoWork());
            actor.Tell(new DoWork());
            actor.Tell(new DoWork());

            Console.ReadKey();
        }
    }

    class SlowlyActor : ReceiveActor
    {
        public SlowlyActor()
        {
            Receive<DoWork>(_ =>
            {
                Task.Delay(3000).Wait();
                Console.WriteLine("Helo world");
            });
        }
    }

    class DoWork
    {
        public DoWork() { }
    }
}
