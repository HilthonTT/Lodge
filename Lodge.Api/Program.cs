using Lodge.Api.Extensions;
using Lodge.Application;
using Lodge.BackgroundTasks;
using Lodge.Infrastructure;
using Lodge.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddBackgroundTasks(builder.Configuration)
    .AddApplication();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.ApplyMigrations();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.Run();
