using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROG7311_POE_ST10021259.Data;
using PROG7311_POE_ST10021259.Models;
using PROG7311_POE_ST10021259.Services;

namespace PROG7311_POE_ST10021259.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly GlmsDbContext _context;
        private readonly ICurrencyService _currencyService;

        public ServiceRequestsController(GlmsDbContext context, ICurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        // Get ServiceRequests
        public async Task<IActionResult> Index()
        {
            var requests = await _context.ServiceRequests
                .Include(sr => sr.Contract)
                    .ThenInclude(c => c!.Client)
                .OrderByDescending(sr => sr.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // Get the ServiceRequests details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var sr = await _context.ServiceRequests
                .Include(s => s.Contract).ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (sr == null) return NotFound();
            return View(sr);
        }

        // Get the create ServiceRequests
        public async Task<IActionResult> Create(int? contractId)
        {
            // Get active contracts only for the dropdown
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active).ToListAsync();

            var rate = await _currencyService.GetUsdToZarRateAsync();

            var vm = new ServiceRequestCreateViewModel
            {
                ContractId = contractId ?? 0,
                ExchangeRate = rate,
                Contracts = activeContracts.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Client?.Name} — {c.ServiceLevel} [{c.Status}]",
                    Selected = c.Id == contractId
                })
            };

            return View(vm);
        }

        // Post for create ServiceRequests
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequestCreateViewModel vm)
        {
            // Reload the contracts for redisplay incase of an error
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            vm.Contracts = activeContracts.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Client?.Name} — {c.ServiceLevel} [{c.Status}]",
                Selected = c.Id == vm.ContractId
            });

            if (!ModelState.IsValid)
            {
                vm.ExchangeRate = await _currencyService.GetUsdToZarRateAsync();
                return View(vm);
            }

            // Validate that the contract is not Expired or OnHold
            var contract = await _context.Contracts.FindAsync(vm.ContractId);
            if (contract == null)
            {
                ModelState.AddModelError("ContractId", "Selected contract does not exist.");
                vm.ExchangeRate = await _currencyService.GetUsdToZarRateAsync();
                return View(vm);
            }

            if (contract.Status != ContractStatus.Active)
            {
                ModelState.AddModelError("ContractId", "Service requests can only be created for Active contracts.");
                vm.ExchangeRate = await _currencyService.GetUsdToZarRateAsync();
                return View(vm);
            }

            // Get live exchange rate and calculate Rands
            var rate = await _currencyService.GetUsdToZarRateAsync();
            var zarAmount = _currencyService.ConvertUsdToZar(vm.CostUsd, rate);

            var serviceRequest = new ServiceRequest
            {
                ContractId = vm.ContractId,
                Description = vm.Description,
                CostUsd = vm.CostUsd,
                CostZar = zarAmount,
                ExchangeRateUsed = rate,
                Status = vm.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(serviceRequest);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Service request created. Cost: ${vm.CostUsd:N2} USD = R{zarAmount:N2} ZAR (rate: {rate:N4})";
            return RedirectToAction(nameof(Index));
        }

        // Get edit for ServiceRequests
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var sr = await _context.ServiceRequests.FindAsync(id);
            if (sr == null) return NotFound();

            ViewBag.Contracts = new SelectList(
                await _context.Contracts.Include(c => c.Client).ToListAsync(),
                "Id", "ServiceLevel", sr.ContractId);

            return View(sr);
        }

        // Post ServiceRequests edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,ContractId,Description,CostUsd,CostZar,ExchangeRateUsed,Status,CreatedAt")] ServiceRequest serviceRequest)
        {
            if (id != serviceRequest.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(serviceRequest);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Service request updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ServiceRequests.Any(e => e.Id == serviceRequest.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Contracts = new SelectList(
                await _context.Contracts.Include(c => c.Client).ToListAsync(),
                "Id", "ServiceLevel", serviceRequest.ContractId);
            return View(serviceRequest);
        }

        // Get delete for ServiceRequests
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var sr = await _context.ServiceRequests
                .Include(s => s.Contract).ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (sr == null) return NotFound();
            return View(sr);
        }

        // Podt the ServiceRequests delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sr = await _context.ServiceRequests.FindAsync(id);
            if (sr != null)
            {
                _context.ServiceRequests.Remove(sr);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Service request deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // API endpoint get current rate for JavaScript to automatically calculate
        [HttpGet]
        public async Task<IActionResult> GetRate()
        {
            var rate = await _currencyService.GetUsdToZarRateAsync();
            return Json(new { rate });
        }
    }
}
