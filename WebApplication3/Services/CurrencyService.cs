using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using WebApplication3.DTO_s;
using WebApplication3.Interfaces;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class CurrencyService : ICurrencyService
{
    private readonly string _connectionString;

    public CurrencyService(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<bool> AddCurrency(CurrencyRequestDTO request)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            var transaction = conn.BeginTransaction();
            try
            {
                var sqlQuery = "SELECT * FROM Country WHERE Name = @CountryName";
                object? result = null;
                foreach (var country in request.Countries)
                {
                    using (var sqlCommand = new SqlCommand(sqlQuery, conn, transaction))
                    {
                        sqlCommand.Parameters.AddWithValue("@CountryName", country.Name);

                        result = await sqlCommand.ExecuteScalarAsync();

                        if (result == null)
                            throw new Exception("Country doesn't exist");
                    }
                }

                sqlQuery = "SELECT * FROM Currency WHERE Name = @CurrencyName";

                result = null;
                using (var sqlCommand = new SqlCommand(sqlQuery, conn, transaction))
                {
                    sqlCommand.Parameters.AddWithValue("@CurrencyName", request.CurrencyName);
                    result = await sqlCommand.ExecuteScalarAsync();
                }

                int id = 1;
                if (result != null)
                {
                    sqlQuery = "SELECT ID FROM CURRENCY WHERE Name = @CurrencyName";
                    using (var sqlCommand = new SqlCommand(sqlQuery, conn, transaction))
                    {
                        sqlCommand.Parameters.AddWithValue("@CurrencyName", request.CurrencyName);
                        var reader = await sqlCommand.ExecuteReaderAsync();
                        await reader.ReadAsync();
                        id = reader.GetInt32(0);
                        await reader.CloseAsync();
                    }

                    sqlQuery = "UPDATE Currency SET Rate = @Rate WHERE Name = @CountryName";
                    using (var sqlCommand2 = new SqlCommand(sqlQuery, conn, transaction))
                    {
                        sqlCommand2.Parameters.AddWithValue("@CountryName", request.CurrencyName);
                        sqlCommand2.Parameters.AddWithValue("@Rate", request.Rate);

                        if (await sqlCommand2.ExecuteNonQueryAsync() == 0)
                            throw new Exception("Failed to update currency rate");
                    }
                }
                else
                {
                    sqlQuery = "SELECT COALESCE(MAX(Id), 1) FROM CURRENCY";
                    using (var sqlCommand = new SqlCommand(sqlQuery, conn, transaction))
                    {
                        var reader = await sqlCommand.ExecuteReaderAsync();
                        await reader.ReadAsync();
                        id = reader.GetInt32(0) + 1;
                        await reader.CloseAsync();
                    }

                    sqlQuery = "INSERT INTO CURRENCY (Id, Name, Rate) VALUES (@Id, @Name, @Rate)";
                    using (var sqlCommand = new SqlCommand(sqlQuery, conn, transaction))
                    {
                        sqlCommand.Parameters.AddWithValue("@Id", id);
                        sqlCommand.Parameters.AddWithValue("@Name", request.CurrencyName);
                        sqlCommand.Parameters.AddWithValue("@Rate", request.Rate);
                        if (await sqlCommand.ExecuteNonQueryAsync() == 0)
                            throw new Exception("Failed to insert new currency");
                    }
                }

                foreach (var country in request.Countries)
                {
                    sqlQuery =
                        "select * from Currency_Country cc join Country country ON country.Id = cc.Country_Id join Currency cur on cur.Id = cc.Currency_Id where country.Name = @CountryName and cur.Name = @CurrencyName";
                    result = null;
                    using (var sqlCommand = new SqlCommand(sqlQuery, conn, transaction))
                    {
                        sqlCommand.Parameters.AddWithValue("@CountryName", country.Name);
                        sqlCommand.Parameters.AddWithValue("@CurrencyName", request.CurrencyName);

                        result = await sqlCommand.ExecuteScalarAsync();
                    }

                    if (result == null)
                    {
                        sqlQuery =
                            "INSERT INTO Currency_Country (Country_Id, Currency_Id) VALUES (@CountryId, @CurrencyId)";
                        using (var sqlCommand2 = new SqlCommand(sqlQuery, conn, transaction))
                        {
                            sqlCommand2.Parameters.AddWithValue("@CountryId", country.Id);
                            sqlCommand2.Parameters.AddWithValue("@CurrencyId", id);

                            if (await sqlCommand2.ExecuteNonQueryAsync() == 0)
                                throw new Exception("Failed to insert new currency");
                        }
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }

    public async Task<object?> SearchCurrency(string type, string query)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            try
            {
                await conn.OpenAsync();
                if (type == "Country")
                {
                    var sqlQuery =
                        "SELECT cur.Name, cur.Rate FROM currency cur JOIN Currency_Country CC on cur.Id = CC.Currency_Id JOIN Country C on C.Id = CC.Country_Id where C.Name = @query";

                    using (var cmd = new SqlCommand(sqlQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@query", query);
                        var reader = await cmd.ExecuteReaderAsync();
                        
                        var results = new List<object?>();

                        while (await reader.ReadAsync())
                        {
                            results.Add(new { Name = reader.GetString(0), Rate = reader.GetFloat(1) });
                        }

                        await reader.CloseAsync();
                        
                        return new { Name = query, Currencies = results };
                    }
                }
                else if (type == "Currency")
                {
                    var sqlQuery =
                        "SELECT * from Country country join Currency_Country CC on country.Id = CC.Country_Id join Currency cur ON cur.Id = CC.Currency_Id where cur.Name = @query";

                    using (var cmd = new SqlCommand(sqlQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@query", query);
                        var reader = await cmd.ExecuteReaderAsync();
                        
                        var results = new List<Country>();

                        while (await reader.ReadAsync())
                        {
                            results.Add(new Country
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                        
                        await reader.CloseAsync();

                        return results.IsNullOrEmpty() ? null : results;
                    }
                }
                else
                {
                    throw new Exception("Invalid type. Expected: [Country, Currency]");
                }
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}