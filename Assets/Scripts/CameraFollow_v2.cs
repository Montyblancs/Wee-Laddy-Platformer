using UnityEngine;
using System.Collections;

// [RequireComponent(typeof(Camera))]
public class CameraFollow_v2 : MonoBehaviour
{

    public Camera thisCamera;
    public Controller2D target;
    public float verticalOffset;
    public Vector2 focusAreaSize;
    [Range(0.01f, 1f)]
    public float focusMoveFraction;

    //Paralax Vars
    public GameObject BGParentObject;
    public GameObject bulletParentObject;
    [Range(0, 1)]
    public float BackgroundSpeed;

    FocusArea focusArea;

    void Awake()
    {
        thisCamera = GetComponent<Camera>();
    }

    void Start()
    {
        focusArea = new FocusArea(target.collider.bounds, focusAreaSize, focusMoveFraction, thisCamera);
    }

    void LateUpdate()
    {
        focusArea.Update(target.collider.bounds);

        Vector3 focusPosition = (Vector3)(focusArea.center + Vector2.up * verticalOffset);

        transform.position = Vector3.Lerp(target.transform.position, focusPosition, 1f) + Vector3.forward * -10;

        //Parallax Scrolling
        focusPosition.x = focusPosition.x * BackgroundSpeed;
        focusPosition.y = focusPosition.y * BackgroundSpeed;
        BGParentObject.transform.position = focusPosition + Vector3.forward * 0;
        bulletParentObject.transform.position = focusPosition;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0, 1, .5f);
        Gizmos.DrawCube(focusArea.center, focusAreaSize);
        Gizmos.color = new Color(0, 1, 0, .5f);
        Gizmos.DrawCube(focusArea.focusPoint, new Vector2(0.2f, 0.2f));
        Gizmos.color = new Color(1, 0, 0, .5f);
        Gizmos.DrawCube(focusArea.center, focusAreaSize*focusMoveFraction);
    }

    struct FocusArea
    {
        public Vector2 center
        {
            get { return outerLimits.center; }
            set {
                outerLimits.center = (Vector3)value;
                innerLimits.center = (Vector3)value;
            }
        }
        public Vector2 size
        {
            get { return outerLimits.size; }
            set {
                outerLimits.size = (Vector3)value;
                innerLimits.size = (Vector3)value*fractionToMoveFocus;
            }
        }
        public Vector3 focusPoint
        {
            get { return focusTriggerPoint; }
            set {
                // do nothing for now
            }
        }
        Camera targetCamera;
        Bounds innerLimits;
        Bounds outerLimits;
        Vector3 focusTriggerPoint;
        float fractionToMoveFocus;
        float left, right;
        float top, bottom;

        public FocusArea(Bounds targetBounds, Vector2 size, float focusMoveFraction, Camera aCamera)
        {
            targetCamera = aCamera;
            fractionToMoveFocus = focusMoveFraction;
            focusTriggerPoint = targetBounds.center;
            Vector3 limitSize = new Vector3(size.x, size.y, 1f);
            innerLimits = new Bounds(targetBounds.center, limitSize*focusMoveFraction);
            outerLimits = new Bounds(targetBounds.center, limitSize);
            left = outerLimits.min.x;
            right = outerLimits.max.x;
            bottom = outerLimits.min.y;
            top = outerLimits.max.y;
        }

        private Vector3 calcFocusPoint(Bounds targetBounds)
        {
            // For now we will get the distance from target center to mouse. Later we may want to use target bounds (like innerLimits.ClosestPoint(Input.mousePosition))
            Vector3 mousePosition = targetCamera.ScreenToWorldPoint(Input.mousePosition);
            // for now we have to set z to the same as the target for the 2d plane. Later we should adapt to be usable in 3d.
            mousePosition.z = targetBounds.center.z;
            Vector3 focusPoint = Vector3.Lerp(targetBounds.center, mousePosition, fractionToMoveFocus);
            return focusPoint;
        }

        //Velocity is added here, at shiftX. Add the mouse calculations to shiftX?
        public void Update(Bounds targetBounds)
        {
            focusTriggerPoint = calcFocusPoint(targetBounds);

            float intersectDistance = 0f;
            if (innerLimits.Contains(focusTriggerPoint) != true) {
                Ray focusToCenterRay = new Ray(focusTriggerPoint, (targetBounds.center - focusTriggerPoint));
                Ray centerToFocusRay = new Ray(targetBounds.center, (focusTriggerPoint - targetBounds.center));
                Debug.DrawRay(centerToFocusRay.origin, (focusTriggerPoint - targetBounds.center), Color.green, 0);
                if (outerLimits.Contains(focusTriggerPoint) != true) {
                    // we want to limit the focus ray origin to the outer bounds, we set the origin of the ray to the bound intersect point.
                    if (outerLimits.IntersectRay(focusToCenterRay, out intersectDistance)) {
                        focusToCenterRay = new Ray(focusToCenterRay.GetPoint(intersectDistance), (targetBounds.center - focusTriggerPoint));
                    }
                }
                // set the center of the limits to move a fraction of the vector bewteen the inner limit intersect point and the focus point.
                if (innerLimits.IntersectRay(focusToCenterRay, out intersectDistance)) {
                    innerLimits.center = Vector3.Lerp(targetBounds.center, centerToFocusRay.GetPoint(intersectDistance), 1f);
                }
                outerLimits.center = innerLimits.center;
            }
        }
    }

}
