using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Player))]
public class PlayerInput : MonoBehaviour
{
    Player player;

    void Start()
    {
        player = GetComponent<Player>();
    }

    void Update()
    {
        Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        player.SetDirectionalInput(directionalInput);

        //Jumping
        if (Input.GetKeyDown(KeyCode.W))
        {
            player.OnJumpInputDown();
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            player.OnJumpInputUp();
        }

        //Shooting
        if (Input.GetMouseButtonDown(0))
        {
            player.OnMouseButtonDown();
        }

        if (Input.GetMouseButton(0))
        {
            player.OnMouseButtonHold();
        }
        
        //Changing shooting plane
        if (Input.GetKeyDown(KeyCode.Q))
        {
            player.OnPlaneChange();
        }

        //Dodge Roll
        if (Input.GetKeyDown(KeyCode.E))
        {
            player.OnDodgeRoll();
        }

        if (Application.isEditor && Input.GetKeyDown(KeyCode.Backspace))
        {
            //Reload scene for debug purposes
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Application.isEditor && Input.GetKeyDown(KeyCode.BackQuote))
        {
            //Toggle debug panel
            CanvasGroup cGroup = GameObject.Find("DebugUI").GetComponent<CanvasGroup>();
            cGroup.alpha = (cGroup.alpha >= 1f) ? 0f : 1f;
        }
    }
}
