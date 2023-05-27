using MassTransit;

namespace MassTransitIssue;

public class TestSaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string? State { get; set; }
    public Guid? TriggerTokenId { get; set; }
}
