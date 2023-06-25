using PandaFileImporter;
using PandaTech.ServiceResponse;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsProduction())
{
    builder.Services.AddSingleton<IExceptionHandler, DebugExceptionHandler>();
}
else
{
    builder.Services.AddSingleton<IExceptionHandler, PublicExceptionHandler>();
}

builder.Services.AddSingleton<FileImporter>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
