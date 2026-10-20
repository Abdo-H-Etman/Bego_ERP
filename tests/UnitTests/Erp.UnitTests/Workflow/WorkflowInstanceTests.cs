using Erp.Modules.Workflow;
using FluentAssertions;
using Xunit;

namespace Erp.UnitTests.Workflow;

public class WorkflowInstanceTests
{
    [Fact]
    public void WorkflowInstance_Should_RecordTransitionsAndAuditHistory()
    {
        var tenantId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var instance = new WorkflowInstance(Guid.NewGuid(), tenantId, "invoice_approval", "Invoice", entityId, "DRAFT");

        instance.CurrentState.Should().Be("DRAFT");
        instance.History.Should().BeEmpty();

        instance.TransitionTo("PENDING_APPROVAL", "SubmitForApproval", "user1@erp.com", "Submitted for review");
        instance.CurrentState.Should().Be("PENDING_APPROVAL");
        instance.History.Should().HaveCount(1);

        var first = instance.History.First();
        first.FromState.Should().Be("DRAFT");
        first.ToState.Should().Be("PENDING_APPROVAL");
        first.ActionName.Should().Be("SubmitForApproval");
        first.PerformedBy.Should().Be("user1@erp.com");
    }
}
