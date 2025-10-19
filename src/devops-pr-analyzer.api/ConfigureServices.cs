using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using devops_pr_analyzer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace devops_pr_analyzer;

public static class ConfigureServices
{

    public static WebApplicationBuilder AddServiceBusProcessing(this WebApplicationBuilder builder, string connectionString)
    {
        if(string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string for Azure Service Bus cannot be null or empty.", nameof(connectionString));

        builder.Services.AddHostedService<ServiceBusProcessorService>();
        builder.Services.AddSingleton(sp =>
        {
            var connectionString = builder.Configuration.GetConnectionString("AzureServiceBus");
            return new ServiceBusClient(connectionString);
        });

        builder.Services.AddSingleton(x =>
        {
            var connectionString = builder.Configuration.GetConnectionString("AzureServiceBus");
            return new ServiceBusAdministrationClient(connectionString);

        });


        return builder;
    }

    public static WebApplicationBuilder AddServiceBusProcessing(this WebApplicationBuilder builder, string serviceBusFullyQualifiedNamespace, TokenCredential azureCredential)
    {
        if (string.IsNullOrWhiteSpace(serviceBusFullyQualifiedNamespace))
            throw new ArgumentException("Service Bus fully qualified namespace cannot be null or empty.", nameof(serviceBusFullyQualifiedNamespace));

        builder.Services.AddHostedService<ServiceBusProcessorService>();
        builder.Services.AddSingleton(sp =>
        {
            return new ServiceBusClient(serviceBusFullyQualifiedNamespace, azureCredential);
        });

        builder.Services.AddSingleton(x =>
        {
            return new ServiceBusAdministrationClient(serviceBusFullyQualifiedNamespace, azureCredential);
        });


        return builder;
    }
}
