using Akka.Actor;
using Akka.Routing;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ManyActors
{
    static class Configuration
    {
        public const int N = 50_000;
        public const int TargetMessageCount = N;
        public const int UserActorCount = N;
    }

    class Program
    {
        static void Main()
        {
            var actorSystem = ActorSystem.Create(name: $"{nameof(Program)}");

            var consoleProps = ConsoleWriterActor.CreateProps();
            var consoleActor = actorSystem.ActorOf(consoleProps, $"{nameof(ConsoleWriterActor)}");

            var watcherProps = CounterMessageActor.CreateProps(consoleActor);
            var watcherActor = actorSystem.ActorOf(watcherProps, $"{nameof(CounterMessageActor)}");

            var requestProps = RequestActor.CreateProps(watcherActor);
            //.WithDispatcher("akka.io.pinned-dispatcher");
            var requestActor = actorSystem.ActorOf(requestProps, $"{nameof(RequestActor)}");

            //var userProps = UserActor.CreateProps(requestActor)
            //    .WithRouter(new RoundRobinPool(Configuration.UserActorCount));
            var userProps = UserActor.CreateProps(requestActor)
                .WithRouter(new BroadcastPool(Configuration.UserActorCount));
            //var userProps = UserActor.CreateProps(requestActor);
            var userActor = actorSystem.ActorOf(userProps, $"{nameof(UserActor)}");

            Stopwatch watch = new();

            // roundrobin
            //watch.Start();
            //for (int j = 0; j < Configuration.UserActorCount; j++)
            //{
            //    userActor.Tell(new StartInvoke());
            //}
            //watch.Stop();
            //Console.WriteLine($"Tell time: {watch.ElapsedMilliseconds} ms");
            //watch.Restart();

            // broadcast
            for (int i = 0; i < 5; i++)
            {
                watch.Start();
                userActor.Tell(new StartInvoke());
                watch.Stop();
                Console.WriteLine($"Tell time: {watch.ElapsedMilliseconds} ms");
                watch.Restart();
            }

            Console.ReadKey();
        }
    }

    class UserActor : ReceiveActor
    {
        private readonly IActorRef RequestActor;

        public UserActor(IActorRef requestActor)
        {
            RequestActor = requestActor;

            Receive<StartInvoke>(message =>
            {
                Task.Run(async () =>
                {
                    await InvokeAsync();
                });
            });
        }

        public virtual async Task InvokeAsync()
        {
            await RequestAsync();
        }

        public async Task<EmptyResponseMessage> RequestAsync()
        {
            var response = await RequestActor.Ask<EmptyResponseMessage>(new EmptyRequestMessage());
            return response;
        }

        public static Props CreateProps(IActorRef requestActor) => Props.Create<UserActor>(requestActor);
    }

    class RequestActor : ReceiveActor
    {
        public RequestActor(IActorRef counterActor)
        {
            Receive<EmptyRequestMessage>(message =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    counterActor.Tell(new RequestCompletedMessage());
                    return new EmptyResponseMessage();
                }).PipeTo(Sender);
            });
        }

        public static Props CreateProps(IActorRef watcherActor) => Props.Create<RequestActor>(watcherActor);
    }

    class CounterMessageActor : ReceiveActor
    {
        private const int TargetReceivedMessageCount = Configuration.TargetMessageCount;
        private readonly IActorRef ConsoleActor;
        private int ReceivedMessageCount = 0;
        private DateTime TimeFirstMessage;
        private DateTime TimeLastMessage;

        public CounterMessageActor(IActorRef consoleActor)
        {
            ConsoleActor = consoleActor;
            StartCount();
        }

        public void StartCount()
        {
            Receive<RequestCompletedMessage>(_ =>
            {
                ReceivedMessageCount++;
                TimeFirstMessage = DateTime.Now;
                Become(FinishCount);
            });
        }

        public void FinishCount()
        {
            Receive<RequestCompletedMessage>(_ =>
            {
                ReceivedMessageCount++;

                if (ReceivedMessageCount == TargetReceivedMessageCount)
                {
                    TimeLastMessage = DateTime.Now;
                    ConsoleActor.Tell(new WatcherResponseMessage(TimeFirstMessage, TimeLastMessage, ReceivedMessageCount));
                    ReceivedMessageCount = 0;
                    Become(StartCount);
                }
            });
        }

        public static Props CreateProps(IActorRef consoleActor) => Props.Create<CounterMessageActor>(consoleActor);
    }

    class ConsoleWriterActor : ReceiveActor
    {
        public ConsoleWriterActor()
        {
            Receive<WatcherResponseMessage>(message =>
            {
                Console.WriteLine($"First time message: {message.TimeFirstMessage}\n" +
                    $"Last time message:{message.TimeLastMessage}\n" +
                    $"Diff: {(message.TimeLastMessage - message.TimeFirstMessage).TotalMilliseconds} ms\n" +
                    $"Counter: {message.Counter}");
            });
        }

        public static Props CreateProps() => Props.Create<ConsoleWriterActor>();
    }

    class StartInvoke
    {
    }

    class EmptyRequestMessage
    {
    }

    class EmptyResponseMessage
    {
    }

    class RequestCompletedMessage
    { }

    class WatcherResponseMessage
    {
        public readonly DateTime TimeFirstMessage;
        public readonly DateTime TimeLastMessage;
        public readonly int Counter;

        public WatcherResponseMessage(DateTime timeFirstMessage, DateTime timeLastMessage, int counter)
        {
            TimeFirstMessage = timeFirstMessage;
            TimeLastMessage = timeLastMessage;
            Counter = counter;
        }
    }
}
