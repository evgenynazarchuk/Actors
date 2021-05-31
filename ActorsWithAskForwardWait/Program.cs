using Akka.Actor;
using System;
using System.Threading.Tasks;

namespace ActorsWithAskForwardWait
{
    class Program
    {
        static void Main()
        {
            using var system = ActorSystem.Create($"{nameof(Program)}");
            var targetActor = system.ActorOf<TargetActorWithWait>($"{nameof(TargetActorWithWait)}");
            var forwardActor = system.ActorOf(Props.Create(typeof(ActorWithForward), targetActor), $"{nameof(ActorWithForward)}");

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

    class TargetActorWithWait : ReceiveActor
    {
        public TargetActorWithWait()
        {
            Receive<string>(msg =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    return $"{msg}!";
                }).PipeTo(Sender);
            });

            //Receive<string>(msg =>
            //{
            //    Task.Factory.StartNew(new Action<object>((sender) =>
            //    {
            //        Task.Delay(1000).GetAwaiter().GetResult();
            //        (sender as IActorRef).Tell($"{msg}!");
            //    }), Sender);
            //});
        }
    }
}
