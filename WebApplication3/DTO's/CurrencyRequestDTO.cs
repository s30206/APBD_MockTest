using WebApplication3.Models;

namespace WebApplication3.DTO_s;

public class CurrencyRequestDTO
{
    public string CurrencyName { get; set; }
    public float Rate { get; set; }
    public List<Country> Countries { get; set; }
}