using System.Collections.Generic;
using UnityEngine;

/// <summary>Authoring data only. Colliders are baked by the Editor; no runtime generation.</summary>
public sealed class ShorelineBoundaryPath : MonoBehaviour
{
    public List<Vector3> controlPoints = new List<Vector3>();
    public bool closed;
    [Tooltip("Land lies on the right when following the control points.")]
    public bool landOnRight = true;
    public float bottom = -30;
    public float top = 35;
    [Min(1)] public float thickness = 20;
    [Min(1)] public int edgesPerSection = 4;
    [Min(0)] public float landSideOverlap = 1;
    public PhysicsMaterial material;
    [HideInInspector] public string generatedAssetFolder;
    [HideInInspector] public Transform generatedRoot;
}
