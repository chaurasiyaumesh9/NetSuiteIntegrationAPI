using NetSuiteIntegrationAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<NetSuiteOptions>(
    builder.Configuration.GetSection("NetSuite")
);

Console.WriteLine($"Environment: {builder.Configuration["NetSuite:PrivateKey"]}");

Console.WriteLine("---- CONFIG TEST ----");
Console.WriteLine($"AccountId: {builder.Configuration["NetSuite:AccountId"]}");
Console.WriteLine($"ClientId: {builder.Configuration["NetSuite:ClientId"]}");
Console.WriteLine($"PrivateKey length: {builder.Configuration["NetSuite:PrivateKey"]?.Length}");
Console.WriteLine("----------------------");

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<NetSuiteService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularLocal",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseCors("AngularLocal");

app.UseAuthorization();

app.MapControllers();

app.Run();
