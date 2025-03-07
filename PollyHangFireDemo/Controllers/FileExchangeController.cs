using Hangfire;
using Microsoft.AspNetCore.Mvc;
using PollyHangFireDemo.HangfireUtils;

namespace PollyHangFireDemo.Controllers
{
    [ApiController]
    public class FileExchangeController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public FileExchangeController(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;

        }

        [HttpGet]
        [Route("api/[controller]/MoveFile")]
        public async Task<IActionResult> MoveFileAsync()
        {
            try
            {
                var helper = new Helper();
                _backgroundJobClient.JobEnqueuer(
                    action: () => helper.MoveFileToDestinationAsyncWithoutPolly(),
                    queue: "move_file",
                    onRetry: (ex, timeSpan, retryCount) => RetryPolicies.PrintDebugLine(ex, timeSpan, retryCount)
                    );
                return Ok("File will be move shortly...");
            }
            catch (Exception ex)
            {
                return Ok($"An unexpected error occurred. Details: {ex.Message}");
            }
        }

    }
}
