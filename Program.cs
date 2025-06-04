using EveLogiBro.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework with SQLite database
builder.Services.AddDbContext<LogiDbContext>(options =>
    options.UseSqlite("Data Source=evelogibro.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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