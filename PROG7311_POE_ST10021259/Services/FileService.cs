namespace PROG7311_POE_ST10021259.Services
{
    public interface IFileService
    {

        // Validates and saves an uploaded PDF file to the simulated file server and returns the saved file path
  
        Task<(string filePath, string fileName)> SaveContractFileAsync(IFormFile file);

        //Returns the physical path to a stored file
        string GetFilePath(string storedPath);

        //Deletes a stored file if it exists
        void DeleteFile(string? storedPath);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileService> _logger;

        // only allow 
        private static readonly string[] AllowedMimeTypes = { "application/pdf" };
        // Allowed extensions
        private static readonly string[] AllowedExtensions = { ".pdf" };
        // Max 10MB
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public FileService(IWebHostEnvironment env, ILogger<FileService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<(string filePath, string fileName)> SaveContractFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file was provided.");

            // Validate extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException($"Invalid file type '{extension}'. Only PDF files are allowed.");

            // Validate MIME type
            if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new InvalidOperationException($"Invalid content type '{file.ContentType}'. Only PDF files are allowed.");

            // Validate file size
            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException($"File size exceeds the maximum allowed size of 10MB.");

            // Build the upload folder (simulated file server)
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "contracts");
            Directory.CreateDirectory(uploadFolder);

            // Create unique filename to avoid collisions
            var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(uploadFolder, uniqueName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Saved contract file: {Path}", fullPath);

            // Return the relative web path and original filename
            return ($"uploads/contracts/{uniqueName}", file.FileName);
        }

        public string GetFilePath(string storedPath)
        {
            return Path.Combine(_env.WebRootPath, storedPath.Replace('/', Path.DirectorySeparatorChar));
        }

        public void DeleteFile(string? storedPath)
        {
            if (string.IsNullOrEmpty(storedPath)) return;
            var fullPath = GetFilePath(storedPath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file: {Path}", fullPath);
            }
        }
    }
}
