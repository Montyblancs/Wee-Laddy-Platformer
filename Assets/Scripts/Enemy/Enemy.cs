using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyController2D))]
public class Enemy : MonoBehaviour
{

    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    public GameObject targetToChase;
    public float accelerationTimeAirborne = .2f;
    public float accelerationTimeGrounded = .1f;
    private float moveSpeed = 6;
    public float shootDelay = 5;
    public GameObject projectileType;
    public GameObject enemyNearBulletParentContainer;
    public Camera mainCam;
    public float damageOnTouch;
	public float deadBodyLastTime;

    bool readyToFire;
    bool fireTimerStarted;
	bool DeadBodyTimerStarted;
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

	CharacterStats stats;
	[HideInInspector]
	public bool statCanMove = true;
	[HideInInspector]
	public bool statCanFire = true;

	// holds a list of all active coroutines started by this object
	private List<string> activeCoroutines = new List<string> { };

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
		DeadBodyTimerStarted = false;
        objectAudio = GetComponent<AudioSource>();

        thisCollider = GetComponent<BoxCollider2D>();
        playerCollider = targetToChase.GetComponent<BoxCollider2D>();

		enemyAnimator = GetComponent<Animator>();
		enemySpriteRender = GetComponent<SpriteRenderer>();

		stats = GetComponent<CharacterStats>();

		// start checking the player stats so we can have the player reflect the condition
		StartCoroutine("MonitorCondition");
    }

    void Update()
    {
        Vector3 inView = Camera.main.WorldToViewportPoint(gameObject.transform.position);
		if (inView.x > -0.15f && inView.x < 1.15f && inView.y > -0.15f && inView.y < 1.15f)
        {
            rend.enabled = true;
            CalculateVelocity();
			if (!stats.isDead()){
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
				TryToFire ();
			}
				
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

			SetAnimatorParameters();
        }
        else
        {
            rend.enabled = false;
        }
    }

	public void OnEnable()
	{
		// start the ConditionMonitor Coroutine if its not already started.
		if (!this.activeCoroutines.Contains("MonitorCondition"))
		{
			// if stats component has not been fetched, don't bother.
			if (stats)
			{
				StartCoroutine("MonitorCondition");
			}
		}
	}

	public void OnDisable()
	{
		// make sure to stop monitoring conditio
		StopCoroutine("MonitorCondition");
		// remove the coroutine from the list.
		this.activeCoroutines.Remove("MonitorCondition");
	}

	// monitor life and death based on the CharacterStats
	IEnumerator MonitorCondition()
	{
		// don't bother monitoring if the CharacterStats component is not set
		if (!stats)
		{
			Debug.Log("The CharacterStats component for this player has not been set yet.");
			yield break;
		}
		// Add this coroutine to the list of active Coroutines
		this.activeCoroutines.Add("MonitorCondition");
		// we will assume any conditions have yet been applied when this coroutine started.
		ConditionType appliedCondition = stats.Condition;
		// Start looping until the coroutine is manually stopped.
		while (true)
		{
			// check if they were not yet dead, but they need to be.
			if (stats.isDead() && appliedCondition != ConditionType.DEAD)
			{
				// start by making sure the character is dead according to the CharacterStats
				if (stats && !stats.isDead())
				{
					stats.kill();
				}
				// disable all functionality
				statCanMove = false;
				statCanFire = false;
				//change layer so bullets can pass through
				gameObject.layer = 0;
				// set the new applied condition
				appliedCondition = stats.Condition;
				//Throw Animator trigger
				enemyAnimator.SetTrigger("has_died");
				// wait a few seconds before being able to live or die again
				StartCoroutine(DeadBodyTimer(deadBodyLastTime));

				yield return new WaitForSeconds(2f);
				// if they aren't dead and they previously were, well... bring um back.
			}
			else if (stats.isAlive() && appliedCondition == ConditionType.DEAD)
			{
				// start by making sure the character is alive according to the CharacterStats
				if (stats && !stats.isAlive())
				{
					stats.revive();
				}
				// enable all functionality
				statCanMove = true;
				statCanFire = true;
				// set the new applied condition
				appliedCondition = stats.Condition;
				// wait a few seconds before being able to revive again
				yield return new WaitForSeconds(2f);
			}
			// Make player speed reflect the speed stat
			if (this.moveSpeed != stats.SPD)
			{
				this.moveSpeed = stats.SPD;
			}
			// only poll on a set interval, for now every tenth of a second
			yield return new WaitForSeconds(0.1f);
		}
		// NOTE: whenever this coroutine is stopped, we should remove it from the coroutine list like below:
		// this.activeCoroutines.Remove("MonitorCondition");
	}

	// passes kill to the CharacterStats component
	public void kill()
	{
		if (this.stats)
		{
			this.stats.kill();
		}
	}

	// passes revive to the CharacterStats component
	public void revive()
	{
		if (this.stats)
		{
			this.stats.revive();
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

	private IEnumerator DeadBodyTimer(float duration)
	{
		if (!DeadBodyTimerStarted)
		{
			DeadBodyTimerStarted = true;
			yield return new WaitForSeconds(duration);
			Destroy (gameObject);
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
		if (statCanMove) {
			velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
		} else {
			velocity.x = 0;
		}      
        velocity.y += gravity * Time.deltaTime;
    }
}
