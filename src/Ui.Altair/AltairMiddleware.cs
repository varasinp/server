using GraphQL.Server.Ui.Altair.Internal;
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

namespace GraphQL.Server.Ui.Altair
{
    /// <summary>
    /// A middleware for Altair GraphQL
    /// </summary>
    public class AltairMiddleware
    {
        private readonly GraphQLAltairOptions _options;

        /// <summary>
        /// The static file middleware
        /// </summary>
        private readonly StaticFileMiddleware _staticFileMiddleware;

        /// <summary>
        /// The page model used to render Altair
        /// </summary>
        private AltairPageModel _pageModel;

        /// <summary>
        /// Create a new <see cref="AltairMiddleware"/>
        /// </summary>
        /// <param name="nextMiddleware">The next middleware</param>
        /// <param name="hostingEnv">Provides information about the web hosting environment an application is running in</param>
        /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of <see cref="ILogger"/> from the registered <see cref="ILoggerProvider"/></param>
        /// <param name="options">Options to customize middleware</param>
        public AltairMiddleware(RequestDelegate nextMiddleware, IWebHostEnvironment hostingEnv, ILoggerFactory loggerFactory, GraphQLAltairOptions options)
        {
            if (nextMiddleware == null) throw new ArgumentNullException(nameof(nextMiddleware));

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _staticFileMiddleware = CreateStaticFileMiddleware(nextMiddleware, hostingEnv, loggerFactory);
        }

        /// <summary>
        /// Try to execute the logic of the middleware
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            return IsAltairRequest(httpContext.Request)
                ? InvokeAltair(httpContext.Response)
                : _staticFileMiddleware.Invoke(httpContext);
        }

        private bool IsAltairRequest(HttpRequest httpRequest)
            => HttpMethods.IsGet(httpRequest.Method) && httpRequest.Path.StartsWithSegments(_options.Path);

        private Task InvokeAltair(HttpResponse httpResponse)
        {
            httpResponse.ContentType = "text/html";
            httpResponse.StatusCode = 200;

            // Initialize page model if null
            if (_pageModel == null)
                _pageModel = new AltairPageModel(_options);

            byte[] data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpResponse.Body.WriteAsync(data, 0, data.Length);
        }

        private StaticFileMiddleware CreateStaticFileMiddleware(RequestDelegate nextMiddleware, IWebHostEnvironment hostingEnv, ILoggerFactory loggerFactory)
        {
            const string embeddedFileNamespace = "GraphQL.Server.Ui.Altair.cdn.altair_ui_dist";

            var staticFileOptions = new StaticFileOptions
            {
                FileProvider = new EmbeddedFileProvider(typeof(AltairMiddleware).GetTypeInfo().Assembly, embeddedFileNamespace)
            };

            return new StaticFileMiddleware(nextMiddleware, hostingEnv, Options.Create(staticFileOptions), loggerFactory);
        }
    }
}
