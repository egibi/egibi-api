#nullable disable
using EgibiCoreLibrary;

namespace EgibiCoreLibrary.Models
{
    public class RequestResponse
    {
        public object ResponseData { get; set; }
        public int ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public ResponseError ResponseError { get; set; }

        public RequestResponse(object data = null, int? code = null, string message = null, ResponseError error = null)
        {
            ResponseData = data;
            ResponseCode = (int)code;
            ResponseMessage = message;
            ResponseError = error;
        }
    }

    public class ResponseError
    {
        public string ErrorCode { get; set; }
        public string ExceptionMessage { get; set; }
        public string InnerExceptionMessage { get; set; }

        public ResponseError(Exception exception, string code = "500")
        {
            ErrorCode = code;
            ExceptionMessage = exception.Message;
            InnerExceptionMessage = exception.InnerException?.Message;
        }
    }
}
