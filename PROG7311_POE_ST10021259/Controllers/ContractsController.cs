using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROG7311_POE_ST10021259.Data;
using PROG7311_POE_ST10021259.Models;
using PROG7311_POE_ST10021259.Services;

namespace PROG7311_POE_ST10021259.Controllers
{
    public class ContractsController : Controller
    {
        private readonly GlmsDbContext _context;
        private readonly IFileService _fileService;

        public ContractsController(GlmsDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        // Index action - list and filter contracts
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, ContractStatus? status)
        {
            
            var query = _context.Contracts
                .Include(c => c.Client)
                .AsQueryable();

            // filters
            if (fromDate.HasValue)
                query = query.Where(c => c.StartDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(c => c.EndDate <= toDate.Value);

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            var results = await query.OrderBy(c => c.StartDate).ToListAsync();

            var vm = new ContractFilterViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Status = status,
                Results = results
            };

            return View(vm);
        }

        // Get Contract Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null) return NotFound();
            return View(contract);
        }

        // get Contract Create
        public IActionResult Create()
        {
            ViewBag.Clients = new SelectList(_context.Clients, "Id", "Name");
            return View();
        }

        // post Contract Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ClientId,StartDate,EndDate,Status,ServiceLevel")] Contract contract,
            IFormFile? signedAgreement)
        {
            if (ModelState.IsValid)
            {
                // Handle PDF upload
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    try
                    {
                        var (path, fileName) = await _fileService.SaveContractFileAsync(signedAgreement);
                        contract.SignedAgreementPath = path;
                        contract.SignedAgreementFileName = fileName;
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError("signedAgreement", ex.Message);
                        ViewBag.Clients = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
                        return View(contract);
                    }
                }

                _context.Add(contract);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Contract created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
            return View(contract);
        }

        // Get for edit contracts
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return NotFound();
            ViewBag.Clients = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
            return View(contract);
        }

        // Post for edit contract
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,ClientId,StartDate,EndDate,Status,ServiceLevel,SignedAgreementPath,SignedAgreementFileName")] Contract contract,
            IFormFile? signedAgreement)
        {
            if (id != contract.Id) return NotFound();

            // Load existing contract to check if status change is allowed
            var existing = await _context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existing != null && existing.Status == ContractStatus.Draft && contract.Status != ContractStatus.Draft)
            {
                // Only allow status change from Draft if PDF is uploaded (either existing or new upload)
                bool hasPdf = !string.IsNullOrEmpty(contract.SignedAgreementPath) || (signedAgreement != null && signedAgreement.Length > 0);
                if (!hasPdf)
                {
                    ModelState.AddModelError("Status", "Cannot change status from Draft until a signed agreement PDF is uploaded.");
                    ViewBag.Clients = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
                    return View(contract);
                }
            }

            if (ModelState.IsValid)
            {
                
                // handling a new pdf upload old one needs to be replaced
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    try
                    {
                        // Delete old file
                        _fileService.DeleteFile(contract.SignedAgreementPath);

                        var (path, fileName) = await _fileService.SaveContractFileAsync(signedAgreement);
                        contract.SignedAgreementPath = path;
                        contract.SignedAgreementFileName = fileName;
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError("signedAgreement", ex.Message);
                        ViewBag.Clients = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
                        return View(contract);
                    }
                }

                try
                {
                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Contract updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Contracts.Any(e => e.Id == contract.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = new SelectList(_context.Clients, "Id", "Name", contract.ClientId);
            return View(contract);
        }

        // Get delete contract
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts.Include(c => c.Client).FirstOrDefaultAsync(m => m.Id == id);
            if (contract == null) return NotFound();
            return View(contract);
        }

        // Post delete contract
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract != null)
            {
                bool hasActiveRequests = contract.ServiceRequests.Any(sr =>
                    sr.Status != ServiceRequestStatus.Completed &&
                    sr.Status != ServiceRequestStatus.Cancelled);

                if (hasActiveRequests)
                {
                    TempData["Error"] = "Cannot delete this contract because it has service requests that are not completed or cancelled.";
                    return RedirectToAction(nameof(Index));
                }

                _fileService.DeleteFile(contract.SignedAgreementPath);
                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Contract deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Get download contracts gives the pdf t3o download
        public async Task<IActionResult> Download(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null || string.IsNullOrEmpty(contract.SignedAgreementPath))
                return NotFound("No agreement file found for this contract.");

            var physicalPath = _fileService.GetFilePath(contract.SignedAgreementPath);
            if (!System.IO.File.Exists(physicalPath))
                return NotFound("File not found on server.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            return File(fileBytes, "application/pdf", contract.SignedAgreementFileName ?? "agreement.pdf");
        }
    }
}
