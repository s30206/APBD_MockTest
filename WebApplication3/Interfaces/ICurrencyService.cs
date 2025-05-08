using WebApplication3.DTO_s;

namespace WebApplication3.Interfaces;

public interface ICurrencyService
{
    public Task<bool> AddCurrency(CurrencyRequestDTO request);
    public Task<object?> SearchCurrency(string type, string query);
}