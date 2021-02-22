using GraphQL.Server.Ui.Voyager.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#if NETSTANDARD2_0
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace GraphQL.Server.Ui.Voyager
{
    /// <summary>
    /// A middleware for Voyager
    /// </summary>
    public class VoyagerMiddleware
    {
        private readonly GraphQLVoyagerOptions _options;

        /// <summary>
        /// The static file middleware
        /// </summary>
        private readonly StaticFileMiddleware _staticFileMiddleware;

        /// <summary>
        /// The page model used to render Voyager
        /// </summary>
        private VoyagerPageModel _pageModel;

        /// <summary>
        /// Create a new <see cref="VoyagerMiddleware"/>
        /// </summary>
        /// <param name="nextMiddleware">The next middleware</param>
        /// <param name="hostingEnv">Provides information about the web hosting environment an application is running in</param>
        /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of <see cref="ILogger"/> from the registered <see cref="ILoggerProvider"/></param>
        /// <param name="options">Options to customize middleware</param>
        public VoyagerMiddleware(RequestDelegate nextMiddleware, IWebHostEnvironment hostingEnv, ILoggerFactory loggerFactory, GraphQLVoyagerOptions options)
        {
            if (nextMiddleware == null) throw new ArgumentNullException(nameof(nextMiddleware));

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _staticFileMiddleware = CreateStaticFileMiddleware(nextMiddleware, hostingEnv, loggerFactory);
        }

        /// <summary>
        /// Try to execute the logic of the middleware
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        /// <returns></returns>
        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            return IsVoyagerRequest(httpContext.Request)
                ? InvokeVoyager(httpContext.Response)
                : _staticFileMiddleware.Invoke(httpContext);
        }

        private bool IsVoyagerRequest(HttpRequest httpRequest)
            => HttpMethods.IsGet(httpRequest.Method) && httpRequest.Path.StartsWithSegments(_options.Path);

        private Task InvokeVoyager(HttpResponse httpResponse)
        {
            httpResponse.ContentType = "text/html";
            httpResponse.StatusCode = 200;

            // Initialize page model if null
            if (_pageModel == null)
                _pageModel = new VoyagerPageModel(_options);

            byte[] data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpResponse.Body.WriteAsync(data, 0, data.Length);
        }

        private StaticFileMiddleware CreateStaticFileMiddleware(RequestDelegate nextMiddleware, IWebHostEnvironment hostingEnv, ILoggerFactory loggerFactory)
        {
            const string embeddedFileNamespace = "GraphQL.Server.Ui.Voyager.cdn.voyager_ui_dist";

            var staticFileOptions = new StaticFileOptions
            {
                FileProvider = new EmbeddedFileProvider(typeof(VoyagerMiddleware).GetTypeInfo().Assembly, embeddedFileNamespace)
            };

            return new StaticFileMiddleware(nextMiddleware, hostingEnv, Options.Create(staticFileOptions), loggerFactory);
        }
    }
}
