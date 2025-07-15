using Microsoft.AspNetCore.Mvc;
using PQBI.Web.Controllers;

namespace PQBI.Web.Public.Controllers
{
    public class AboutController : PQBIControllerBase
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}