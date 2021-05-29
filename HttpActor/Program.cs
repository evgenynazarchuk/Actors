using Akka.Actor;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                    using var httpResponseMessage = HttpClient.SendAsync(message).GetAwaiter().GetResult();
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

        // if content is null then throw exception
        private string _content = string.Empty; 
        private HttpMethod _httpMethod = HttpMethod.Get;

        // If use 2.0 version, then throw exception
        // If use 2.0 need use async method
        private Version _version = new(2, 0); 

        private HttpVersionPolicy _policy = HttpVersionPolicy.RequestVersionOrLower;
        private Dictionary<string, IEnumerable<string>> _headers;

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

        public HttpRequestMessageBuilder UseHttpMethod(string method)
        {
            _httpMethod = new HttpMethod(method);
            return this;
        }

        public HttpRequestMessageBuilder UseVersion(string version)
        {
            _version = new Version(version);
            return this;
        }

        public HttpRequestMessageBuilder UseVersionPolicy(HttpVersionPolicy policy)
        {
            _policy = policy;
            return this;
        }

        public HttpRequestMessageBuilder UseRequestHeaders(Dictionary<string, string> headers)
        {
            foreach ((var key, var value) in headers)
            {
                _headers.Add(key, new List<string> { value });
            }

            return this;
        }

        public HttpRequestMessageBuilder UseRequestHeaders(Dictionary<string, IEnumerable<string>> headers)
        {
            _headers = headers;
            return this;
        }

        public HttpRequestMessage Build()
        {
            var httpResponseMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri(this._requestUri, UriKind.Relative),
                Content = new StringContent(_content),
                Method = _httpMethod,
                Version = _version,
                VersionPolicy = _policy
            };

            if (_headers is not null)
            {
                foreach ((var key, var value) in _headers)
                {
                    httpResponseMessage.Headers.Add(key, value);
                }
            }

            return httpResponseMessage;
        }
    }
}
