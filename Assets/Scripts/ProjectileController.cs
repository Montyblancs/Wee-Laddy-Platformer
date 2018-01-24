using UnityEngine;

public class ProjectileController : MonoBehaviour
{

    public Vector3 targetCoords;
    public float bulletSpeed;
    Vector3 spawnCoords;
    Vector3 travelDirection;
    public AudioClip[] ricSounds;

    public LayerMask collisionMask;
    public LayerMask farCollisionMask;
    public LayerMask enemyMask;
    public bool isEnemyBullet;

    AudioSource objectAudio;
    Renderer rend;

    // Use this for initialization
    void Start()
    {
        spawnCoords = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, targetCoords.z);
        objectAudio = GetComponent<AudioSource>();
        rend = GetComponent<Renderer>();

        travelDirection.x = targetCoords.x - spawnCoords.x;
        travelDirection.y = targetCoords.y - spawnCoords.y;
        //z direction needs to be altered to hit background targets, 0 for foreground targets
        travelDirection.z = targetCoords.z;
        travelDirection.Normalize();
    }

    // Update is called once per frame
    void Update()
    {
        if (rend.enabled)
        {
            transform.Translate(travelDirection * Time.deltaTime * bulletSpeed);

            //If bullet isn't visible, destroy it.
            //http://answers.unity3d.com/questions/8003/how-can-i-know-if-a-gameobject-is-seen-by-a-partic.html
            Vector3 inView = Camera.main.WorldToViewportPoint(gameObject.transform.position);
            if (inView.x < -0.1 || inView.x > 1.1 || inView.y < -0.1 || inView.y > 1.1) { Destroy(gameObject); }

            if (gameObject.transform.position.z == 5)
            {
                //Check for enemy hit before ground hit?
                //to get object that was hit:
                //hit.transform.gameObject

                RaycastHit2D eHit = Physics2D.Raycast(gameObject.transform.position, travelDirection, Time.deltaTime * bulletSpeed, enemyMask);
                Debug.DrawRay(gameObject.transform.position, travelDirection, Color.red);

                if (eHit)
                {
                    if (isEnemyBullet)
                    {

                    }
                    else
                    {
                        AudioSource enemyAudio = eHit.transform.GetComponent<AudioSource>();
                        if (enemyAudio)
                        {
                            Enemy eScript = eHit.transform.GetComponent<Enemy>();
                            AudioClip pickedSound = eScript.deathSounds[Random.Range(0, eScript.deathSounds.Length)];
                            enemyAudio.panStereo = inView.x;
                            enemyAudio.PlayOneShot(pickedSound, 0.4f);
                            rend.enabled = false;
                            eScript.isDying = true;
                            Destroy(eHit.transform.gameObject, pickedSound.length);
                        }
                        else
                        {
                            Destroy(eHit.transform.gameObject);
                        }
                        Destroy(gameObject);
                    }
                }

                RaycastHit2D wHit = Physics2D.Raycast(gameObject.transform.position, travelDirection, Time.deltaTime * bulletSpeed, collisionMask);

                if (wHit)
                {
                    AudioClip pickedSound = ricSounds[Random.Range(0, ricSounds.Length)];
                    objectAudio.panStereo = inView.x;
                    objectAudio.PlayOneShot(pickedSound, 0.2f);
                    rend.enabled = false;
                    Destroy(gameObject, pickedSound.length);
                }

            }
            else
            {
                //Check for distant collisions, shrink bullet size
                //RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, travelDirection, Time.deltaTime * bulletSpeed, farCollisionMask);
                RaycastHit wHit2;
                RaycastHit eHit2;

                Debug.DrawRay(gameObject.transform.position, travelDirection, Color.red);

                if (Physics.Raycast(gameObject.transform.position, travelDirection, out eHit2, Time.deltaTime * bulletSpeed, enemyMask, QueryTriggerInteraction.Ignore))
                {
                    //AudioClip pickedSound = ricSounds[Random.Range(0, ricSounds.Length)];
                    //objectAudio.panStereo = inView.x;
                    //objectAudio.PlayOneShot(pickedSound, 0.1f);
                    //rend.enabled = false;
                    Destroy(gameObject);
                    Destroy(eHit2.transform.gameObject);
                }

                if (Physics.Raycast(gameObject.transform.position, travelDirection, out wHit2, Time.deltaTime * bulletSpeed, farCollisionMask, QueryTriggerInteraction.Ignore))
                {
                    AudioClip pickedSound = ricSounds[Random.Range(0, ricSounds.Length)];
                    objectAudio.panStereo = inView.x;
                    objectAudio.PlayOneShot(pickedSound, 0.1f);
                    rend.enabled = false;
                    Destroy(gameObject, pickedSound.length);
                }

                //initial - 0.25 0.25 1
                //Redo scaling so the bullet reaches min as it arrives to the other plane
                float newScale = 0.25f;
                if (gameObject.transform.position.z != 5f)
                {
                    newScale = (0.25f / (gameObject.transform.position.z - 5f)) * 3f;
                    if (newScale < 0.03f)
                    {
                        Destroy(gameObject);
                    }
                    if (newScale > 0.25f)
                    {
                        newScale = 0.25f;
                    }
                }
                gameObject.transform.localScale = new Vector3(newScale, newScale, 1);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.layer == 8)
        {
            Debug.Log("hit");
        }
    }

    //Debug
    void OnDrawGizmos()
    {
        //Gizmos.color = new Color(0, 1, 0, .5f);
        //Gizmos.DrawSphere(targetCoords, 3f);
    }
}
