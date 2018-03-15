using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// TODO: Make the CharacterStats Component Optional?
[RequireComponent(typeof(Controller2D))]
[RequireComponent(typeof(CharacterStats))]
public class Player : MonoBehaviour
{

    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    private float accelerationTimeAirborne = .2f;
    private float accelerationTimeGrounded = .1f;
    // move speed is dependant on the SPD stat of the character stats script, so the value set here is more of a default.
    private float moveSpeed = 6;
    // for convenience sake, use the property below to change character speed, or you can modify stats.SPD
    public float MoveSpeed
    {
        get { return (stats ? stats.SPD : moveSpeed); }
        set
        {
            // if characterstats is fetched, set the stat.
            if (stats)
            {
                stats.SPD = value;
            }
            else
            {
                this.moveSpeed = value;
            }
        }
    }

    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = .25f;

    public sbyte dodgeLayer;
    public sbyte playerLayer;
    public float dodgeCooldown = 3f;
    public float dodgeDuration = 0.2f;

    public GameObject farBulletParentContainer;
    public GameObject nearBulletParentContainer;
    public GameObject casingContainer;
    public GameObject defaultProjectileType;
    public GameObject projectileType;
    ProjectileController thisProjectileController;
    AudioClip shotSound;
    byte currentShotType;
    static byte SHOT_NORMAL = 0;
    static byte SHOT_CHAINGUN = 1;
    static byte SHOT_SHOTGUN = 2;
    short specialRoundsLeft;
    float specialFireRate;
    private bool canFire;
    private bool canDodge;
    public GameObject spawnPoint;

    public Camera playerCam;

    float timeToWallUnstick;

    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    public Vector3 velocity;
    private float accelerationX = 0f;

    Controller2D controller;
    CharacterStats stats;
    Renderer render;

    Vector2 directionalInput;
    AudioSource objectAudio;
    bool wallSliding;
    short wallDirX;

    public Texture2D FlatCursor;
    public Texture2D FarCursor;
    byte shotDir;
    static byte DIR_NEAR = 0;
    static byte DIR_FAR = 1;

    // variables to determine what the player is currently able to do
    [HideInInspector]
    public bool statCanMove = true;
    [HideInInspector]
    public bool statCanFire = true;

    // holds a list of all active coroutines started by this object
    private List<string> activeCoroutines = new List<string> { };

    // -- variables for UI HUD elements --
    // an image in the UI to reflect show what weapon the player has
    [SerializeField]
    public Image weaponImage;
    // a sprite for when the player has no weapon pickup active, fetch on awake from weaponImage
    private Sprite defaultWeaponImage;
    // an image in the UI to reflect show what weapon the player has
    [SerializeField]
    public Text ammoDisplay;

    [HideInInspector]
    public Animator playerAnimator;
    [HideInInspector]
    public SpriteRenderer playerSpriteRender;

    void Start()
    {
        // get essential components
        controller = GetComponent<Controller2D>();
        stats = GetComponent<CharacterStats>();
        render = GetComponent<Renderer>();
        playerAnimator = GetComponent<Animator>();
        playerSpriteRender = GetComponent<SpriteRenderer>();

        // see if we can fetch the default weapon sprite from the ui image
        if (weaponImage)
        {
            defaultWeaponImage = weaponImage.sprite;
        }

        // set the boolean enablers to default state
        statCanMove = true;
        statCanFire = true;

        // start checking the player stats so we can have the player reflect the condition
        StartCoroutine("MonitorCondition");

        // calculate limiter variables
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        shotDir = DIR_NEAR;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.SetCursor(FlatCursor, new Vector2(15, 15), CursorMode.Auto);

        objectAudio = GetComponent<AudioSource>();
        thisProjectileController = projectileType.GetComponent<ProjectileController>();
        shotSound = thisProjectileController.shotSound;
        currentShotType = SHOT_NORMAL;
        canFire = true;
        canDodge = true;
        specialRoundsLeft = -1;
        specialFireRate = -1;
    }

    void Update()
    {
        // update movement
        CalculateVelocity();
        HandleWallSliding();

        //Animator
        SetAnimatorParameters();

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
    }

    private void SetAnimatorParameters()
    {
        playerAnimator.SetFloat("x_velocity", Mathf.Abs(velocity.x));
        playerAnimator.SetFloat("y_velocity", velocity.y);
        playerAnimator.SetBool("on_ground", controller.collisions.below);
        playerSpriteRender.flipX = (controller.collisions.faceDir != 1) ? true : false;
    }

