using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG7311_POE_ST10021259.Data;
using PROG7311_POE_ST10021259.Models;

namespace PROG7311_POE_ST10021259.Controllers
{
    public class ClientsController : Controller
    {
        private readonly GlmsDbContext _context;

        public ClientsController(GlmsDbContext context)
        {
            _context = context;
        }

        // Get Clients
        public async Task<IActionResult> Index()
        {
            return View(await _context.Clients.ToListAsync());
        }

        // Get Client details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var client = await _context.Clients
                .Include(c => c.Contracts)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (client == null) return NotFound();
            return View(client);
        }

        //Get Create Clients
        public IActionResult Create()
        {
            return View();
        }

        // Post create client
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ContactDetails,Region")] Client client)
        {
            if (ModelState.IsValid)
            {
                _context.Add(client);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Client created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // Get edit Client
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();
            return View(client);
        }

        // Post edit Client
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ContactDetails,Region")] Client client)
        {
            if (id != client.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Client updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Clients.Any(e => e.Id == client.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // Get for delete Client
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clients.FirstOrDefaultAsync(m => m.Id == id);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Contracts)
                    .ThenInclude(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return RedirectToAction(nameof(Index));

            // Check all contracts are expired
            bool hasNonExpiredContracts = client.Contracts.Any(c => c.Status != ContractStatus.Expired);
            if (hasNonExpiredContracts)
            {
                TempData["Error"] = "Cannot delete this client because they have contracts that are not expired.";
                return RedirectToAction(nameof(Index));
            }

            // Check all service requests across all contracts are completed or cancelled
            bool hasActiveServiceRequests = client.Contracts
                .SelectMany(c => c.ServiceRequests)
                .Any(sr => sr.Status != ServiceRequestStatus.Completed && sr.Status != ServiceRequestStatus.Cancelled);

            if (hasActiveServiceRequests)
            {
                TempData["Error"] = "Cannot delete this client because one or more contracts have service requests that are not completed or cancelled.";
                return RedirectToAction(nameof(Index));
            }

            // Safe to delete — cascade delete contracts (service requests cascade via DB)
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Client and all associated expired contracts deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
