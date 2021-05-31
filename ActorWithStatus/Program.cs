using Akka.Actor;
using System;

namespace ActorWithStatus
{
    class Program
    {
        static void Main()
        {
            using var systemActor = ActorSystem.Create("my-system");
            var actorProps = Props.Create<ActorWithStatus>();
            var actor = systemActor.ActorOf(actorProps);

            actor.Tell("1");
            actor.Tell("2");
            actor.Tell("3");
            actor.Tell("4");

            Console.ReadKey();
        }
    }

    class ActorWithStatus : ReceiveActor
    {
        public ActorWithStatus()
        {
            Become(First);
        }

        void First()
        {
            Receive<string>(msg =>
            {
                Console.WriteLine($"First: {msg}");
                Become(Second);
            });
        }

        void Second()
        {
            Receive<string>(msg =>
            {
                Console.WriteLine($"Second: {msg}");
                Become(First);
            });
        }
    }
}
