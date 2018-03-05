using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleBlog.Models;
using SimpleBlog.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace SimpleBlog.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

        
        public IActionResult About()
        {
            ViewData["Message"] = "In this blog you can find interesting articles about .NET framework";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Want to write an article? Contact us:";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
