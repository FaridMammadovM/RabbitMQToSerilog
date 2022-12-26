using Autofac;
using Autofac.Extensions.DependencyInjection;
using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Factory;
using RabbitMQ.Client;
using SearchApi.AutoFac;
using SearchApi.IntegrationEvents.Event;
using SearchApi.IntegrationEvents.EventHandlers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new AutofacModule());
    });

var logger = new LoggerConfiguration()
                .WriteTo.File("logs.txt")
                .CreateLogger();
builder.Logging.AddSerilog(logger);
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddLogging(i => 
//{ 
//    i.ClearProviders();
//    i.AddProvider(new MyCustomLoggerProvide());
//    });
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<MedicalIntegrationEventHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
{
    EventBusConfig config = new EventBusConfig
    {
        ConnectionRetryCount = 5,
        EventNameSuffix = "IntegrationEvent",
        SubscriberClientAppName = "SearchApi",
        EventBusType = EventBusType.RabbitMQ,
        //Connection = new ConnectionFactory()
        //{
        //    HostName = "rabbitmq"
        //}
    };

    return EventBusFactory.Create(config, sp);
});

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


IEventBus eventBus = app.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<MedicalIntegrationEvent, MedicalIntegrationEventHandler>();

app.Run();