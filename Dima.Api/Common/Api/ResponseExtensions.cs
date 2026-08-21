using Dima.Core.Responses;

namespace Dima.Api.Common.Api;

public static class ResponseExtensions
{
    public static IResult ToResult<TData>(
        this Response<TData> response)
    {
        return response.Code switch
        {
            StatusCodes.Status200OK =>
                Results.Ok(response),

            StatusCodes.Status201Created =>
                Results.Json(
                    response,
                    statusCode: StatusCodes.Status201Created),

            StatusCodes.Status400BadRequest =>
                Results.BadRequest(response),

            StatusCodes.Status401Unauthorized =>
                Results.Json(
                    response,
                    statusCode: StatusCodes.Status401Unauthorized),

            StatusCodes.Status403Forbidden =>
                Results.Json(
                    response,
                    statusCode: StatusCodes.Status403Forbidden),

            StatusCodes.Status404NotFound =>
                Results.NotFound(response),

            StatusCodes.Status409Conflict =>
                Results.Conflict(response),

            _ =>
                Results.Json(
                    response,
                    statusCode: response.Code)
        };
    }
}