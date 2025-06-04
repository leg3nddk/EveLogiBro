using EveLogiBro.Data;
using EveLogiBro.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework with SQLite database
builder.Services.AddDbContext<LogiDbContext>(options =>
    options.UseSqlite("Data Source=evelogibro.db"));

// Add the log monitoring background service
builder.Services.AddHostedService<LogMonitorService>();

// Add the log parser as a singleton service (can be injected elsewhere if needed)
builder.Services.AddSingleton<EveLogParser>();

// Add CORS to allow the web interface to communicate with the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod() 
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogiDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors();

// Enable static file serving
app.UseStaticFiles();

// Comment out HTTPS redirect for now
// app.UseHttpsRedirection();

app.UseAuthorization();

// Map controllers for our API endpoints
app.MapControllers();

// Set default route to serve our index.html
app.MapFallbackToFile("index.html");

app.Run();