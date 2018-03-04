using UnityEngine;

public class RaycastController3D : MonoBehaviour
{
    public LayerMask collisionMask;
    public LayerMask worldBoundMask;

    public const float skinWidth = .015f;
    const float dstBetweenRays = .25f;
    [HideInInspector]
    public int horizontalRayCount;
    [HideInInspector]
    public int verticalRayCount;

    [HideInInspector]
    public float horizontalRaySpacing;
    [HideInInspector]
    public float verticalRaySpacing;

    [HideInInspector]
    public BoxCollider colliderCust;
    public RaycastOrigins raycastOrigins;

    public virtual void Awake()
    {
        colliderCust = GetComponent<BoxCollider>();
    }

    public virtual void Start()
    {
        CalculateRaySpacing();
    }

    public void UpdateRaycastOrigins()
    {
        Bounds bounds = colliderCust.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        raycastOrigins.bottomRight = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        raycastOrigins.topLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        raycastOrigins.topRight = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        raycastOrigins.zIndex = bounds.center.z;
    }

    public void CalculateRaySpacing()
    {
        Bounds bounds = colliderCust.bounds;
        bounds.Expand(skinWidth * -2);

        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;

        horizontalRayCount = Mathf.RoundToInt(boundsHeight / dstBetweenRays);
        verticalRayCount = Mathf.RoundToInt(boundsWidth / dstBetweenRays);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
        public float zIndex;
    }
}
