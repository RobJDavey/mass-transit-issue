using MassTransit;
using MassTransitIssue.Messages;

namespace MassTransitIssue;

public class TestStateMachine : MassTransitStateMachine<TestSaga>
{
    public TestStateMachine()
    {
        InstanceState(s => s.State);

        Event(() => Start, e => e
            .CorrelateById(c => c.Message.Id));

        Event(() => Cancel, e => e
            .CorrelateById(c => c.Message.Id));

        Event(() => Trigger, e => e
            .CorrelateById(c => c.Message.Id));

        Schedule(() => TriggerSchedule, saga => saga.TriggerTokenId, cfg =>
        {
            cfg.Delay = TimeSpan.FromMinutes(10);
        });

        Initially(
            When(Start)
                .TransitionTo(AwaitingActivity)
                .Schedule(TriggerSchedule, c => c.Init<Trigger>(new Trigger
                {
                    Id = c.Saga.CorrelationId,
                })));

        During(AwaitingActivity,
            When(Cancel)
                .Finalize(),
            When(Trigger)
                .Publish(c => new Notify
                {
                    Id = c.Saga.CorrelationId,
                })
                .Finalize());

        SetCompletedWhenFinalized();
    }

    public State AwaitingActivity { get; } = default!;

    public Schedule<TestSaga, Trigger> TriggerSchedule { get; } = null!;

    public Event<Start> Start { get; } = default!;

    public Event<Cancel> Cancel { get; } = default!;

    public Event<Trigger> Trigger { get; } = default!;
}
