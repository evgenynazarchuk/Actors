using Akka.Actor;
using System;
using System.Diagnostics;

namespace Actors
{
    class Program
    {
        static void Main()
        {
            var stopWatch = new Stopwatch();
            using var system = ActorSystem.Create("my-system");
            var actor = system.ActorOf<MessageActor>("my-actor");

            stopWatch.Start();
            for (int i = 0; i < 100000; i++)
            {
                actor.Tell(new InformationMessage("Hello world\n"));
            }
            stopWatch.Stop();

            Console.ReadKey();
            Console.WriteLine($"Watch: {stopWatch.ElapsedMilliseconds} ms");
            Console.ReadKey();
        }
    }

    class InformationMessage
    {
        public readonly string _text;
        public string Text { get => _text; }

        public InformationMessage(string messageText)
        {
            _text = messageText;
        }
    }

    class MessageActor : ReceiveActor
    {
        public MessageActor()
        {
            Receive<InformationMessage>(message =>
            {
                Console.Write(message.Text);
            });
        }
    }
}
