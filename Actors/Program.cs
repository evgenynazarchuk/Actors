using Akka.Actor;
using System;

namespace Actors
{
    class Program
    {
        static void Main()
        {
            using var actorSystem = ActorSystem.Create("My-System");
            var actor = actorSystem.ActorOf<MessageActor>("My-Actor");

            for (int i = 0; i < 10000; i++)
            {
                actor.Tell(new InformationMessage("Hello world\n"));
            }

            Console.ReadKey();
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

    class InformationMessage
    {
        public readonly string _text;
        public string Text { get => _text; }

        public InformationMessage(string messageText)
        {
            _text = messageText;
        }
    }
}
