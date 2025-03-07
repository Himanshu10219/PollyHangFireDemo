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
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] {  "default","move_file" }; //  Make sure your queue is included
});
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });

var app = builder.Build();


app.UseHttpsRedirection();
app.UseHangfireDashboard();
app.MapHangfireDashboard();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthorization();

app.MapControllers();

app.Run();
