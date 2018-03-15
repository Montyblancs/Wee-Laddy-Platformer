using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyController2D))]
public class Enemy : MonoBehaviour
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
    public GameObject enemyNearBulletParentContainer;
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

    EnemyController2D controller;

    BoxCollider2D thisCollider;
    BoxCollider2D playerCollider;

    Vector2 directionalInput;
    //bool wallSliding;
    //int wallDirX;

    [HideInInspector]
    public bool isDying = false;

    Renderer rend;

	[HideInInspector]
	public Animator enemyAnimator;
	[HideInInspector]
	public SpriteRenderer enemySpriteRender;

    void Start()
    {
        controller = GetComponent<EnemyController2D>();
        velocity.x = 0;
        velocity.y = 0;

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        rend = GetComponent<Renderer>();

        readyToFire = false;
        fireTimerStarted = false;
        objectAudio = GetComponent<AudioSource>();

        thisCollider = GetComponent<BoxCollider2D>();
        playerCollider = targetToChase.GetComponent<BoxCollider2D>();

		enemyAnimator = GetComponent<Animator>();
		enemySpriteRender = GetComponent<SpriteRenderer>();
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
                if (Vector3.Distance(targetToChase.transform.position, gameObject.transform.position) >= 6f)
                {
                    TowardsPlayer.x = -1f;
                }
                else if (Vector3.Distance(targetToChase.transform.position, gameObject.transform.position) <= 4f)
                {
                    TowardsPlayer.x = 1f;
                    //Check if player is touching
                    if (thisCollider.bounds.Intersects(playerCollider.bounds))
                    {
                        CharacterStats stats;
                        if (stats = targetToChase.GetComponent<CharacterStats>())
                        {
                            //Needs invuln state
                            stats.damage(damageOnTouch);
                        }
                    }
                }
            }
            else if (targetToChase.transform.position.x >= gameObject.transform.position.x)
            {
                if (Vector3.Distance(targetToChase.transform.position, gameObject.transform.position) >= 6f)
                {
                    TowardsPlayer.x = 1f;
                }
                else if (Vector3.Distance(targetToChase.transform.position, gameObject.transform.position) <= 4f)
                {
                    TowardsPlayer.x = -1f;
                    //Check if player is touching
                    if (thisCollider.bounds.Intersects(playerCollider.bounds))
                    {
                        CharacterStats stats;
                        if (stats = targetToChase.GetComponent<CharacterStats>())
                        {
                            //Needs invuln state
                            stats.damage(damageOnTouch);
                        }
                    }
                }
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

			TryToFire ();

			SetAnimatorParameters();
        }
        else
        {
            rend.enabled = false;
            if (isDying)
            {
                BoxCollider2D thisBox = gameObject.GetComponent<BoxCollider2D>();
                thisBox.enabled = false;
            }
        }
    }

	void TryToFire()
	{
		if (!fireTimerStarted && !readyToFire)
		{
			StartCoroutine(FireTimer(shootDelay));
		}
		else if (readyToFire)
		{
			//Fire Projectile at player
			var fireDirection = controller.collisions.faceDir;

			Vector3 bulletSpawnPoint = new Vector3(gameObject.transform.position.x + fireDirection - (0.5f * fireDirection), gameObject.transform.position.y, 5);
			Vector3 shotTarget = targetToChase.transform.position;

			Vector3 travelDirection;
			travelDirection.x = shotTarget.x - bulletSpawnPoint.x;
			travelDirection.y = shotTarget.y - bulletSpawnPoint.y;
			//z direction needs to be altered to hit background targets, 0 for foreground targets
			travelDirection.z = bulletSpawnPoint.z;
			travelDirection.Normalize();

			//50 is bullet speed * 5 here, pull from enemy shot type in the future
			RaycastHit2D wHit = Physics2D.Raycast(gameObject.transform.position, travelDirection, Time.deltaTime * 50, controller.collisionMask);
			if (!wHit)
			{
				GameObject thisProjectile = Instantiate(projectileType, bulletSpawnPoint, Quaternion.identity);
				thisProjectile.transform.parent = enemyNearBulletParentContainer.transform;

				ProjectileController projectileScript = thisProjectile.GetComponent<ProjectileController>();

				projectileScript.targetCoords = shotTarget;

				objectAudio.PlayOneShot(shotSound, 0.3f);

				readyToFire = false;
				StartCoroutine(FireTimer(shootDelay));
			}
			else
			{
				//Try to shoot again with a miniscule timer
				readyToFire = false;
				StartCoroutine(FireTimer(0.5f));
			}
		}
	}

	private void SetAnimatorParameters()
	{
		enemyAnimator.SetFloat("x_velocity", Mathf.Abs(velocity.x));
		enemyAnimator.SetFloat("y_velocity", velocity.y);
		enemyAnimator.SetBool("on_ground", controller.collisions.below);
		enemySpriteRender.flipX = (controller.collisions.faceDir != 1) ? true : false;
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
