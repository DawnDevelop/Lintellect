
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using devops_pr_analyzer.Constants;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Diagnostics;
using System.Text.Json;

namespace devops_pr_analyzer.Services;

public class ServiceBusProcessorService(ServiceBusClient serviceBusClient, ServiceBusAdministrationClient adminClient) : BackgroundService
{
   
    private readonly ServiceBusProcessor processor = serviceBusClient.CreateProcessor(ApplicationConstants.ServiceBusQueueName, new ServiceBusProcessorOptions());

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(!await adminClient.QueueExistsAsync(ApplicationConstants.ServiceBusQueueName, stoppingToken))
        {
            await adminClient.CreateQueueAsync(ApplicationConstants.ServiceBusQueueName, cancellationToken: stoppingToken);
        }

        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;

        await processor.StartProcessingAsync(stoppingToken);

        // Wait until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            var message = args.Message.Body.ToObjectFromJson<Models.PullRequestCreatedEvent>(new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true
            });


            await args.CompleteMessageAsync(args.Message);

        }
        catch (Exception)
        {
            await args.DeadLetterMessageAsync(args.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await processor.StopProcessingAsync(cancellationToken);
        await processor.DisposeAsync();
    }


}