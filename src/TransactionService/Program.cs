using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;


namespace TransactionService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Add Redis
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));
            builder.Services.AddSingleton<ICacheService, RedisCacheService>();

            // Configure Swagger with JWT support
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Transaction Service API",
                    Version = "v1",
                    Description = "API for handling financial transactions",
                    Contact = new OpenApiContact
                    {
                        Name = "Support",
                        Email = "support@xyz.com"
                    }
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
                });

                // Include XML comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            // Configure rate limiting (moved before app building)
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("fixed", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers["X-Client-Id"].ToString(),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit"),
                            Window = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("RateLimiting:Window")),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = builder.Configuration.GetValue<int>("RateLimiting:QueueLimit")
                        }));
            });

            // Configure JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                    };
                });

            // Configure Serilog for logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.MongoDB(builder.Configuration["MongoDbSettings:ConnectionString"],
                                 collectionName: builder.Configuration["MongoDbSettings:LogsCollectionName"])
                .CreateLogger();

            builder.Host.UseSerilog();

            // Add other services
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient();
            builder.Services.AddAutoMapper(typeof(Program));
            builder.Services.AddHealthChecks();

            // Add metrics
            builder.Services.AddMetrics();
            builder.Services.AddSingleton<MetricReporter>();

            // Add health checks
            builder.Services.AddHealthChecks()
                .AddSqlServer(builder.Configuration.GetConnectionString("SqlServer"),
                    name: "sqlserver-check",
                    tags: new[] { "ready" })
                .AddRedis(builder.Configuration.GetConnectionString("Redis"),
                    name: "redis-check",
                    tags: new[] { "ready" })
                .AddMongoDb(builder.Configuration["MongoDbSettings:ConnectionString"],
                    name: "mongodb-check",
                    tags: new[] { "ready" });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Middleware ordering is important here
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // Apply rate limiting middleware (moved after UseRouting)
            app.UseRateLimiter();
            app.UseMiddleware<IdempotencyMiddleware>();
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Metrics middleware
            app.UseMetricServer();
            app.UseMiddleware<MetricsMiddleware>();

            // Endpoints
            app.MapControllers();
            app.MapHealthChecks("/health");

            // Configure health check endpoints
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            app.Run();
        }
    }
}