using McServerApi.Services;
using Microsoft.AspNetCore.Authentication;

AppConfiguration configuration = new();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(configuration);
builder.Services.AddSingleton<Storage>();
builder.Services.AddSingleton<Server>();
builder.Services.AddSingleton<JarCache>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run($"http://*:{configuration.ApiPort}");