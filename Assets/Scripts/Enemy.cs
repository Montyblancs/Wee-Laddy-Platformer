using UnityEngine;

[RequireComponent(typeof(EnemyController2D))]
public class Enemy : MonoBehaviour
{

    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    public GameObject targetToChase;
    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;
    float moveSpeed = 6;

    float timeToWallUnstick;

    public AudioClip[] deathSounds;

    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing;

    EnemyController2D controller;

    Vector2 directionalInput;
    bool wallSliding;
    int wallDirX;

    [HideInInspector]
    public bool isDying = false;

    Renderer rend;

    void Start()
    {
        controller = GetComponent<EnemyController2D>();
        velocity.x = 0;
        velocity.y = 0;

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        Vector3 inView = Camera.main.WorldToViewportPoint(gameObject.transform.position);
        if (inView.x > -0.15f && inView.x < 1.15f && inView.y > -0.15f && inView.y < 1.15f && !isDying)
        {
            rend.enabled = true;
            CalculateVelocity();
            Vector2 TowardsPlayer = new Vector2(0, 0);

            if (targetToChase.transform.position.x < gameObject.transform.position.x)
            {
                TowardsPlayer.x = -1f;
            }
            else
            {
                TowardsPlayer.x = 1f;
            }

            SetDirectionalInput(TowardsPlayer);

            controller.Move(velocity * Time.deltaTime, directionalInput);

            if (controller.collisions.above || controller.collisions.below)
            {
                if (controller.collisions.slidingDownMaxSlope)
                {
                    velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
                }
                else
                {
                    velocity.y = 0;
                }
            }
        } else
        {
            rend.enabled = false;
            if (isDying)
            {
                BoxCollider2D thisBox = gameObject.GetComponent<BoxCollider2D>();
                thisBox.enabled = false;
            }
        }
    }

    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }

    public void OnJumpInputDown()
    {
        if (controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope)
            {
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                { // not jumping against max slope
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                }
            }
            else
            {
                velocity.y = maxJumpVelocity;
            }
        }
    }

    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

    void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
        velocity.y += gravity * Time.deltaTime;
    }
}
