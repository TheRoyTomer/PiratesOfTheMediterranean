using UnityEngine;

public class PatrolPoint : MonoBehaviour
{
    [SerializeField] private PatrolPoint[] connectedPoints;

    public PatrolPoint[] ConnectedPoints => connectedPoints;
}