using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace PhraseHelperApi
{
    public class CustomActionResult : IActionResult
    {
        private readonly object _data;
        private readonly int _statusCode;
        private readonly string _message;

        public CustomActionResult(object data, int statusCode = 200, string message = "OK")
        {
            _data = data;
            _statusCode = statusCode;
            _message = message;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = new BaseResponse
            {
                Status = _statusCode == 200 ? "success" : "error",
                Code = _statusCode,
                Message = _message,
                Data = _data,
                Timestamp = DateTime.UtcNow
            };

            var jsonResult = new JsonResult(response)
            {
                StatusCode = _statusCode
            };

            await jsonResult.ExecuteResultAsync(context);
        }
    }
}
