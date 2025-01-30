using Hangfire;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Retry;
using System;
using System.Diagnostics;
using static System.Collections.Specialized.BitVector32;

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
                Debug.WriteLine("File move operation started.");

                _backgroundJobClient.Enqueue(() => Helper.MoveFileToDestinationAsync(null));
                // Execute the retry logic for moving the file
               
                return Ok("File will be move shortly...");
            }
            catch (Exception ex)
            {
                return Ok($"An unexpected error occurred. Details: {ex.Message}");
            }
        }

    }
}
