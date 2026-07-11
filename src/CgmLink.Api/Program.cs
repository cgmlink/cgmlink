using System.Text.Json.Serialization;
using Asp.Versioning;
using CgmLink.Api.Observability;
using FluentValidation;
using CgmLink.Api.Endpoints;
using CgmLink.Api.Middleware;
using CgmLink.Api.Models;
using CgmLink.Api.Swagger;
using CgmLink.Data;
using CgmLink.Data.Repository;
using CgmLink.Identity;
using CgmLink.LibreLinkClient;
using CgmLink.Mail;
using CgmLink.Nutrition;
using CgmLink.Sync.LibreLink;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .EnableApiVersionBinding();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(opt =>
{
    opt.CustomSchemaIds(type =>
    {
        return type.FullName.Replace("CgmLink.Api.Endpoints.", "")
            .Replace("CgmLink.Identity.Endpoints.", "")
            .Replace("CgmLink.Api.Models.", "")
            .Replace("CgmLink.Identity.Models.", "")
            .Replace(".", "_");
    });
    opt.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Scheme = "bearer"
    });
    opt.OperationFilter<SecurityRequirementsOperationFilter>();
    opt.SchemaFilter<XEnumNamesSchemaFilter>();
});

builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));

builder.Services.AddHealthChecks().AddDatabaseHealthChecks();
builder.Services.AddData(builder.Configuration.GetSection("Data").Bind);
builder.Services.AddIdentity(builder.Configuration.GetSection("Identity").Bind);
builder.Services.AddNutrition(builder.Configuration.GetSection("Nutrition").Bind);

builder.Services.AddTransient<ExceptionMiddleware>();

builder.Services.AddScoped<CgmLinkDbInitializer>();
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("Api"));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<LibreLinkOptions>(builder.Configuration.GetSection("LibreLink"));

builder.Services.AddLibreLinkClientFactory();

builder.Services.AddMail(builder.Configuration.GetSection("Mail").Bind);

builder.Services.Configure<DataServiceOptions>(builder.Configuration.GetSection("DataService"));
builder.Services.AddHostedService<DataService>();

builder.Services.AddHostedService<LibreSyncService>();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSerilog(dispose: true);
});

builder.AddObservability();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseIdentity();

app.UseHealthChecks("/health");

app.MapIdentityEndpoints();
app.MapCgmLinkEndpoints();
app.MapNutritionEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<CgmLinkDbInitializer>();
    await dbInitializer.InitialiseDbAsync(app.Lifetime.ApplicationStopping);
}

await app.RunAsync();
