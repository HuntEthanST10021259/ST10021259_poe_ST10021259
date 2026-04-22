namespace PROG7311_POE_ST10021259.Services
{
    public interface ICurrencyService
    {
        //Fetches the current USD to ZAR exchange rate from the API forExchangeRate 
        Task<decimal> GetUsdToZarRateAsync();

        //Converts a USD amount to Rands using the given rate.
        decimal ConvertUsdToZar(decimal amountUsd, decimal rate);
    }

    public class CurrencyService : ICurrencyService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CurrencyService> _logger;

        // Simple in memory cache to not over use the free API
        private static decimal _cachedRate = 0;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public CurrencyService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<CurrencyService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<decimal> GetUsdToZarRateAsync()
        {
            // Return cached rate if still valid 
            if (_cachedRate > 0 && DateTime.UtcNow < _cacheExpiry)
                return _cachedRate;

            await _lock.WaitAsync();
            try
            {
                // Double check after acquiring lock
                if (_cachedRate > 0 && DateTime.UtcNow < _cacheExpiry)
                    return _cachedRate;

                var apiKey = _configuration["ExchangeRateApi:ApiKey"] ?? "de3425b10e9184d5fbc48e54";
                var client = _httpClientFactory.CreateClient();
                var url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/USD";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
                    if (json?.conversion_rates != null && json.conversion_rates.TryGetValue("ZAR", out var zarRate) && zarRate > 0)
                    {
                        _cachedRate = (decimal)zarRate;
                        _cacheExpiry = DateTime.UtcNow.AddMinutes(15);
                        _logger.LogInformation("Fetched USD→ZAR rate: {Rate}", _cachedRate);
                        return _cachedRate;
                    }
                }

                // Fallback rate if API fails
                _logger.LogWarning("Could not fetch live rate. Using fallback rate of 18.50.");
                return 18.50m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exchange rate.");
                return 18.50m; // fallback
            }
            finally
            {
                _lock.Release();
            }
        }

        public decimal ConvertUsdToZar(decimal amountUsd, decimal rate)
        {
            if (rate <= 0)
                throw new ArgumentException("Exchange rate must be greater than zero.", nameof(rate));
            if (amountUsd < 0)
                throw new ArgumentException("Amount cannot be negative.", nameof(amountUsd));

            return Math.Round(amountUsd * rate, 2);
        }

        private class ExchangeRateResponse
        {
            public string? result { get; set; }
            public Dictionary<string, double>? conversion_rates { get; set; }
        }
    }
}
