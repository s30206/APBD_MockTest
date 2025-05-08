using Microsoft.IdentityModel.Tokens;
using WebApplication3.DTO_s;
using WebApplication3.Interfaces;
using WebApplication3.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Database");

builder.Services.AddSingleton<ICurrencyService, CurrencyService>(s => new CurrencyService(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/api/currency", async (CurrencyRequestDTO request, ICurrencyService service) =>
{
    try
    {
        var result = await service.AddCurrency(request);
        return result ? Results.Created() : Results.BadRequest();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapGet("/api/search", async (string type, string query, ICurrencyService service) =>
{
    try
    {
        var result = await service.SearchCurrency(type, query);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();