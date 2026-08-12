using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Core.Workflows
{
    public sealed class GameplayWorkflowManager
    {
        private readonly Dictionary<long, GameplayWorkflowInstance> instances = new Dictionary<long, GameplayWorkflowInstance>();
        private readonly WorkflowScheduler scheduler = new WorkflowScheduler();
        private readonly WorkflowValidator validator = new WorkflowValidator();
        private readonly WorkflowReservationService reservations = new WorkflowReservationService();
        private readonly WorkflowExecutor executor = new WorkflowExecutor();
        private readonly IEventBus eventBus;
        private long nextHandle = 1;
        private long nextSequence = 1;

        public WorkflowDiagnostics Diagnostics { get; } = new WorkflowDiagnostics();

        public GameplayWorkflowManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public GameplayWorkflowInstance RequestWorkflow(GameplayWorkflowDefinition definition)
        {
            GameplayWorkflowInstance instance = new GameplayWorkflowInstance(nextHandle++, definition, nextSequence++);
            instances.Add(instance.Handle, instance);
            Diagnostics.RecordRequested();
            eventBus?.Publish(new WorkflowRequested(instance.Handle));
            return instance;
        }

        public bool ValidateWorkflow(long handle)
        {
            GameplayWorkflowInstance instance = instances[handle];
            bool valid = validator.Validate(instance) && instance.ChangeState(WorkflowState.Validated);
            if (valid) eventBus?.Publish(new WorkflowValidated(handle));
            else Diagnostics.RecordFailed();
            return valid;
        }

        public bool QueueWorkflow(long handle)
        {
            GameplayWorkflowInstance instance = instances[handle];
            if (!instance.ChangeState(WorkflowState.Queued)) return false;
            scheduler.Queue(instance);
            eventBus?.Publish(new WorkflowQueued(handle));
            return true;
        }

        public bool ExecuteWorkflow()
        {
            GameplayWorkflowInstance instance = scheduler.DequeueNext();
            if (instance == null) return false;
            if (!reservations.Reserve(instance) || !instance.ChangeState(WorkflowState.Reserved))
            {
                instance.ChangeState(WorkflowState.Failed);
                Diagnostics.RecordFailed();
                eventBus?.Publish(new WorkflowFailed(instance.Handle));
                return false;
            }
            eventBus?.Publish(new WorkflowStarted(instance.Handle));
            bool executed = executor.Execute(instance);
            reservations.Release(instance);
            if (executed)
            {
                Diagnostics.RecordCompleted();
                eventBus?.Publish(new WorkflowCompleted(instance.Handle));
            }
            return executed;
        }

        public bool CancelWorkflow(long handle)
        {
            bool ok = instances[handle].ChangeState(WorkflowState.Cancelled);
            if (ok) { Diagnostics.RecordCancelled(); eventBus?.Publish(new WorkflowCancelled(handle)); }
            return ok;
        }

        public bool InterruptWorkflow(long handle)
        {
            bool ok = instances[handle].ChangeState(WorkflowState.Interrupted);
            if (ok) { Diagnostics.RecordInterrupted(); eventBus?.Publish(new WorkflowInterrupted(handle)); }
            return ok;
        }

        public bool ResumeWorkflow(long handle)
        {
            GameplayWorkflowInstance instance = instances[handle];
            if (instance.State == WorkflowState.Suspended && instance.ChangeState(WorkflowState.Queued))
            {
                scheduler.Queue(instance);
                return true;
            }
            if (instance.State == WorkflowState.Interrupted && instance.ChangeState(WorkflowState.Retrying) && instance.ChangeState(WorkflowState.Queued))
            {
                scheduler.Queue(instance);
                return true;
            }
            return false;
        }

        public IReadOnlyList<GameplayWorkflowInstance> QueryActiveWorkflows()
        {
            List<GameplayWorkflowInstance> active = new List<GameplayWorkflowInstance>();
            foreach (GameplayWorkflowInstance instance in instances.Values)
            {
                if (instance.State != WorkflowState.Completed && instance.State != WorkflowState.Cancelled && instance.State != WorkflowState.Failed)
                {
                    active.Add(instance);
                }
            }
            return active;
        }
    }
}
