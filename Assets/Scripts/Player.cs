using UnityEngine;
using System.Collections;

// TODO: Make the CharacterStats Component Optional?
[RequireComponent(typeof(Controller2D))]
[RequireComponent(typeof(CharacterStats))]
public class Player : MonoBehaviour
{

    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;
    float moveSpeed = 6;

    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = .25f;

    public GameObject farBulletParentContainer;
    public GameObject nearBulletParentContainer;
    public GameObject defaultProjectileType;
    public GameObject projectileType;
    ProjectileController thisProjectileController;
    AudioClip shotSound;
    short currentShotType;
    int specialRoundsLeft;
    float specialFireRate;
    float timeToNextFire;
    public GameObject spawnPoint;

    public Camera playerCam;

    float timeToWallUnstick;

    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    public Vector3 velocity;
    float velocityXSmoothing;

    Controller2D controller;
    CharacterStats stats;

    Vector2 directionalInput;
    AudioSource objectAudio;
    bool wallSliding;
    int wallDirX;

    public Texture2D FlatCursor;
    public Texture2D FarCursor;
    int shotDir; //0 - flat 1 - far

    // variables to determine if what the player is currently able to do
    bool canMove = true;
    bool canFire = true;

    void Start()
    {
        // get essential components
        controller = GetComponent<Controller2D>();
        stats = GetComponent<CharacterStats>();

        // start checking the player stats so we can have the player reflect the condition
        StartCoroutine("MonitorCondition");

        // set the boolean enablers to default state
        this.canMove = true;
        this.canFire = true;

        // calculate limiter variables
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        shotDir = 0;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.SetCursor(FlatCursor, new Vector2(15, 15), CursorMode.Auto);

        objectAudio = GetComponent<AudioSource>();
        thisProjectileController = projectileType.GetComponent<ProjectileController>();
        shotSound = thisProjectileController.shotSound;
        currentShotType = 0;
        timeToNextFire = 0;
        specialRoundsLeft = -1;
        specialFireRate = -1;
    }

    void Update()
    {
        // update movement
        CalculateVelocity();
        HandleWallSliding();

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

        if(timeToNextFire > 0)
            timeToNextFire -= Time.deltaTime;
    }

    public void OnEnable()
    {
        StartCoroutine("MonitorCondition");
    }

    public void OnDisable()
    {
        StopCoroutine("MonitorCondition");
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
        if (!this.canMove) return;
        directionalInput = input;
    }

    //Inputs
    public void OnJumpInputDown()
    {
        // just the return if this function is disabled
        if (!this.canMove) {
            return;
        }
        if (wallSliding)
        {
            if (wallDirX == directionalInput.x)
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            }
            else if (directionalInput.x == 0)
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            }
            else
            {
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
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
        // just the return if this function is disabled
        if (!this.canMove) {
            return;
        }
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

    public void OnMouseButtonDown()
    {
        // just the return if this function is disabled
        if (!this.canFire) {
            return;
        }
        //Semi-Auto fire only, see MouseButtonHold for full auto
        if (currentShotType == 0)
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
            if (shotDir == 1)
            {
                thisProjectile.transform.parent = farBulletParentContainer.transform;
            }
            else
            {
                thisProjectile.transform.parent = nearBulletParentContainer.transform;
            }
            ProjectileController projectileScript = thisProjectile.GetComponent<ProjectileController>();

            //Mouse position is not equal to position in game world, just the position on the screen.
            //Need game world equivilent position for this coord.
            Vector3 shotTarget = Input.mousePosition;
            if (shotDir == 0)
            {
                shotTarget.z = 10;
            }
            else
            {
                shotTarget.z = 21;
            }

            projectileScript.targetCoords = playerCam.ScreenToWorldPoint(shotTarget);

            objectAudio.PlayOneShot(shotSound, 0.3f);

            if (specialRoundsLeft != -1)
                specialRoundsLeft--;

            if (specialRoundsLeft == 0)
                ProjectileChange(defaultProjectileType, -1, 0, 0);
        }

    }

