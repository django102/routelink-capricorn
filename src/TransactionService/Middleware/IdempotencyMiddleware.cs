using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using TransactionService.Interfaces;

namespace TransactionService.Middleware
{
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ICacheService _cacheService;

        public IdempotencyMiddleware(RequestDelegate next, ICacheService cacheService)
        {
            _next = next;
            _cacheService = cacheService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == HttpMethods.Post)
            {
                var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
                
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    var cacheKey = $"idempotency_{idempotencyKey}";
                    var cachedResponse = await _cacheService.GetCacheAsync<string>(cacheKey);
                    
                    if (cachedResponse != null)
                    {
                        context.Response.StatusCode = StatusCodes.Status409Conflict;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(cachedResponse);
                        return;
                    }

                    // Capture the response
                    var originalBodyStream = context.Response.Body;
                    using var responseBody = new MemoryStream();
                    context.Response.Body = responseBody;

                    await _next(context);

                    // Cache successful responses
                    if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                    {
                        responseBody.Seek(0, SeekOrigin.Begin);
                        var responseText = await new StreamReader(responseBody).ReadToEndAsync();
                        await _cacheService.SetCacheAsync(cacheKey, responseText, TimeSpan.FromHours(24));

                        responseBody.Seek(0, SeekOrigin.Begin);
                        await responseBody.CopyToAsync(originalBodyStream);
                    }
                    return;
                }
            }

            await _next(context);
        }
    }
}