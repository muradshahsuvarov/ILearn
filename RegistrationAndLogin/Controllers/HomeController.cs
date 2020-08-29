using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RegistrationAndLogin.Controllers
{

    public class HomeController : Controller
    {
        // GET: Home
        [HttpGet]
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("MyProfile", "User");
            }
            return View();
        }

        [HttpGet]
        public ActionResult About()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public ActionResult Services()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Contact()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public ActionResult myApplications()
        {
            return View();
        }

    }
}