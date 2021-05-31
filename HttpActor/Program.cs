using Akka.Actor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpActor
{
    class Program
    {
        static void Main()
        {
            //
            var httpResponseBuilder = new HttpRequestMessageBuilder();
            httpResponseBuilder.UseRequest("home/getContent")
                .UseVersionPolicy(HttpVersionPolicy.RequestVersionOrLower);
            var httprequestMesage1 = httpResponseBuilder.Build();
            var httprequestMesage2 = httpResponseBuilder.Build();
            var httprequestMesage3 = httpResponseBuilder.Build();

            //
            using var system = ActorSystem.Create("http-system");
            var actorStrategy = new OneForOneStrategy(1, TimeSpan.FromMinutes(1), e => Directive.Restart);
            var httpActorProps = Props.Create<HttpActor>().WithSupervisorStrategy(actorStrategy);
            var httpActor = system.ActorOf(httpActorProps, "httpclient");

            //
            var taskList = new List<Task<HttpResult>>()
            {
                httpActor.Ask<HttpResult>(httprequestMesage1),
                httpActor.Ask<HttpResult>(httprequestMesage2),
                httpActor.Ask<HttpResult>(httprequestMesage3)
            };
            Task.WaitAll(taskList.ToArray());

            //
            taskList.ForEach(task =>
            {
                if (task.Result.Success)
                {
                    Console.WriteLine(task.Result.HttpResponseMessage.Content.GetContent());
                }
                else
                {
                    Console.WriteLine(task.Result.Error);
                }
            });
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
                Task.Run(async () =>
                {
                    HttpResponseMessage httpResponseMessage = null;

                    httpResponseMessage = await HttpClient.SendAsync(message);

                    return new HttpResult(httpResponseMessage);

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

    class HttpResult
    {
        public readonly HttpResponseMessage HttpResponseMessage = null;
        public readonly string Error = null;
        public bool Success { get => Error is null; }
        public bool Failure { get => Error is not null; }

        public HttpResult(HttpResponseMessage httpResponseMessage)
        {
            HttpResponseMessage = httpResponseMessage;
        }
        public HttpResult(string error)
        {
            Error = error;
        }
    }

    static class HttpContentExtenstion
    {
        public static string GetContent(this HttpContent httpContent)
        {
            using var stream = httpContent.ReadAsStream();
            string content = Encoding.UTF8.GetString((stream as MemoryStream).ToArray());
            return content;
        }
    }
}
