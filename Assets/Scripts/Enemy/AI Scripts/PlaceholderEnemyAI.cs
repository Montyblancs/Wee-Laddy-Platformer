using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyController2D))]
[RequireComponent(typeof(CharacterStats))]
public class PlaceholderEnemyAI : MonoBehaviour
{
	//Public vars
	public GameObject targetToChase;
	public float damageOnTouch = 2;
	public float shootDelay = 5;
	public float maxJumpHeight = 4;
	public float minJumpHeight = 1;
	public float timeToJumpApex = .4f;
	public float accelerationTimeAirborne = .2f;
	public float accelerationTimeGrounded = .1f;
	public GameObject projectileType;
	public GameObject enemyNearBulletParentContainer;
	public AudioClip shotSound;

	//Private vars
	Vector2 directionalInput;
	bool readyToFire = false;
	Vector3 velocity;
	float gravity;
	float maxJumpVelocity;
	float minJumpVelocity;
	float velocityXSmoothing;
	private List<string> activeCoroutines = new List<string> { };

	//Components
	Renderer rend;
	EnemyController2D controller;
	CharacterStats stats;
	BoxCollider2D thisCollider;
	BoxCollider2D playerCollider;
	Animator enemyAnimator;
	SpriteRenderer enemySpriteRender;
	AudioSource objectAudio;
	Enemy enemyScript;

	void Start()
	{
		rend = GetComponent<Renderer>();
		controller = GetComponent<EnemyController2D>();
		stats = GetComponent<CharacterStats>();
		thisCollider = GetComponent<BoxCollider2D>();
		playerCollider = targetToChase.GetComponent<BoxCollider2D>();
		enemyAnimator = GetComponent<Animator>();
		enemySpriteRender = GetComponent<SpriteRenderer>();
		objectAudio = GetComponent<AudioSource>();
		enemyScript = GetComponent<Enemy> ();

		gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
		maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
		minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
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
							CharacterStats playerStats;
							if (playerStats = targetToChase.GetComponent<CharacterStats>())
							{
								playerStats.damage(damageOnTouch);
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
							CharacterStats playerStats;
							if (playerStats = targetToChase.GetComponent<CharacterStats>())
							{
								playerStats.damage(damageOnTouch);
							}
						}
					}
				}

				SetDirectionalInput(TowardsPlayer);
				TryToFire ();
			}

			if (!enemyScript.statCanMove) {
				velocity.x = 0;
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

	void CalculateVelocity()
	{
		float targetVelocityX = directionalInput.x * stats.SPD;
		velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);    
		velocity.y += gravity * Time.deltaTime;
	}

	public void SetDirectionalInput(Vector2 input)
	{
		directionalInput = input;
	}

	private IEnumerator FireTimer(float duration)
	{
		if (!activeCoroutines.Contains("FireTimer"))
		{
			activeCoroutines.Add("FireTimer");
			yield return new WaitForSeconds(duration);
			activeCoroutines.Remove("FireTimer");
			readyToFire = true;
		}
	}

	void TryToFire()
	{
		if (!activeCoroutines.Contains("FireTimer") && !readyToFire)
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
}

