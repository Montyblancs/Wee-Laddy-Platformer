using UnityEngine;
using System.Collections;

// [RequireComponent(typeof(Camera))]
public class CameraFollow_v2 : MonoBehaviour
{

    public Camera thisCamera;
    public Player target;
    private Collider2D thisCollider;
    public float verticalOffset;
    public Vector2 focusAreaSize;
    [Range(0.01f, 1f)]
    public float focusShiftDeadzone = 0.5f;
    [Range(0.01f, 1f)]
    public float focusShiftAnchor = 0.4f;
    [Range(0.01f, 4f)]
    public float focusShiftIntensity = 1f;

    //Paralax Vars
    public GameObject BGParentObject;
    public GameObject bulletParentObject;
    public GameObject farEnemyContainer;
    [Range(0, 1)]
    public float BackgroundSpeed;

    FocusArea focusArea;

    void Awake()
    {
        thisCamera = GetComponent<Camera>();
        thisCollider = target.GetComponent<Collider2D>();
    }

    void Start()
    {
        focusArea = new FocusArea(thisCollider.bounds, focusAreaSize, focusShiftDeadzone, focusShiftAnchor, focusShiftIntensity, thisCamera, target);
        transform.position = new Vector3(thisCollider.bounds.center.x, thisCollider.bounds.center.y, -10f);
    }

    void LateUpdate()
    {
        focusArea.Update(thisCollider.bounds);

        // focus point in the focus area is on the same z plane as the target, so we can calculate distances properly.
        Vector3 cameraFocusPoint = (focusArea.focusPoint + Vector3.up * verticalOffset);
        Vector3 positionToFocus = cameraFocusPoint - transform.position;
        float snapDistance = 0.2f;

        // smoothly travel toward target position if we are not already there.
        if (positionToFocus.magnitude > snapDistance)
        {
            // Set the Camera to 10 units away from 0 in the z axis, which should be the background layer
            transform.position = Vector3.Lerp(transform.position, new Vector3(cameraFocusPoint.x, cameraFocusPoint.y, -10f), 0.4f);
        }
        else
        {
            transform.position = new Vector3(cameraFocusPoint.x, cameraFocusPoint.y, -10f);
        }

        //Parallax Scrolling
        cameraFocusPoint = new Vector3(transform.position.x * BackgroundSpeed, transform.position.y * BackgroundSpeed, 0.0f);
        BGParentObject.transform.position = cameraFocusPoint;
        farEnemyContainer.transform.position = cameraFocusPoint;
        bulletParentObject.transform.position = cameraFocusPoint;
    }

    void OnDrawGizmos()
    {
        if (enabled)
        {
            Gizmos.color = new Color(0, 0, 1, .5f);
            Gizmos.DrawCube(focusArea.center, focusAreaSize);
            Gizmos.color = new Color(1, 0, 0, .5f);
            Gizmos.DrawCube(focusArea.center, focusAreaSize * focusShiftDeadzone);
            Gizmos.color = new Color(0, 1, 0, .5f);
            Gizmos.DrawCube(focusArea.focusPoint, new Vector2(0.2f, 0.2f));
        }
    }

    struct FocusArea
    {
        public Vector2 center
        {
            get { return outerLimits.center; }
            set
            {
                outerLimits.center = (Vector3)value;
                innerLimits.center = (Vector3)value;
            }
        }
        public Vector2 size
        {
            get { return outerLimits.size; }
            set
            {
                outerLimits.size = (Vector3)value;
                innerLimits.size = (Vector3)value * deadzoneFraction;
            }
        }
        public Vector3 focusPoint
        {
            get { return focusCenterPoint; }
            set
            {
                // do nothing for now
            }
        }
        Camera targetCamera;
        Bounds innerLimits;
        Bounds outerLimits;
        Vector3 focusTriggerPoint;
        Vector3 focusCenterPoint;
        float deadzoneFraction;
        float shiftFraction;
        float shiftIntensity;
        Player target;

        public FocusArea(Bounds targetBounds, Vector2 size, float shiftDeadzone, float shiftAnchor, float shiftIntensityIn, Camera aCamera, Player inTarget)
        {
            targetCamera = aCamera;
            deadzoneFraction = shiftDeadzone;
            shiftFraction = shiftAnchor;
            shiftIntensity = shiftIntensityIn;
            focusTriggerPoint = targetBounds.center;
            focusCenterPoint = targetBounds.center;
            Vector3 limitSize = new Vector3(size.x, size.y, 1f);
            innerLimits = new Bounds(targetBounds.center, limitSize * deadzoneFraction);
            outerLimits = new Bounds(targetBounds.center, limitSize);
            target = inTarget;
        }

        private Vector3 calcFocusPoint(Bounds targetBounds)
        {
            // For now we will get the distance from target center to mouse. Later we may want to use target bounds (like innerLimits.ClosestPoint(Input.mousePosition))
            Vector3 mousePosition = targetCamera.ScreenToWorldPoint(Input.mousePosition);
            // for now we have to set z to the same as the target for the 2d plane. Later we should adapt to be usable in 3d.
            mousePosition.z = targetBounds.center.z;
            Vector3 focusPoint = Vector3.Lerp(targetBounds.center, mousePosition, shiftFraction);
            return focusPoint;
        }

        //Velocity is added here, at shiftX. Add the mouse calculations to shiftX?
        public void Update(Bounds targetBounds)
        {
            focusTriggerPoint = calcFocusPoint(targetBounds);

            float intersectDistance = 0f;

            innerLimits.center = targetBounds.center;
            outerLimits.center = targetBounds.center;
            if (innerLimits.Contains(focusTriggerPoint) != true)
            {
                Vector3 centerToFocusVector = (focusTriggerPoint - targetBounds.center);
                Ray focusToCenterRay = new Ray(focusTriggerPoint, -centerToFocusVector);
                Ray centerToFocusRay = new Ray(targetBounds.center, centerToFocusVector);
                Debug.DrawRay(centerToFocusRay.origin, centerToFocusVector, Color.green, 0);
                if (outerLimits.Contains(focusTriggerPoint) != true)
                {
                    // we want to limit the focus ray origin to the outer bounds, we set the origin of the ray to the bound intersect point.
                    if (outerLimits.IntersectRay(focusToCenterRay, out intersectDistance))
                    {
                        focusToCenterRay.origin = focusToCenterRay.GetPoint(intersectDistance);
                    }
                }
                // set the focus point to shift the distance from the edge of the bounds to the focus trigger.
                if (innerLimits.IntersectRay(focusToCenterRay, out intersectDistance))
                {
                    focusCenterPoint = Vector3.Lerp(targetBounds.center, centerToFocusRay.GetPoint(intersectDistance * shiftIntensity), 1f);
                }
            }
            else
            {
                // while in the inner region, the camera will focus on the player.
                focusCenterPoint = targetBounds.center;
            }
        }
    }

}
