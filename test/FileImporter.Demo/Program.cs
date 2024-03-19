using Carter;
using FileImporter.Demo.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddDatabase();
builder.Services.AddCarter();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.DeleteDatabase();
app.CreateDatabase();

app.UseSwagger();
app.UseSwaggerUI();
app.MapCarter();
app.MapControllers();

app.Run();