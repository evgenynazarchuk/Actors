using Akka.Actor;
using System;

namespace ActorWithException
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

            var actorProps = Props.Create<ActorWithForward>().WithSupervisorStrategy(strategy);
            var actor = actorSystem.ActorOf(actorProps, $"{nameof(ActorWithForward)}");

            actor.Tell("Evgeny");
            actor.Tell("Evgeny");
            actor.Tell("");
            actor.Tell("Evgeny");
            actor.Tell("Evgeny");

            Console.ReadKey();
        }
    }

    class ActorWithForward : ReceiveActor
    {
        public ActorWithForward()
        {
            var strategy = new OneForOneStrategy(
                    maxNrOfRetries: 1,
                    withinTimeRange: TimeSpan.FromMinutes(1),
                    e => Directive.Stop);

            var childProps = Props.Create<ActorWithException>().WithSupervisorStrategy(strategy);
            var child = Context.ActorOf(childProps, $"{nameof(ActorWithException)}");

            Receive<string>(msg =>
            {
                child.Tell(msg);
            });
        }
    }

    class ActorWithException : ReceiveActor
    {
        public ActorWithException()
        {
            Receive<string>(name =>
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new Exception();
                }

                Console.WriteLine($"Hello, {name}");
            });
        }
    }
}
