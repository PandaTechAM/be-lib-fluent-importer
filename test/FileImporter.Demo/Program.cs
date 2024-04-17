using FileImporter.Demo.Extensions;
using FluentMinimalApiMapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddDatabase();
builder.AddEndpoints();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.DeleteDatabase();
app.CreateDatabase();

app.UseSwagger();
app.UseSwaggerUI();
app.MapEndpoints();
app.MapControllers();

app.Run();

//todo default value, Not empty, Validation checks, Db context is not needed, review usage