using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MVCeTicaret.Models;

namespace MVCeTicaret.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Context db = new Context();

            TempData["Uzakdogu"] = db.Products.Where(x => x.SubCategoryID == 1).OrderBy(x => Guid.NewGuid()).Take(4).ToList();
            TempData["Amerikan"] = db.Products.Where(x => x.SubCategoryID == 2).OrderBy(x => Guid.NewGuid()).Take(4).ToList();
            TempData["Bagimsiz"] = db.Products.Where(x => x.SubCategoryID == 4).OrderBy(x => Guid.NewGuid()).Take(4).ToList();

            return View();
        }
    }
}