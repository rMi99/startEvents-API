using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace StartEvent_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var healthInfo = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                uptime = Process.GetCurrentProcess().StartTime,
                memory = GC.GetTotalMemory(false),
                processorCount = Environment.ProcessorCount
            };

            return Ok(healthInfo);
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "pong", timestamp = DateTime.UtcNow });
        }

        [HttpGet("ready")]
        public IActionResult Ready()
        {
            // Add any readiness checks here (database connectivity, external services, etc.)
            return Ok(new { status = "Ready", timestamp = DateTime.UtcNow });
        }

        [HttpGet("live")]
        public IActionResult Live()
        {
            // Add any liveness checks here
            return Ok(new { status = "Alive", timestamp = DateTime.UtcNow });
        }
    }
}
