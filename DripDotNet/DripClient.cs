﻿using Drip.Protocol;
using RestSharp;
using RestSharp.Deserializers;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Drip
{
    public partial class DripClient
    {
        protected const string BatchRequestBodyKey = "batches";

        public const string BaseUrl = "https://api.getdrip.com/v2/";
        public string ApiKey { get; set; }
        public string AccessToken { get; set; }
        public string AccountId { get; set; }

        private RestClient client;
        protected RestClient Client
        {
            get
            {
                if (client == null)
                    client = CreateRestClient();
                return client;
            }
        }

        public DripClient()
        {
        }

        public DripClient(string apiKey = null, string accountId = null, string accessToken = null)
        {
            ApiKey = apiKey;
            AccountId = accountId;
            AccessToken = accessToken;
        }

        protected virtual TResponse GetResource<TResponse>(string resourceUrl, string urlSegmentKey, string urlSegmentValue)
            where TResponse : DripResponse, new()
        {
            var req = new RestRequest(resourceUrl, Method.GET);
            req.AddUrlSegment(urlSegmentKey, urlSegmentValue);
            return Execute<TResponse>(req);
        }

        protected virtual Task<TResponse> GetResourceAsync<TResponse>(string resourceUrl, string urlSegmentKey, string urlSegmentValue, CancellationToken cancellationToken)
            where TResponse : DripResponse, new()
        {
            var req = new RestRequest(resourceUrl, Method.GET);
            req.AddUrlSegment(urlSegmentKey, urlSegmentValue);
            return ExecuteAsync<TResponse>(req, cancellationToken);
        }

        protected virtual TResponse PostResource<TResponse>(string resourceUrl, string requestBodyKey, object requestBody, string urlSegmentKey = null, string urlSegmentValue = null)
            where TResponse : DripResponse, new()
        {
            var req = CreatePostRequest(resourceUrl, requestBodyKey, requestBody, urlSegmentKey, urlSegmentValue);
            return Execute<TResponse>(req);
        }

        protected virtual Task<TResponse> PostResourceAsync<TResponse>(string resourceUrl, string requestBodyKey, object requestBody, CancellationToken cancellationToken, string urlSegmentKey = null, string urlSegmentValue = null)
            where TResponse : DripResponse, new()
        {
            var req = CreatePostRequest(resourceUrl, requestBodyKey, requestBody, urlSegmentKey, urlSegmentValue);
            return ExecuteAsync<TResponse>(req, cancellationToken);
        }

        protected virtual TResponse PostBatchResource<TResponse, TData>(string resourceUrl, string key, TData data)
            where TResponse : DripResponse, new()
        {
            var body = new Dictionary<string, TData>[] { new Dictionary<string, TData> { { key, data } } };
            var req = CreatePostRequest(resourceUrl, BatchRequestBodyKey, body);
            return Execute<TResponse>(req);
        }

        protected virtual Task<TResponse> PostBatchResourceAsync<TResponse, TData>(string resourceUrl, string key, TData data, CancellationToken cancellationToken)
            where TResponse : DripResponse, new()
        {
            var body = new Dictionary<string, TData>[] { new Dictionary<string, TData> { { key, data } } };
            var req = CreatePostRequest(resourceUrl, BatchRequestBodyKey, body);
            return ExecuteAsync<TResponse>(req, cancellationToken);
        }

        protected virtual IRestRequest CreatePostRequest(string resourceUrl, string requestBodyKey = null, object requestBody = null, string urlSegmentKey = null, string urlSegmentValue = null)
        {
            var req = new RestRequest(resourceUrl, Method.POST);
            req.JsonSerializer = new RestSharpLcaseUnderscoreSerializer();

            if (requestBodyKey != null && requestBody != null)
                req.AddJsonBody(new Dictionary<string, object> { { requestBodyKey, requestBody } });
            if (urlSegmentKey != null && urlSegmentValue != null)
                req.AddUrlSegment(urlSegmentKey, urlSegmentValue);
            return req;
        }

        protected virtual TResponse Execute<TResponse>(IRestRequest request)
            where TResponse : DripResponse, new()
        {
            var resp = Client.Execute<TResponse>(request);
            var result = resp.Data ?? new TResponse();
            result.ProcessRestResponse(request, resp);
            return result;
        }

        protected virtual async Task<TResponse> ExecuteAsync<TResponse>(IRestRequest request, CancellationToken cancellationToken)
            where TResponse : DripResponse, new()
        {
            TResponse result;
            try
            {
                var resp = await Client.ExecuteTaskAsync<TResponse>(request, cancellationToken);
                result = resp.Data ?? new TResponse();
                result.ProcessRestResponse(request, resp);
            } 
            catch (SerializationException ex)
            {
                //The RestSharp async interface throws this exception if the content is not json
                //While the sync one simply records it on the resp object...
                //TODO: File bug with RestSharp
                result = new TResponse();
                result.ProcessRestResponse(request, null);
            }

            return result;
        }

        protected virtual RestClient CreateRestClient()
        {
            var client = new RestClient(BaseUrl);
            client.AddDefaultHeader("Content-Type", "application/vnd.api+json");
            client.UserAgent = "Drip DotNet v#" + typeof(DripClient).Assembly.GetName().Version.ToString();
            client.AddDefaultUrlSegment("accountId", AccountId);
            client.AddHandler("application/vnd.api+json", new JsonDeserializer());

            if (string.IsNullOrEmpty(AccessToken))
                client.Authenticator = new HttpBasicAuthenticator(ApiKey, string.Empty);
            else
                client.AddDefaultHeader("Authorization", "Bearer #" + AccessToken);

            return client;
        }
    }
}
