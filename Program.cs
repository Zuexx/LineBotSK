using Line.Messaging;
using Linebot.Foundations.Bots;
using SHL.MRDashboard.Api.Extension;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MediatR service
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Dependency Injection For Line Messaging SDK
builder.Services.AddSingleton<ILineMessagingClient, LineMessagingClient>();

// Dependency Injection For Line Messaging SDK - Webook / Bot
builder.Services.AddSingleton<ILineBotApp, LineBotApp>();

// Dependency Injection For HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register endpoint related modules
builder.Services.RegisterModules();


var app = builder.Build();
app.MapEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();//(c => { c.DefaultModelExpandDepth(-1); });
}

app.UseHttpsRedirection();

app.Run();
