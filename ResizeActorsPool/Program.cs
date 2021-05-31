using System;
using Akka.Actor;
using Akka.Routing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace ResizeActorsPool
{
    static class ActorConfiguration
    {
        public const int N = 100;
    }

    class Program
    {
        static void Main()
        {
            var actorSystem = ActorSystem.Create(name: $"{nameof(Program)}");

            var consoleProps = ConsoleActor.CreateProps();
            var consoleActor = actorSystem.ActorOf(consoleProps, $"{nameof(ConsoleActor)}");

            var counterProps = CounterActor.CreateProps();
            var counterActor = actorSystem.ActorOf(consoleProps, $"{nameof(CounterActor)}");

            var requestRouterProps = RequestRouterActor.CreateProps();
            var requestRouterActor = actorSystem.ActorOf(requestRouterProps, $"{nameof(RequestRouterActor)}");

            var userProps = UserActor.CreateProps(requestRouterActor)
                .WithRouter(new RoundRobinPool(ActorConfiguration.N));
            var userActor = actorSystem.ActorOf(userProps, $"{nameof(UserActor)}");

            Stopwatch watch = new();
            watch.Start();
            userActor.Tell(new StartInvokeMessage());
            watch.Stop();
            Console.WriteLine($"Tell time: {watch.ElapsedMilliseconds} ms");
            watch.Restart();

            Console.ReadKey();
        }
    }


    class UserActor : ReceiveActor
    {
        private readonly IActorRef RequestRouterActor;

        public UserActor(IActorRef requestRouterActor)
        {
            RequestRouterActor = requestRouterActor;

            Start();
        }

        public void Start()
        {
            Receive<StartInvokeMessage>(message =>
            {
                Task.Run(async () =>
                {
                    await InvokeAsync();
                });
            });
        }

        public async Task InvokeAsync()
        {
            await RequestAsync();
        }

        public async Task RequestAsync()
        {
            await RequestRouterActor.Ask<ResponseMessage>(new RequestMessage());
        }

        public static Props CreateProps(IActorRef requestHub) => Props.Create<UserActor>(requestHub);
    }

    class RequestRouterActor : ReceiveActor
    {
        private readonly IActorRef RequestActor;

        public RequestRouterActor()
        {
            var requestActorProps = ResizeActorsPool.RequestActor.CreateProps()
                .WithRouter(new RoundRobinPool(ActorConfiguration.N, new DefaultResizer(1, ActorConfiguration.N)));
            this.RequestActor = Context.ActorOf(requestActorProps);

            Start();
        }

        public void Start()
        {
            Receive<RequestMessage>(message =>
            {
                RequestActor.Forward(message);
            });
        }

        public static Props CreateProps() => Props.Create<RequestRouterActor>();
    }

    class RequestActor : ReceiveActor
    {
        public readonly IActorRef CounterActor;

        public RequestActor()
        {
            CounterActor = Context.ActorSelection($"{nameof(CounterActor)}").Anchor;
            Start();
        }

        public void Start()
        {
            ReceiveAsync<RequestMessage>(async _ =>
            {
                await Task.Delay(1000);
                Sender.Tell(new ResponseMessage());
                CounterActor.Tell(new RequestCompletedMessage());
            });
        }

        public static Props CreateProps() => Props.Create<RequestActor>();
    }

    class CounterActor : ReceiveActor
    {
        private const int TargetReceivedMessageCount = ActorConfiguration.N;
        private readonly IActorRef ConsoleActor;
        private int ReceivedMessageCount = 0;
        private DateTime TimeFirstMessage;
        private DateTime TimeLastMessage;

        public CounterActor(IActorRef consoleActor)
        {
            ConsoleActor = Context.ActorSelection($"{nameof(ConsoleActor)}").Anchor;
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
                    ConsoleActor.Tell(new CounterMessage(TimeFirstMessage, TimeLastMessage, ReceivedMessageCount));
                    ReceivedMessageCount = 0;
                    Become(StartCount);
                }
            });
        }

        public static Props CreateProps() => Props.Create<CounterActor>();
    }

    class ConsoleActor : ReceiveActor
    {
        public ConsoleActor()
        {
            Receive<CounterMessage>(message =>
            {
                Console.WriteLine($"First time message: {message.TimeFirstMessage}\n" +
                    $"Last time message:{message.TimeLastMessage}\n" +
                    $"Diff: {(message.TimeLastMessage - message.TimeFirstMessage).TotalMilliseconds} ms\n" +
                    $"Counter: {message.Counter}");
            });
        }

        public static Props CreateProps() => Props.Create<ConsoleActor>();
    }

    class RequestMessage { }
    class ResponseMessage { }
    class RequestCompletedMessage { }
    class StartInvokeMessage { }
    class CounterMessage
    {
        public readonly DateTime TimeFirstMessage;
        public readonly DateTime TimeLastMessage;
        public readonly int Counter;

        public CounterMessage(DateTime timeFirstMessage, DateTime timeLastMessage, int counter)
        {
            TimeFirstMessage = timeFirstMessage;
            TimeLastMessage = timeLastMessage;
            Counter = counter;
        }
    }
}
