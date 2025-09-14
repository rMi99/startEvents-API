using Microsoft.AspNetCore.Mvc;

namespace StartEvent_API.Extensions
{
    public static class ControllerExtensions
    {
        public static IActionResult HandleException(this ControllerBase controller, Exception ex, string message = "An error occurred")
        {
            // Log the exception (you can inject ILogger here if needed)
            // For now, we'll just return a proper error response
            return controller.StatusCode(500, new { Success = false, Message = message, Error = ex.Message });
        }

        public static IActionResult HandleBadRequest(this ControllerBase controller, Exception ex, string message = "Bad request")
        {
            return controller.BadRequest(new { Success = false, Message = message, Error = ex.Message });
        }
    }
}
