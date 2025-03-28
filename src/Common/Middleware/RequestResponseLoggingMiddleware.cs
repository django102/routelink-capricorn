using System.Diagnostics;
using System.Text;
using Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly IMongoLogger _mongoLogger;

        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger,
            IMongoLogger mongoLogger)
        {
            _next = next;
            _logger = logger;
            _mongoLogger = mongoLogger;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Log request
            var request = await FormatRequest(context.Request);
            _logger.LogInformation($"Request: {request}");
            await _mongoLogger.LogInformation($"Request: {request}", context.RequestServices.GetService<ServiceConfig>().ServiceName);

            // Copy original response body stream
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Log response
            var response = await FormatResponse(context.Response);
            _logger.LogInformation($"Response: {response}");
            await _mongoLogger.LogInformation($"Response: {response}", context.RequestServices.GetService<ServiceConfig>().ServiceName);

            // Calculate and log duration
            stopwatch.Stop();
            _logger.LogInformation($"Request duration: {stopwatch.ElapsedMilliseconds}ms");
            await _mongoLogger.LogInformation($"Request duration: {stopwatch.ElapsedMilliseconds}ms", 
                context.RequestServices.GetService<ServiceConfig>().ServiceName);

            await responseBody.CopyToAsync(originalBodyStream);
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            request.EnableBuffering();
            var body = request.Body;
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            body.Seek(0, SeekOrigin.Begin);
            request.Body = body;

            return $"{request.Scheme} {request.Host}{request.Path} {request.QueryString} {bodyAsText}";
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            
            return $"Status: {response.StatusCode} - {text}";
        }
    }
}