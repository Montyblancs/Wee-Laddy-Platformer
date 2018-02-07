using UnityEngine;

public class CasingController : MonoBehaviour
{

    public Vector3 velocity;
    public float moveSpeed;
    public float timeToLive = 3f;
    public LayerMask platformLayer;
    private BoxCollider2D thisCollider;
    private bool stopMoving = false;

    // Use this for initialization
    void Start()
    {
        //Use https://docs.unity3d.com/ScriptReference/Collider2D.IsTouchingLayers.html
        //Collider2D.IsTouchingLayers
        thisCollider = GetComponent<BoxCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //Remove bullet if not on screen
        Vector3 inView = Camera.main.WorldToViewportPoint(gameObject.transform.position);
        if (inView.x < -0.1 || inView.x > 1.1 || inView.y < -0.1 || inView.y > 1.1) { Destroy(gameObject); }

        timeToLive = timeToLive - Time.deltaTime;
        if (timeToLive <= 0)
        {
            Destroy(gameObject);
        }

        if (!stopMoving)
        {
            velocity.y = velocity.y + (moveSpeed * -1);
            RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, Vector2.down, -0.5f, platformLayer);
            if (hit)
            {
                Vector3 newPos = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y + hit.distance);
                transform.position = newPos;
                stopMoving = true;
            }
            else
            {
                transform.Translate(velocity * Time.fixedDeltaTime);
            }


        }

    }
}
