using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class FallbackController: Controller
    {
        public ActionResult Index()
        {
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(),
                "wwroot", "index.html"), "text/HTML");
        }
    }
}