    //Coroutine Timers
    private IEnumerator FireTimer(float duration)
    {
        if (canFire && duration > 0)
        {
            canFire = !canFire;
            yield return new WaitForSeconds(duration);
            canFire = !canFire;
        }
    }

    private IEnumerator DodgeTimer(float duration)
    {
        //Dodge movement is handled in Controller2D
        if (!controller.isDodging && duration > 0)
        {
            controller.isDodging = true;
            gameObject.layer = dodgeLayer;
			playerAnimator.SetTrigger("start_slide");
            yield return new WaitForSeconds(duration);
            controller.isDodging = false;
            gameObject.layer = playerLayer;
			playerAnimator.SetTrigger("end_slide");
        }
    }

    private IEnumerator DodgeCooldownTimer(float duration)
    {
        if (canDodge && duration > 0)
        {
            canDodge = !canDodge;
            yield return new WaitForSeconds(duration);
            canDodge = !canDodge;
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

    public void Respawn()
    {
        // remove any current movement variables
        velocity.y = 0;
        // put the player back at the spawn point
        transform.position = spawnPoint.transform.position;
    }

    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = (!statCanMove) ? Vector2.zero : input;
    }

    //Inputs
    public void OnJumpInputDown()
    {
        // just the return if this function is disabled
        if (!statCanMove) return;
        // if this player is wallsliding, do a few special things.
        if (wallSliding)
        {
            if (wallDirX == directionalInput.x)
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
                playerAnimator.SetTrigger("start_jump");
            }
            else if (directionalInput.x == 0)
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
                playerAnimator.SetTrigger("start_jump");
            }
            else
            {
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
                playerAnimator.SetTrigger("start_jump");
            }
        }
        if (controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope)
            {
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                { // not jumping against max slope
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                    playerAnimator.SetTrigger("start_jump");
                }
            }
            else
            {
                velocity.y = maxJumpVelocity;
                playerAnimator.SetTrigger("start_jump");
            }
        }
    }

    public void OnJumpInputUp()
    {
        // just the return if this movement functionality is disabled
        if (!statCanMove) return;
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

    public void OnMouseButtonDown()
    {
        // just the return if this function is disabled
        if (!statCanFire) return;
        //Semi-Auto fire only, see MouseButtonHold for full auto
        if ((currentShotType == SHOT_NORMAL || currentShotType == SHOT_SHOTGUN) && canFire)
        {
            var fireDirection = controller.collisions.faceDir;
            //Determine if player is wall sliding, don't allow fire inside of wall
            if (wallSliding && fireDirection == wallDirX)
            {
                fireDirection = fireDirection * -1;
            }

            if (currentShotType == SHOT_NORMAL)
            {
                //Create a projectile that travels towards the current position of the mouse cursor.
                //gameObject refers to the parent object of this script
                GameObject thisProjectile = Instantiate(projectileType, new Vector3(gameObject.transform.position.x + fireDirection - (0.5f * fireDirection), gameObject.transform.position.y, gameObject.transform.position.z), Quaternion.identity);
                thisProjectile.transform.parent = (shotDir == DIR_FAR) ? farBulletParentContainer.transform : nearBulletParentContainer.transform;
                ProjectileController projectileScript = thisProjectile.GetComponent<ProjectileController>();
                projectileScript.casingContainer = casingContainer;
                projectileScript.CreateCasing(new Vector3(gameObject.transform.position.x + fireDirection - (0.5f * fireDirection), gameObject.transform.position.y, 5));

                //Mouse position is not equal to position in game world, just the position on the screen.
                //Need game world equivilent position for this coord.
                Vector3 shotTarget = Input.mousePosition;

                //Should we get the z index of an object under the cursor at the time of click for an accurate target?
                shotTarget.z = (shotDir == DIR_NEAR) ? 5 : 16;

                projectileScript.targetCoords = playerCam.ScreenToWorldPoint(shotTarget);
            }
            else if (currentShotType == SHOT_SHOTGUN)
            {
                //A shotgun spread blast
                //Note : flat plane spread is dependent on cursor distance from player. Make spread static in the future
                List<GameObject> theseBullets = new List<GameObject>();
                for (byte i = 1; i <= 5; i++)
                {
                    GameObject thisBullet = Instantiate(projectileType, new Vector3(gameObject.transform.position.x + fireDirection - (0.5f * fireDirection), gameObject.transform.position.y, 5), Quaternion.identity);
                    theseBullets.Add(thisBullet);
                }

                if (shotDir == DIR_FAR)
                {
                    foreach (GameObject TB in theseBullets)
                    {
                        TB.transform.parent = farBulletParentContainer.transform;
                    }
                }
                else
                {
                    foreach (GameObject TB in theseBullets)
                    {
                        TB.transform.parent = nearBulletParentContainer.transform;
                    }
                }
                List<ProjectileController> theseScripts = new List<ProjectileController>();
                foreach (GameObject TB in theseBullets)
                {
                    ProjectileController temp = TB.GetComponent<ProjectileController>();
                    theseScripts.Add(temp);
                }
                theseScripts[0].casingContainer = casingContainer;
                theseScripts[0].CreateCasing(new Vector3(gameObject.transform.position.x + fireDirection - (0.5f * fireDirection), gameObject.transform.position.y, 5));

                //Mouse position is not equal to position in game world, just the position on the screen.
                //Need game world equivilent position for this coord.
                Vector3 shotTarget = Input.mousePosition;
                if (shotDir == DIR_NEAR)
                {
                    shotTarget.z = 5;

                    //Spread pattern
                    List<Vector3> alteredTargets = new List<Vector3>
                    {
                    new Vector3(shotTarget.x, shotTarget.y, shotTarget.z),
                    new Vector3(shotTarget.x - 21f, shotTarget.y - 21f, shotTarget.z),
                    new Vector3(shotTarget.x - 30f, shotTarget.y - 30f, shotTarget.z),
                    new Vector3(shotTarget.x + 21f, shotTarget.y + 21f, shotTarget.z),
                    new Vector3(shotTarget.x + 30f, shotTarget.y + 30f, shotTarget.z)
                    };

                    var shotIndex = 0;
                    foreach (ProjectileController TS in theseScripts)
                    {
                        TS.targetCoords = playerCam.ScreenToWorldPoint(alteredTargets[shotIndex]);
                        shotIndex++;
                    }
                }
                else
                {
                    shotTarget.z = 16;

                    //+ Pattern
                    List<Vector3> alteredTargets = new List<Vector3>
                    {
                    new Vector3(shotTarget.x, shotTarget.y, shotTarget.z),
                    new Vector3(shotTarget.x + 10f, shotTarget.y, shotTarget.z),
                    new Vector3(shotTarget.x - 10f, shotTarget.y, shotTarget.z),
                    new Vector3(shotTarget.x, shotTarget.y + 10f, shotTarget.z),
                    new Vector3(shotTarget.x, shotTarget.y - 10f, shotTarget.z)
                    };

                    var shotIndex = 0;
                    foreach (ProjectileController TS in theseScripts)
                    {
                        TS.targetCoords = playerCam.ScreenToWorldPoint(alteredTargets[shotIndex]);
                        shotIndex++;
                    }
                }
            }

            objectAudio.PlayOneShot(shotSound, 0.3f);

            //Start fire timer coroutine
            StartCoroutine(FireTimer(specialFireRate));
            if (specialRoundsLeft != -1)
                specialRoundsLeft--;

            if (specialRoundsLeft == 0 && this.projectileType != this.defaultProjectileType)
            {
                ProjectileChange(defaultProjectileType, -1, 0, 0);
            }
            else
            {
                // update the ui after each shot
                this.updateWeaponUI();
            }
        }

    }

    public void OnMouseButtonHold()
    {
        // just the return if this function is disabled
        if (!statCanFire) return;
        if (currentShotType == SHOT_CHAINGUN && canFire)
        {
            var fireDirection = controller.collisions.faceDir;
            //Determine if player is wall sliding, don't allow fire inside of wall
            if (wallSliding && fireDirection == wallDirX)
            {
                fireDirection = fireDirection * -1;
            }

            //Create a projectile that travels towards the current position of the mouse cursor.
            //gameObject refers to the parent object of this script
            GameObject thisProjectile = Instantiate(projectileType, new Vector3(gameObject.transform.position.x + fireDirection - (0.5f * fireDirection), gameObject.transform.position.y, 5), Quaternion.identity);
            thisProjectile.transform.parent = (shotDir == DIR_FAR) ? farBulletParentContainer.transform : nearBulletParentContainer.transform;
            ProjectileController projectileScript = thisProjectile.GetComponent<ProjectileController>();
            projectileScript.casingContainer = casingContainer;
            projectileScript.CreateCasing(new Vector3(gameObject.transform.position.x + fireDirection - (0.5f * fireDirection), gameObject.transform.position.y, 5));

            //Mouse position is not equal to position in game world, just the position on the screen.
            //Need game world equivilent position for this coord.
            Vector3 shotTarget = Input.mousePosition;
            shotTarget.z = (shotDir == DIR_NEAR) ? 5 : 16;

            projectileScript.targetCoords = playerCam.ScreenToWorldPoint(shotTarget);

            objectAudio.PlayOneShot(shotSound, 0.3f);

            //start FireTimer CoRoutine
            StartCoroutine(FireTimer(specialFireRate));
            if (specialRoundsLeft != -1)
                specialRoundsLeft--;

            if (specialRoundsLeft == 0 && this.projectileType != this.defaultProjectileType)
            {
                ProjectileChange(defaultProjectileType, -1, 0, 0);
            }
            else
            {
                // update the ui after each shot
                this.updateWeaponUI();
            }
        }
    }

    public void OnPlaneChange()
    {
        if (shotDir == DIR_NEAR)
        {
            //Switch to far
            Cursor.SetCursor(FarCursor, new Vector2(FarCursor.width / 2, FarCursor.height / 2), CursorMode.Auto);
            shotDir = DIR_FAR;
        }
        else
        {
            Cursor.SetCursor(FlatCursor, new Vector2(FlatCursor.width / 2, FlatCursor.height / 2), CursorMode.Auto);
            shotDir = DIR_NEAR;
        }
    }

    public void OnDodgeRoll()
    {
        ////Dodge movement is handled in Controller2D
        if (canDodge && !controller.isDodging)
        {
            StartCoroutine(DodgeTimer(dodgeDuration));
            StartCoroutine(DodgeCooldownTimer(dodgeCooldown));
        }
    }

    //Weapons
    public void ProjectileChange(GameObject bulletType, short numShots, byte fireType, float fireRate)
    {
        projectileType = bulletType;
        specialRoundsLeft = numShots;
        currentShotType = fireType;
        specialFireRate = fireRate;
        thisProjectileController = projectileType.GetComponent<ProjectileController>();
        shotSound = thisProjectileController.shotSound;
        // swap the sprite to the default weapon image if needed
        if (this.weaponImage && this.projectileType == this.defaultProjectileType && this.weaponImage.sprite != this.defaultWeaponImage)
        {
            // switch ui image to default weapon
            this.weaponImage.sprite = this.defaultWeaponImage;
            this.weaponImage.rectTransform.sizeDelta = new Vector2(defaultWeaponImage.rect.width, defaultWeaponImage.rect.height);
            // remove the ammo display
            ammoDisplay.text = null;
        }
        this.updateWeaponUI();
    }

    public void ProjectileChange(GameObject bulletType, short numShots, byte fireType, float fireRate, Sprite iconForHUD)
    {
        // set the icon in the UI first
        this.weaponImage.sprite = iconForHUD;
        this.weaponImage.rectTransform.sizeDelta = new Vector2(iconForHUD.rect.width, iconForHUD.rect.height);
        // then pass to the main function for changing weapons
        this.ProjectileChange(bulletType, numShots, fireType, fireRate);
    }

    //Movement
    void HandleWallSliding()
    {
        // get the direction the wall is in
        wallDirX = (controller.collisions.left) ? (short)-1 : (short)1;
        // check if directional input is "away" from the wall
        bool inputAway = (directionalInput.x != wallDirX && directionalInput.x != 0) ? true : false;
        // check if we are wall sliding
        wallSliding = false;
        if (!inputAway && (controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            // limit slide speed
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            // ... wtf is this timeToWallUnstick supposed to to? is this incomplete code? This logic will mean its just always ticking down to 0, then reset?
            if (timeToWallUnstick > 0)
            {
                accelerationX = 0;
                velocity.x = 0;

                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                {
                    timeToWallUnstick -= Time.deltaTime;
                }
                else
                {
                    timeToWallUnstick = wallStickTime;
                }
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }

        }

    }

    void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        float transitionTime = (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref accelerationX, transitionTime);
        // if its close enough, make it target value, cause for some reason transitionTime doesn't do that.
        if (Mathf.Abs(accelerationX) < 0.001F)
        {
            accelerationX = 0F;
            velocity.x = targetVelocityX;
        }
        velocity.y += gravity * Time.deltaTime;
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
                // set the new applied condition
                appliedCondition = stats.Condition;
				//Throw Animator trigger
				playerAnimator.SetTrigger("has_died");
                // wait a few seconds before being able to live or die again
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
                // put the player back were they belong
                this.Respawn();
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

    // make sure the ui reflects the current weapon ammo.
    public void updateWeaponUI()
    {
        // check if we need to update the ammo indicator.
        if (this.specialRoundsLeft > 0 && this.ammoDisplay)
        {
            this.ammoDisplay.text = this.specialRoundsLeft.ToString();
        }
    }
}
