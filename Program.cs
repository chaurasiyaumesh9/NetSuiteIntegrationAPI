using NetSuiteIntegrationAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<NetSuiteOptions>(
    builder.Configuration.GetSection("NetSuite")
);

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
}

app.UseCors("AngularLocal");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
