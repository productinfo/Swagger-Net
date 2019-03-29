﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;

namespace Swagger.Net.Tests
{
    [TestFixture]
    public abstract class HttpMessageHandlerTestBase<THandler>
        where THandler : HttpMessageHandler
    {
        public struct TEMP_URI
        {
            public const string DOCS = "http://tempuri.org/swagger/docs/v1";
            public const string INDEX = "http://tempuri.org/swagger/ui/index";
        }

        private string _routeTemplate;

        protected HttpMessageHandlerTestBase(string routeTemplate)
        {
            _routeTemplate = routeTemplate;
        }

        protected HttpConfiguration Configuration { get; set; }

        protected THandler Handler { get; set; }

        [SetUp]
        public void BaseSetUp()
        {
            Configuration = new HttpConfiguration();
        }

        protected void SetUpDefaultRoutesFor(IEnumerable<Type> controllerTypes)
        {
            foreach (var type in controllerTypes)
            {
                var controllerName = type.Name.ToLower().Replace("controller", String.Empty);
                var route = new HttpRoute(
                    String.Format("{0}/{{id}}", controllerName),
                    new HttpRouteValueDictionary(new { controller = controllerName, id = RouteParameter.Optional }));
                Configuration.Routes.Add(controllerName, route);
            }
        }

        protected void SetUpDefaultRouteFor<TController>()
            where TController : ApiController
        {
            SetUpDefaultRoutesFor(new[] { typeof(TController) });
        }

        protected void SetUpCustomRouteFor<TController>(string routeTemplate)
            where TController : ApiController
        {
            var controllerName = typeof(TController).Name.ToLower().Replace("controller", String.Empty);
            var route = new HttpRoute(
                routeTemplate,
                new HttpRouteValueDictionary(new { controller = controllerName, id = RouteParameter.Optional }));
            Configuration.Routes.Add(controllerName, route);
        }

        protected void SetUpAttributeRoutesFrom(Assembly assembly)
        {
            // assembly isn't used but requiring it ensures that it's loaded and, therefore, scanned for attribute routes
            Configuration.MapHttpAttributeRoutes();
            Configuration.EnsureInitialized();
        }

        protected HttpResponseMessage Get(string uri, DateTimeOffset? IfModifiedSince = null)
        {
            if (Handler == null)
                throw new InvalidOperationException("Handler must be set by fixture subclass");

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = Configuration;
            request.Headers.IfModifiedSince = IfModifiedSince;

            var route = new HttpRoute(_routeTemplate);
            var routeData = route.GetRouteData("/", request) ?? new HttpRouteData(route);

            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = routeData;

            return new HttpMessageInvoker(Handler)
                .SendAsync(request, new CancellationToken(false))
                .Result;
        }

        protected TContent GetContent<TContent>(string uri)
        {
            var responseMessage = Get(uri);
            return responseMessage.Content.ReadAsAsync<TContent>().Result;
        }

        protected HttpResponseHeaders GetHeaders(string uri)
        {
            var responseMessage = Get(uri);
            return responseMessage.Headers;
        }

        protected string GetContentAsString(string uri)
        {
            try
            {
                var responseMessage = Get(uri);
                return responseMessage.Content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}