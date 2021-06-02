using System;
using Akka.Actor;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace FileWriteActor
{
    class Program
    {
        static void Main()
        {
            Stopwatch watch = new();

            var system = ActorSystem.Create($"{nameof(Program)}");
            var actor = system.ActorOf(Props.Create<FileWriteActor>());

            watch.Start();
            for (int i = 0; i < 1_000_000; i++)
            {
                actor.Tell(new WriteStringMessage("Hello world! Hello world! Hello world!"));
            }
            watch.Stop();
            Console.WriteLine($"Call time: {watch.ElapsedMilliseconds} ms");

            Console.ReadKey();
            actor.Tell(new CloseFileMessage());
            Console.ReadKey();
        }
    }

    class FileWriteActor : ReceiveActor
    {
        private readonly StreamWriter fileStream = new("result.txt", false, Encoding.UTF8, 65535);

        public FileWriteActor()
        {
            Receive<WriteStringMessage>(message =>
            {
                fileStream.WriteLine(message.Text);
            });

            Receive<CloseFileMessage>(_ =>
            {
                fileStream.Flush();
                fileStream.Close();
            });
        }
    }

    record CloseFileMessage();
    record WriteStringMessage(string Text);
}
