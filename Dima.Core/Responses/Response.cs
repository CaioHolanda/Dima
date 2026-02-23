using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Dima.Core.Responses
{
    public class Response<TData>
    {
        private readonly int _code;
        public TData? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public Response(TData? data,int code=Configuration.DefaultStatusCode,string? message=null)
        {
            Data = data;
            Message = message;
            _code = code;
        }
        [JsonConstructor]
        public Response()=>_code=Configuration.DefaultStatusCode;
        [JsonIgnore]
        public bool IsSuccess => _code is >= 200 and <= 299;
    }
}
