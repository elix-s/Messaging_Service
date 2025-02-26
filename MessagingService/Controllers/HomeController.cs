using Microsoft.AspNetCore.Mvc;

namespace MessagingService.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpGet("client1")]
        public IActionResult Client1()
        {
            return View("Client1");
        }
        
        [HttpGet("client2")]
        public IActionResult Client2()
        {
            return View("Client2");
        }
        
        [HttpGet("client3")]
        public IActionResult Client3()
        {
            return View("Client3");
        }
    }
}