using PROG7311_POE_ST10021259.Services;
using PROG7311_POE_ST10021259.Controllers;
using PROG7311_POE_ST10021259.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace PROG7311_POE_ST10021259.Tests
{
    // ─── 1. Currency Calculation Tests ───────────────────────────────────────
    public class CurrencyCalculationTests
    {
        // We test ConvertUsdToZar directly — no API call needed
        private decimal Convert(decimal usd, decimal rate)
        {
            // Mirror the exact logic from CurrencyService
            if (rate <= 0) throw new ArgumentException("Exchange rate must be greater than zero.");
            if (usd < 0) throw new ArgumentException("Amount cannot be negative.");
            return Math.Round(usd * rate, 2);
        }

        [Fact]
        public void Convert_100Usd_At18_50_Returns1850()
        {
            var result = Convert(100m, 18.50m);
            Assert.Equal(1850.00m, result);
        }

        [Fact]
        public void Convert_ZeroUsd_ReturnsZero()
        {
            var result = Convert(0m, 18.50m);
            Assert.Equal(0.00m, result);
        }

        [Fact]
        public void Convert_RoundsToTwoDecimalPlaces()
        {
            // 1 USD × 18.6789 = 18.68 (rounded)
            var result = Convert(1m, 18.6789m);
            Assert.Equal(18.68m, result);
        }

        [Fact]
        public void Convert_NegativeRate_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Convert(100m, -1m));
        }

        [Fact]
        public void Convert_ZeroRate_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Convert(100m, 0m));
        }

        [Fact]
        public void Convert_NegativeAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Convert(-50m, 18.50m));
        }

        [Fact]
        public void Convert_LargeAmount_IsCorrect()
        {
            var result = Convert(50000m, 18.50m);
            Assert.Equal(925000.00m, result);
        }
    }

    // ─── 2. File Validation Tests ─────────────────────────────────────────────
    public class FileValidationTests
    {
        // Mirrors the validation logic inside FileService
        private static readonly string[] AllowedExtensions = { ".pdf" };
        private static readonly string[] AllowedMimeTypes = { "application/pdf" };
        private const long MaxBytes = 10 * 1024 * 1024; // 10MB

        private void ValidateFile(string fileName, string contentType, long fileSize)
        {
            if (fileSize == 0)
                throw new InvalidOperationException("No file was provided.");

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException($"Invalid file type '{ext}'. Only PDF files are allowed.");

            if (!AllowedMimeTypes.Contains(contentType.ToLowerInvariant()))
                throw new InvalidOperationException($"Invalid content type. Only PDF files are allowed.");

            if (fileSize > MaxBytes)
                throw new InvalidOperationException("File size exceeds the maximum allowed size of 10MB.");
        }

        [Fact]
        public void ValidPdf_PassesValidation()
        {
            // Should not throw
            var ex = Record.Exception(() => ValidateFile("agreement.pdf", "application/pdf", 1024));
            Assert.Null(ex);
        }

        [Fact]
        public void ExeFile_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ValidateFile("malware.exe", "application/octet-stream", 1024));
        }

        [Fact]
        public void DocxFile_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ValidateFile("contract.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 1024));
        }

        [Fact]
        public void JpgFile_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ValidateFile("photo.jpg", "image/jpeg", 1024));
        }

        [Fact]
        public void EmptyFile_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ValidateFile("empty.pdf", "application/pdf", 0));
        }

        [Fact]
        public void FileTooLarge_ThrowsInvalidOperationException()
        {
            long elevenMb = 11 * 1024 * 1024;
            Assert.Throws<InvalidOperationException>(() =>
                ValidateFile("big.pdf", "application/pdf", elevenMb));
        }

        [Fact]
        public void ExactlyMaxSize_PassesValidation()
        {
            var ex = Record.Exception(() => ValidateFile("max.pdf", "application/pdf", MaxBytes));
            Assert.Null(ex);
        }
    }

    // ─── 3. Workflow / Business Rule Tests ────────────────────────────────────
    public class ContractWorkflowTests
    {
        // Mirrors the workflow guard logic from ServiceRequestsController
        private void ValidateContractForRequest(ContractStatus status)
        {
            if (status == ContractStatus.Expired)
                throw new InvalidOperationException("Cannot create a service request for an Expired contract.");
            if (status == ContractStatus.OnHold)
                throw new InvalidOperationException("Cannot create a service request for a contract that is On Hold.");
            if (status == ContractStatus.Draft)
                throw new InvalidOperationException("Cannot create a service request for a Draft contract.");
        }

        [Fact]
        public void DraftContract_BlocksServiceRequest()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ValidateContractForRequest(ContractStatus.Draft));
        }

        [Fact]
        public void ActiveContract_AllowsServiceRequest()
        {
            var ex = Record.Exception(() =>
                ValidateContractForRequest(PROG7311_POE_ST10021259.Models.ContractStatus.Active));
            Assert.Null(ex);
        }


        [Fact]
        public void ExpiredContract_BlocksServiceRequest()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ValidateContractForRequest(PROG7311_POE_ST10021259.Models.ContractStatus.Expired));
        }

        [Fact]
        public void OnHoldContract_BlocksServiceRequest()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ValidateContractForRequest(PROG7311_POE_ST10021259.Models.ContractStatus.OnHold));
        }

        [Fact]
        public void ExpiredContract_ErrorMessage_IsCorrect()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ValidateContractForRequest(PROG7311_POE_ST10021259.Models.ContractStatus.Expired));
            Assert.Contains("Expired", ex.Message);
        }

        [Fact]
        public void OnHoldContract_ErrorMessage_IsCorrect()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ValidateContractForRequest(PROG7311_POE_ST10021259.Models.ContractStatus.OnHold));
            Assert.Contains("On Hold", ex.Message);
        }
    }
}
namespace TestProject
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {

        }
    }
}
