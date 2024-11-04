using Newtonsoft.Json;
using System.Net;

namespace PhraseHelperApi
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = new BaseResponse
            {
                Status = "error",
                Code = (int)HttpStatusCode.InternalServerError,
                Message = exception.Message,
                Error = new ErrorDetail
                {
                    Message = exception.Message,
                    Details = exception.StackTrace
                },
                Timestamp = DateTime.UtcNow
            };

            var result = JsonConvert.SerializeObject(response);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return context.Response.WriteAsync(result);
        }
    }
}
