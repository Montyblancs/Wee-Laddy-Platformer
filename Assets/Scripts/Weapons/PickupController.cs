using UnityEngine;
using UnityEngine.UI;

public class PickupController : MonoBehaviour {

    public GameObject playerObject;
    public GameObject giveBulletType;
    public short numOfShots;
    //Fire Type Codes:
    //0 - Semi-Auto (Once on mouse button down)
    //1 - Full-Auto (Constant while mouse button down)
    //2 - Shotgun (Once on down, has fire delay)
    public byte fireType;
    public float fireRate;
    // sprite that will show up in the HUD when picked up
    public Sprite iconForHUD;

    Renderer rend;

    BoxCollider2D thisCollider;
    BoxCollider2D playerCollider;

    Player playerScript;

    void Start () {
        rend = GetComponent<Renderer>();
        thisCollider = GetComponent<BoxCollider2D>();
        playerCollider = playerObject.GetComponent<BoxCollider2D>();
        playerScript = playerObject.GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 inView = Camera.main.WorldToViewportPoint(gameObject.transform.position);
        if (inView.x > -0.15f && inView.x < 1.15f && inView.y > -0.15f && inView.y < 1.15f)
        {
            rend.enabled = true;
            if(thisCollider.bounds.Intersects(playerCollider.bounds))
            {
                //Do Pickup
                playerScript.ProjectileChange(giveBulletType, numOfShots, fireType, fireRate, iconForHUD);
                Destroy(gameObject);
            }
        }
        else
        {
            rend.enabled = false;
        }
    }
}
