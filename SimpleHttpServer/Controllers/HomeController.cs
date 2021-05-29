using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace SimpleHttpServer.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        public async Task<IActionResult> GetContent()
        {
            await Task.Delay(3000);
            return Content("Hello world");
        }
    }
}
