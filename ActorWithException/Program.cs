using System;
using Akka;
using Akka.Actor;

namespace ActorWithException
{
    class Program
    {
        static void Main()
        {
            //using var actorSystem = ActorSystem.Create($"{nameof(Program)}");
            //var targetActor = actorSystem.ActorOf<TargetActor>($"{nameof(TargetActor)}");
            //var forwardActor = actorSystem.ActorOf(Props.Create(typeof(ActorWithForward), targetActor),$"{nameof(ActorWithForward)}");
        }
    }
        
}
