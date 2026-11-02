using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PublicGrievanceSystem.Models;

namespace PublicGrievanceSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
public IActionResult LogoutSuccess()
{
    TempData["LogoutMessage"] =
        "You have successfully logged out of the application.";

    return RedirectToAction("Index", "Home");
}
    public IActionResult Help()
{
    return View();
}

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
