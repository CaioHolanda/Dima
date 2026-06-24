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

        public int Code { get; set; }

        [JsonIgnore]
        public bool IsSuccess => Code is >= 200 and <= 299;

        [JsonConstructor]
        public Response(
            TData? data,
            int code=Configuration.DefaultStatusCode,
            string? message=null)
        {
            Data = data;
            Message = message;
            Code = code;
        }

    }
}
