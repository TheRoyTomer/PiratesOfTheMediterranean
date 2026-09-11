using UnityEngine;

public class FiringDirectionController : MonoBehaviour
{
    [SerializeField] private FiringDirection selectedDirection = FiringDirection.Front;

    public FiringDirection SelectedDirection => selectedDirection;
    

    public void CycleDirection()
    {
        selectedDirection = selectedDirection switch
        {
            FiringDirection.Front => FiringDirection.Right,
            FiringDirection.Right => FiringDirection.Left,
            FiringDirection.Left => FiringDirection.Front,
            _ => FiringDirection.Front
        };
    }
}