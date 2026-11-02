
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PublicGrievanceSystem.Data;

namespace PublicGrievanceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.Complaints
                .AsNoTracking()
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                query = query.Where(c =>
                    c.CreatedAt >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDateExclusive =
                    toDate.Value.Date.AddDays(1);

                query = query.Where(c =>
                    c.CreatedAt < endDateExclusive);
            }

            var complaints = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(complaints);
        }
    }
}
