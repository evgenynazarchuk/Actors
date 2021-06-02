using System;
using Akka.Actor;
using Akka.Routing;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CreatedTimeActor
{
    class Program
    {
        static void Main()
        {
            var system = ActorSystem.Create($"{nameof(Program)}");
            var httpclient = system.ActorOf(HttClient.CreateProps());
            var users = system.ActorOf(User.CreateProps(httpclient).WithRouter(new RoundRobinPool(50_000)));

            Stopwatch watch = new();

            System.Timers.Timer timer = new(1000);
            timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                watch.Start();
                for (int i = 0; i < 50_000; i++)
                {
                    users.Tell(new StartInvokeMessage());
                }
                watch.Stop();
                Console.WriteLine($"Time users tell: {watch.ElapsedMilliseconds} ms");

                if (TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds) > TimeSpan.FromSeconds(1))
                {
                    Console.Beep();
                }
                watch.Reset();
            };

            timer.Start();

            Console.ReadKey();
        }
    }

    class User : ReceiveActor
    {
        public User(IActorRef httpClient)
        {
            Receive<StartInvokeMessage>(_ =>
            {
                httpClient.Ask(new RequestMessage());
                httpClient.Ask(new RequestMessage());
                httpClient.Ask(new RequestMessage());
                httpClient.Ask(new RequestMessage());
                httpClient.Ask(new RequestMessage());
            });
        }

        public static Props CreateProps(IActorRef httpClient) => Props.Create<User>(httpClient);
    }

    class HttClient : ReceiveActor
    {
        public HttClient()
        {
            //var request = Context.ActorOf(Request.CreateProps().WithRouter(new RoundRobinPool(100000)));
            Receive<RequestMessage>(message =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    return new ResponseMessage();
                }).PipeTo(Sender);
            });
        }

        public static Props CreateProps() => Props.Create<HttClient>();
    }

    class Request : ReceiveActor
    {
        public Request()
        {
            //ReceiveAsync<RequestMessage>(async _ =>
            //{
            //    await Task.Delay(1000);
            //    Sender.Tell(new ResponseMessage());
            //});

            Receive<RequestMessage>(_ =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    return new ResponseMessage();
                }).PipeTo(Sender);
            });
        }

        public static Props CreateProps() => Props.Create<Request>();
    }

    record StartInvokeMessage();
    record RequestMessage();
    record ResponseMessage();
}
