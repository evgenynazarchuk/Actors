using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActorWithWait
{
    class Program
    {
        static void Main()
        {
            var actorSystem = ActorSystem.Create($"{nameof(Program)}");
            var actor = actorSystem.ActorOf<ActorWithWait>($"{nameof(ActorWithWait)}");

            List<Task<string>> tasks = new();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(actor.Ask<string>(new DoWork()));
            }
            Task.WaitAll(tasks.ToArray());

            foreach (var task in tasks)
            {
                Console.WriteLine(task.Result);
            }

            Console.ReadKey();
        }
    }

    class DoWork
    {
        public DoWork()
        {
        }
    }

    class ActorWithWait : ReceiveActor
    {
        public ActorWithWait()
        {
            Receive<DoWork>(_ =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    return "Hello world";
                }).PipeTo(Sender);
            });
        }
    }
}
