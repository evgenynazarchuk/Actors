using Akka.Actor;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace HttpActor
{
    class Program
    {
        static void Main()
        {
            var httpResponseBuilder = new HttpRequestMessageBuilder();
            httpResponseBuilder.UseRequest("home/getContent");

            var httprequestMesage1 = httpResponseBuilder.Build();
            var httprequestMesage2 = httpResponseBuilder.Build();
            var httprequestMesage3 = httpResponseBuilder.Build();

            using var system = ActorSystem.Create("http-system");
            var httpActorProps = Props.Create<HttpActor>();
            var httpActor = system.ActorOf(httpActorProps, "httpclient");

            var taskList = new List<Task<string>>()
            {
                httpActor.Ask<string>(httprequestMesage1),
                httpActor.Ask<string>(httprequestMesage2),
                httpActor.Ask<string>(httprequestMesage3)
            };
            Task.WaitAll(taskList.ToArray());

            taskList.ForEach(task => Console.WriteLine(task.Result));
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
                Task.Run(() =>
                {
                    using var httpResponseMessage = HttpClient.Send(message);
                    using var stream = httpResponseMessage.Content.ReadAsStream();
                    string content = Encoding.UTF8.GetString((stream as MemoryStream).ToArray());
                    return content;
                }).PipeTo(Sender);
            });
        }
    }

    class HttpRequestMessageBuilder
    {
        private string _requestUri;
        private string _content = string.Empty; // Empty is required if content is null

        public HttpRequestMessageBuilder()
        {
        }

        public HttpRequestMessageBuilder UseRequest(string requestUri)
        {
            _requestUri = requestUri;
            return this;
        }

        public HttpRequestMessageBuilder UseContent(string content)
        {
            _content = content;
            return this;
        }

        public HttpRequestMessage Build()
        {
            return new HttpRequestMessage()
            {
                RequestUri = new Uri(this._requestUri, UriKind.Relative),
                Content = new StringContent(_content)
            };
        }
    }

}
