using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddHangfire(configuration =>
    configuration.UseInMemoryStorage()); // You can replace this with a persistent storage if needed

// Register the Hangfire Server
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHangfireServer();
var app = builder.Build();


app.UseHttpsRedirection();
app.UseHangfireDashboard();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthorization();

app.MapControllers();

app.Run();
