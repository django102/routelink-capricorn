using System;
using System.Net;

namespace Common.Exceptions
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public ApiException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError) 
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}