    public void OnMouseButtonHold()
    {
        // just the return if this function is disabled
        if (!this.canFire) {
            return;
        }
        if (currentShotType == 1 && timeToNextFire <= 0)
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
            if (shotDir == 1)
            {
                thisProjectile.transform.parent = farBulletParentContainer.transform;
            }
            else
            {
                thisProjectile.transform.parent = nearBulletParentContainer.transform;
            }
            ProjectileController projectileScript = thisProjectile.GetComponent<ProjectileController>();

            //Mouse position is not equal to position in game world, just the position on the screen.
            //Need game world equivilent position for this coord.
            Vector3 shotTarget = Input.mousePosition;
            if (shotDir == 0)
            {
                shotTarget.z = 10;
            }
            else
            {
                shotTarget.z = 21;
            }

            projectileScript.targetCoords = playerCam.ScreenToWorldPoint(shotTarget);

            objectAudio.PlayOneShot(shotSound, 0.3f);

            timeToNextFire = specialFireRate;
            if (specialRoundsLeft != -1)
                specialRoundsLeft--;

            if (specialRoundsLeft == 0)
                ProjectileChange(defaultProjectileType, -1, 0, 0);
        }
    }

    public void OnPlaneChange()
    {
        if (shotDir == 0)
        {
            //Switch to far
            Cursor.SetCursor(FarCursor, new Vector2(FarCursor.width / 2, FarCursor.height / 2), CursorMode.Auto);
            shotDir = 1;
        }
        else
        {
            Cursor.SetCursor(FlatCursor, new Vector2(FlatCursor.width / 2, FlatCursor.height / 2), CursorMode.Auto);
            shotDir = 0;
        }
    }

    //Weapons
    public void ProjectileChange(GameObject bulletType, int numShots, short fireType, float fireRate)
    {
        projectileType = bulletType;
        specialRoundsLeft = numShots;
        currentShotType = fireType;
        specialFireRate = fireRate;
        thisProjectileController = projectileType.GetComponent<ProjectileController>();
        shotSound = thisProjectileController.shotSound;
    }

    //Movement
    void HandleWallSliding()
    {
        wallDirX = (controller.collisions.left) ? -1 : 1;
        wallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            if (timeToWallUnstick > 0)
            {
                velocityXSmoothing = 0;
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
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
        velocity.y += gravity * Time.deltaTime;
    }

    // monitor life and death based on the CharacterStats
    IEnumerator MonitorCondition()
    {
        // don't bother monitoring if the CharacterStats component is not set
        if (!stats) {
            Debug.Log("The CharacterStats component for this player has not been set yet.");
            yield break;
        }
        // we will assume any conditions have yet been applied when this coroutine started.
        ConditionType appliedCondition = stats.Condition;
        // Start looping until the coroutine is manually stopped.
        while (true) 
        {
            // check if they were not yet dead, but they need to be.
            if (stats.isDead() && appliedCondition != ConditionType.DEAD) {
                Debug.Log("dieing");
                // start by making sure the character is dead according to the CharacterStats
                if (stats && !stats.isDead()) {
                    stats.kill();
                }
                // disable all functionality
                this.canMove = false;
                this.canFire = false;
                // set the new applied condition
                appliedCondition = stats.Condition;
                // wait a few seconds before being able to live or die again
                yield return new WaitForSeconds(2f);
            // if they aren't dead and they previously were, well... bring um back.
            } else if (stats.isAlive() && appliedCondition == ConditionType.DEAD) {
                Debug.Log("reviving");
                // start by making sure the character is alive according to the CharacterStats
                if (stats && !stats.isAlive()) {
                    stats.revive();
                }
                // enable all functionality
                this.canMove = true;
                this.canFire = true;
                // set the new applied condition
                appliedCondition = stats.Condition;
                // put the player back were they belong
                this.Respawn();
                // wait a few seconds before being able to revive again
                yield return new WaitForSeconds(2f);
            }
            // only poll on a set interval, for now every tenth of a second
            yield return new WaitForSeconds(0.1f);
        }
    }

    // passes kill to the CharacterStats object
    public void kill() {
        if (this.stats) {
            this.stats.kill();
        }
    }

    // passes kill to the CharacterStats object
    public void revive() {
        if (this.stats) {
            this.stats.revive();
        }
    }
}
