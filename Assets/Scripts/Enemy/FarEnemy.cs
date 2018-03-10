using System.Collections;
using UnityEngine;

public class FarEnemy : MonoBehaviour
{
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    public GameObject targetToChase;
    public float accelerationTimeAirborne = .2f;
    public float accelerationTimeGrounded = .1f;
    public float moveSpeed = 6;
    public float shootDelay = 5;
    public GameObject projectileType;
    public GameObject enemyFarBulletParentContainer;
    public Camera mainCam;
    public float damageOnTouch;

    bool readyToFire;
    bool fireTimerStarted;
    //float timeToWallUnstick;

    AudioSource objectAudio;
    public AudioClip shotSound;

    public AudioClip[] deathSounds;

    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing;

    EnemyController3D controller;

    BoxCollider thisCollider;
    BoxCollider2D playerCollider;

    Vector2 directionalInput;
    //bool wallSliding;
    //int wallDirX;

    [HideInInspector]
    public bool isDying = false;

    Renderer rend;

    //TODO : Mouse movement that causes the camera to pan up throws farEnemies through platforms
    //Proposal : Include container of far enemy in camera script, shift the same as the special shift for background objects.

    void Start()
    {
        controller = GetComponent<EnemyController3D>();
        velocity.x = 0;
        velocity.y = 0;

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        rend = GetComponent<Renderer>();

        readyToFire = false;
        fireTimerStarted = false;
        objectAudio = GetComponent<AudioSource>();

        thisCollider = GetComponent<BoxCollider>();
        playerCollider = targetToChase.GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        Vector3 inView = Camera.main.WorldToViewportPoint(gameObject.transform.position);
        if (inView.x > -0.15f && inView.x < 1.15f && inView.y > -0.15f && inView.y < 1.15f && !isDying)
        {
            rend.enabled = true;
            CalculateVelocity();

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

            if (!fireTimerStarted && !readyToFire)
            {
                StartCoroutine(FireTimer(shootDelay));
            }
            else if (readyToFire)
            {
                //Fire Projectile at player
                var fireDirection = controller.collisions.faceDir;

                Vector3 bulletSpawnPoint = gameObject.transform.position;
                Vector3 shotTarget = targetToChase.transform.position;
                //shotTarget.z = 5;

                GameObject thisProjectile = Instantiate(projectileType, bulletSpawnPoint, Quaternion.identity);
                thisProjectile.transform.parent = enemyFarBulletParentContainer.transform;

                ProjectileController projectileScript = thisProjectile.GetComponent<ProjectileController>();

                projectileScript.targetCoords = shotTarget;
                projectileScript.playerBoxCollider = playerCollider;

                objectAudio.PlayOneShot(shotSound, 0.3f);

                readyToFire = false;
                StartCoroutine(FireTimer(shootDelay));
            }
        }
        else
        {
            rend.enabled = false;
            if (isDying)
            {
                BoxCollider thisBox = gameObject.GetComponent<BoxCollider>();
                thisBox.enabled = false;
            }
        }
    }

    private IEnumerator FireTimer(float duration)
    {
        if (!fireTimerStarted)
        {
            fireTimerStarted = true;
            yield return new WaitForSeconds(duration);
            fireTimerStarted = false;
            readyToFire = true;
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

