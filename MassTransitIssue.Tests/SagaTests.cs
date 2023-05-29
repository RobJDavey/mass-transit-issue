using MassTransit;
using MassTransit.QuartzIntegration;
using MassTransit.Testing;
using MassTransitIssue.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace MassTransitIssue.Tests;

[TestCaseOrderer("MassTransitIssue.Tests.TestCaseOrdererAscending", "MassTransitIssue.Tests")]
// [TestCaseOrderer("MassTransitIssue.Tests.TestCaseOrdererDescending", "MassTransitIssue.Tests")]
public class SagaTestsBase
{
    [Fact]
    public async Task TestCancelled()
    {
        await using var serviceProvider = CreateServiceProvider();
        var harness = serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var id = NewId.NextGuid();

        await harness.Bus.Publish(new Start
        {
            Id = id,
        });

        Assert.True(await harness.Consumed.Any<Start>(), "Bus - start not consumed");

        var sagaHarness = harness.GetSagaStateMachineHarness<TestStateMachine, TestSaga>();

        Assert.True(await sagaHarness.Consumed.Any<Start>(), "Saga - start not consumed");
        Assert.True(await sagaHarness.Created.Any(s => s.CorrelationId == id), "Saga - not created");

        await harness.Bus.Publish(new Cancel
        {
            Id = id,
        });

        Assert.True(await sagaHarness.Consumed.Any<Cancel>(), "Saga - cancel not consumed");

        using var adjustment = new QuartzTimeAdjustment(serviceProvider);
        await adjustment.AdvanceTime(TimeSpan.FromMinutes(10));

        Assert.False(await sagaHarness.Consumed.Any<Trigger>(), "Saga - Trigger Received");
        Assert.False(await harness.Published.Any<Notify>(), "Bus - Notify published");
    }

    [Fact]
    public async Task TestNotify()
    {
        await using var serviceProvider = CreateServiceProvider();
        var harness = serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var id = NewId.NextGuid();

        await harness.Bus.Publish(new Start
        {
            Id = id,
        });

        Assert.True(await harness.Consumed.Any<Start>(), "Bus - start not consumed");

        var sagaHarness = harness.GetSagaStateMachineHarness<TestStateMachine, TestSaga>();

        Assert.True(await sagaHarness.Consumed.Any<Start>(), "Saga - start not consumed");
        Assert.True(await sagaHarness.Created.Any(s => s.CorrelationId == id), "Saga - not created");

        using var adjustment = new QuartzTimeAdjustment(serviceProvider);
        await adjustment.AdvanceTime(TimeSpan.FromMinutes(10));

        Assert.True(await sagaHarness.Consumed.Any<Trigger>(), "Saga - Trigger not Received");
        Assert.True(await harness.Published.Any<Notify>(), "Bus - Notify not published");
    }

    private static ServiceProvider CreateServiceProvider()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .Build();

        IServiceCollection services = new ServiceCollection()
            .AddSingleton(configuration);

        services.AddQuartz(static x =>
        {
            x.UseMicrosoftDependencyInjectionJobFactory();
        });

        services.AddMassTransitTestHarness(static x =>
        {
            x.SetInMemorySagaRepositoryProvider();
            x.AddQuartzConsumers();
            x.AddPublishMessageScheduler();

            x.AddSagaStateMachine<TestStateMachine, TestSaga>(static saga =>
            {
                saga.UseInMemoryOutbox();
            });

            x.UsingInMemory(static (ctx, cfg) =>
            {
                cfg.UsePublishMessageScheduler();
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services.BuildServiceProvider(true);
    }
}
