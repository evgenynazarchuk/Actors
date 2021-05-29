using Akka.Actor;
using System;

namespace ActorWithForward
{
    class Program
    {
        static void Main()
        {
            using var actorSystem = ActorSystem.Create($"{nameof(Program)}");
            var targetActor = actorSystem.ActorOf<TargetActor>($"{nameof(TargetActor)}");
            var forwardActor = actorSystem.ActorOf(Props.Create(typeof(ActorWithForward), targetActor), $"{nameof(ActorWithForward)}");

            var resultTask = forwardActor.Ask("Evgeny");
            var result = resultTask.GetAwaiter().GetResult();
            Console.WriteLine(result);
            Console.ReadKey();
        }
    }

    class ActorWithForward : ReceiveActor
    {
        public ActorWithForward(IActorRef target)
        {
            Receive<string>(name =>
            {
                target.Forward($"Hello, {name}");
            });
        }
    }

    class TargetActor : ReceiveActor
    {
        public TargetActor()
        {
            Receive<string>(msg =>
            {
                Sender.Tell($"{msg}!");
            });
        }
    }
}
