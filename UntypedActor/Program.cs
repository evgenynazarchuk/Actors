using Akka.Actor;
using Akka.Routing;
using System;

namespace ActorWithRoundRobinException
{
    class Program
    {
        static void Main()
        {
            using var actorSystem = ActorSystem.Create($"{nameof(Program)}");

            var strategy = new OneForOneStrategy(
                    maxNrOfRetries: 1,
                    withinTimeRange: TimeSpan.FromMinutes(1),
                    e => Directive.Stop);

            var actor = actorSystem.ActorOf(
                Props
                .Create<ActorWithException>()
                .WithRouter(new RoundRobinPool(2)
                .WithSupervisorStrategy(strategy)
                ), name: $"{nameof(ActorWithException)}");

            actor.Tell("Evgeny");
            actor.Tell("");
            actor.Tell("Evgeny");
            actor.Tell("");
            actor.Tell("");
            actor.Tell("Evgeny");
            actor.Tell("Evgeny");
            actor.Tell("Evgeny");
            actor.Tell("Evgeny");

            Console.ReadKey();
        }
    }

    class ActorWithException : ReceiveActor
    {
        public ActorWithException()
        {
            Receive<string>(message =>
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    throw new Exception();
                }

                Console.WriteLine(message);
            });
        }
    }
}
