using Microsoft.AspNetCore.Mvc;
using RentBridge.Domain.Common;

namespace RentBridge.Api;

/// <summary>
/// Maps a failed <see cref="Result"/> onto the HTTP status its
/// <see cref="ResultErrorKind"/> declares, so authorization failures return
/// 403 instead of being flattened into 400 with everything else.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToErrorResponse(this Result result)
    {
        var status = result.ErrorKind switch
        {
            ResultErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ResultErrorKind.NotFound => StatusCodes.Status404NotFound,
            ResultErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return new ObjectResult(new { error = result.Error }) { StatusCode = status };
    }
}
