using FluentImporter.Demo.Extensions;
using FluentMinimalApiMapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddDatabase();
builder.AddMinimalApis();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.DeleteDatabase();
app.CreateDatabase();

app.UseSwagger();
app.UseSwaggerUI();
app.MapMinimalApis();
app.MapControllers();

app.Run();

//todo default value, Not empty, Validation checks, Db context is not needed, review usage