using Akka.Actor;
using System;
using Akka.Pattern;

namespace ActorWithException
{
    class Program
    {
        static void Main()
        {
            using var actorSystem = ActorSystem.Create($"{nameof(Program)}");
            //var actor = actorSystem.ActorOf<ActorWithException>($"{nameof(ActorWithException)}");
            var invoker = actorSystem.ActorOf<ActorWithForward>($"{nameof(ActorWithForward)}");

            //var invoker = actorSystem.ActorOf(
            //    Props
            //    .Create<ActorWithForward>(actor),
            //    $"{nameof(ActorWithForward)}"
            //    );


            for (int i = 0; i < 10; i++)
            {
                invoker.Tell("");
                invoker.Tell("Evgeny");
            }

            Console.ReadKey();
        }
    }

    class ActorWithForward : ReceiveActor
    {
        public ActorWithForward(/*IActorRef target*/)
        {
            Receive<string>(msg =>
            {
                var child = Context.ActorOf<ActorWithException>();
                child.Tell(msg);
                //target.Tell(msg);
            });
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 1,
                withinTimeRange: TimeSpan.FromMinutes(1),
                e => Directive.Stop);
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

        //protected override SupervisorStrategy SupervisorStrategy()
        //{
        //    return new OneForOneStrategy(
        //        maxNrOfRetries: 1,
        //        withinTimeRange: TimeSpan.FromMinutes(1),
        //        e => Akka.Actor.SupervisorStrategy.DefaultStrategy.Decider.Decide(e));
        //}
    }
}
