using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PublicGrievanceSystem.Models;
using PublicGrievanceSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace PublicGrievanceSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(
    ILogger<HomeController> logger,
    ApplicationDbContext context)
{
    _logger = logger;
    _context = context;
}

    public async Task<IActionResult> Index()
{
    var beforeAfterSlides = await _context.Complaints
        .AsNoTracking()
        .Where(c =>
            c.Status == "Resolved" &&
            !string.IsNullOrEmpty(c.ImagePath) &&
            _context.ComplaintResolutionEvidences.Any(e =>
                e.ComplaintId == c.Id &&
                !string.IsNullOrEmpty(e.ImagePath)))
        .OrderByDescending(c => c.CreatedAt)
        .Take(8)
        .Select(c => new
        {
            ComplaintId = c.Id,
            Title = c.Title,
            BeforeImagePath = c.ImagePath,

            AfterImagePath = _context.ComplaintResolutionEvidences
                .Where(e =>
                    e.ComplaintId == c.Id &&
                    !string.IsNullOrEmpty(e.ImagePath))
                .OrderByDescending(e => e.UploadedAt)
                .Select(e => e.ImagePath)
                .FirstOrDefault()
        })
        .ToListAsync();

    ViewBag.BeforeAfterSlides = beforeAfterSlides;

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
