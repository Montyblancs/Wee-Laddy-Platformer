using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public Controller2D target;
    public float verticalOffset;
    public float lookAheadDstX;
    public float lookSmoothTimeX;
    public float verticalSmoothTime;
    public Vector2 focusAreaSize;

    //Paralax Vars
    public GameObject BGParentObject;
    public GameObject bulletParentObject;
    [Range(0, 1)]
    public float BackgroundSpeed;

    FocusArea focusArea;

    float currentLookAheadX;
    float targetLookAheadX;
    float lookAheadDirX;
    float smoothLookVelocityX;
    float smoothVelocityY;

    bool lookAheadStopped;

    void Start()
    {
        focusArea = new FocusArea(target.colliderCust.bounds, focusAreaSize);
    }

    void LateUpdate()
    {
        focusArea.Update(target.colliderCust.bounds);

        Vector2 focusPosition = focusArea.centre + Vector2.up * verticalOffset;

        if (focusArea.velocity.x != 0)
        {
            lookAheadDirX = Mathf.Sign(focusArea.velocity.x);
            if (Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) && target.playerInput.x != 0)
            {
                lookAheadStopped = false;
                targetLookAheadX = lookAheadDirX * lookAheadDstX;
            }
            else
            {
                if (!lookAheadStopped)
                {
                    lookAheadStopped = true;
                    targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookAheadDstX - currentLookAheadX) / 4f;
                }
            }
        }

        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);

        focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);
        focusPosition += Vector2.right * currentLookAheadX;
        transform.position = (Vector3)focusPosition + Vector3.forward * -10;

        //Parallax Scrolling
        focusPosition.x = focusPosition.x * BackgroundSpeed;
        focusPosition.y = focusPosition.y * BackgroundSpeed;
        BGParentObject.transform.position = (Vector3)focusPosition + Vector3.forward * 0;
        bulletParentObject.transform.position = (Vector3)focusPosition;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, .5f);
        Gizmos.DrawCube(focusArea.centre, focusAreaSize);
    }

    struct FocusArea
    {
        public Vector2 centre;
        public Vector2 velocity;
        float left, right;
        float top, bottom;
        float maxMouseRangex, maxMouseRangey;
        bool onLeft, onRight;


        public FocusArea(Bounds targetBounds, Vector2 size)
        {
            left = targetBounds.center.x - size.x / 2;
            right = targetBounds.center.x + size.x / 2;
            bottom = targetBounds.min.y;
            top = targetBounds.min.y + size.y;

            maxMouseRangex = size.x;
            maxMouseRangey = size.y;

            onLeft = false;
            onRight = false;

            velocity = Vector2.zero;
            centre = new Vector2((left + right) / 2, (top + bottom) / 2);
        }

        //Velocity is added here, at shiftX. Add the mouse calculations to shiftX?
        public void Update(Bounds targetBounds)
        {
            //Add max of 1f to left/right depending on mouse cursor
            float mouseX = Input.mousePosition.x;
            if (mouseX < 0)
            {
                mouseX = 0;
            }
            else if (mouseX > Screen.width)
            {
                mouseX = Screen.width;
            }

            Vector2 DistanceFromCenter = new Vector2(mouseX - (Screen.width / 2), Input.mousePosition.y - (Screen.height / 2));
            //current / total results in 0-1f range
            //Left side = current/midpoint | Right side = current/Screen width
            float minX = targetBounds.min.x;
            float maxX = targetBounds.max.x;
            //TargetBounds = The player's coordinates (not size)
            //left/right = The container's edges

            //Debug.Log("Max X:" + maxX + "|right:" + right);
            if (maxX >= right)
            {
                onRight = true;
                onLeft = false;
            }
            else if (minX <= left)
            {
                onLeft = true;
                onRight = false;
            } else
            {
                onLeft = false;
                onRight = false;
            }

            //Debug.Log("L:" + onLeft + "|R:" + onRight);

            /*Comment out these lines to disable shaking camera*/

            //if (DistanceFromCenter.x <= 0)
            //{
            //    //Left Side
            //    float Result = (Input.mousePosition.x / (Screen.width / 2) - 1f) * maxMouseRangex;
            //    minX += Result;
            //}
            //else
            //{
            //    //Right side 
            //    float Result = (Input.mousePosition.x / (Screen.width / 2) - 1f) * maxMouseRangex;
            //    maxX += Result;
            //}
            /*Don't touch anything past this line*/

            //Left side stops fine because left is checked first. Dynamically change order based on bound location?
            float shiftX = 0;
            if (minX < left && !onRight)
            {
                shiftX = minX - left;
                //Debug.Log("FireLeft");
            }
            else if (maxX > right && !onLeft)
            {
                shiftX = maxX - right;
                //Debug.Log("FireRight");
            }
            left += shiftX;
            right += shiftX;

            float minY = targetBounds.min.y;
            float maxY = targetBounds.max.y;
            //Debug.Log(DistanceFromCenter);
            //if (DistanceFromCenter.y <= 0)
            //{
            //    //Bottom Half
            //    minY += (Input.mousePosition.y / (Screen.height / 2) - 1f) * maxMouseRangey;
            //}
            //else
            //{
            //    //Top Half
            //    maxY += (Input.mousePosition.y / (Screen.height) - 1f) * maxMouseRangey;
            //}

            float shiftY = 0;
            if (minY < bottom)
            {
                shiftY = minY - bottom;
            }
            else if (maxY > top)
            {
                shiftY = maxY - top;
            }
            top += shiftY;
            bottom += shiftY;
            centre = new Vector2((left + right) / 2, (top + bottom) / 2);
            velocity = new Vector2(shiftX, shiftY);
        }
    }

}
