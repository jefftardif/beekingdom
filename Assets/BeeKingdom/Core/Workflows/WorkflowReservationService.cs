using System.Collections.Generic;

namespace BeeKingdom.Core.Workflows
{
    public sealed class WorkflowReservationService
    {
        private readonly HashSet<string> reservations = new HashSet<string>();

        public bool Reserve(GameplayWorkflowInstance instance)
        {
            for (int i = 0; i < instance.Definition.RequiredReservations.Count; i++)
            {
                if (reservations.Contains(instance.Definition.RequiredReservations[i])) return false;
            }
            for (int i = 0; i < instance.Definition.RequiredReservations.Count; i++)
            {
                reservations.Add(instance.Definition.RequiredReservations[i]);
            }
            return true;
        }

        public void Release(GameplayWorkflowInstance instance)
        {
            for (int i = 0; i < instance.Definition.RequiredReservations.Count; i++)
            {
                reservations.Remove(instance.Definition.RequiredReservations[i]);
            }
        }
    }
}
