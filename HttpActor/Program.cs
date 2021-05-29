using System;
using Akka;
using Akka.Actor;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Linq;
using System.IO;

namespace HttpActor
{
    class Program
    {
        static void Main()
        {
            var request = new Uri("home/getContent", UriKind.Relative);
            var httprequestMesage1 = new HttpRequestMessage() { RequestUri = request };
            var httprequestMesage2 = new HttpRequestMessage() { RequestUri = request };
            var httprequestMesage3 = new HttpRequestMessage() { RequestUri = request };

            var system = ActorSystem.Create("http-system");
            var httpclient = system.ActorOf<HttpActor>("httpclient");

            httpclient.Tell(httprequestMesage1);
            httpclient.Tell(httprequestMesage2);
            httpclient.Tell(httprequestMesage3);

            Console.ReadKey();
        }
    }

    class HttpActor : ReceiveActor
    {
        public readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://localhost:5000") };

        public HttpActor()
        {
            Receive<HttpRequestMessage>(message =>
            {
                using var httpResponseMessage = HttpClient.Send(message);
                using var stream = httpResponseMessage.Content.ReadAsStream();
                string content = Encoding.UTF8.GetString((stream as MemoryStream).ToArray());
                Console.WriteLine(content);
            });
        }
    }
}
