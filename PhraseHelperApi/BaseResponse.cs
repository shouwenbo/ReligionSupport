using Newtonsoft.Json;

namespace PhraseHelperApi
{
    public class BaseResponse
    {
        public string Status { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public ErrorDetail Error { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ErrorDetail
    {
        public string Message { get; set; }
        public string Details { get; set; }
    }
}
