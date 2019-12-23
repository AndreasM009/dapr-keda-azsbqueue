using System;
using System.Threading.Tasks;
using consumer.Models;
using Microsoft.AspNetCore.Mvc;

namespace consumer.Controllers
{
    [ApiController]
    [Route("message-queue")]
    public class MessageQueueController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Process([FromBody]Message message)
        {
            var start = DateTime.Now;
            await Task.Delay(5 * 1000);
            var end = DateTime.Now;
            Console.WriteLine($"{message.Text} -- Received at: {start.ToString()} -- Finished at: {end.ToString()}");
            return Ok();
        }
    }
}
