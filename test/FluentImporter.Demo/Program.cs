using FluentMinimalApiMapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddMinimalApis();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapMinimalApis();
app.MapControllers();

app.Run();
