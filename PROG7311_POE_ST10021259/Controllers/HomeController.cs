using Microsoft.AspNetCore.Mvc;
using PROG7311_POE_ST10021259.Data;
using PROG7311_POE_ST10021259.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace PROG7311_POE_ST10021259.Controllers
{
    public class HomeController : Controller
    {
        private readonly GlmsDbContext _context;

        public HomeController(GlmsDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalClients = await _context.Clients.CountAsync();
            ViewBag.TotalContracts = await _context.Contracts.CountAsync();
            ViewBag.ActiveContracts = await _context.Contracts.CountAsync(c => c.Status == Models.ContractStatus.Active);
            ViewBag.TotalRequests = await _context.ServiceRequests.CountAsync();
            return View();
        }
    }
}
