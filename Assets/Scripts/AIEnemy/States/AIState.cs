using UnityEngine;

// State objects share the controller's data; they are not scene components.
public abstract class AIState
{
    protected AIController Controller { get; }

    protected AIState(AIController controller)
    {
        Controller = controller;
    }

    public abstract void Tick(Vector3 toTarget, float distance);
}
