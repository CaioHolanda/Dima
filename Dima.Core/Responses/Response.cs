using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Dima.Core.Responses
{
    public class Response<TData>
    {
        public TData? Data { get; set; }
        public string? Message { get; set; } = string.Empty;
        private readonly int _code;
        [JsonConstructor]
        public Response(
            TData? data,
            int code=Configuration.DefaultStatusCode,
            string? message=null)
        {
            Data = data;
            Message = message;
            _code = code;
        }
        public Response()
            =>_code=Configuration.DefaultStatusCode;
        [JsonIgnore]
        public bool IsSuccess => _code is >= 200 and <= 299;
    }
}
