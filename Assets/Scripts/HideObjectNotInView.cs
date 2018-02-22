using UnityEngine;

public class HideObjectNotInView : MonoBehaviour {

    Renderer rend;

    // Use this for initialization
    void Start () {
        rend = GetComponent<Renderer>();
    }
	
	// Update is called once per frame
	void Update () {
        Vector3 inView = Camera.main.WorldToViewportPoint(gameObject.transform.position);
        if (inView.x > -0.55f && inView.x < 1.55f && inView.y > -0.55f && inView.y < 1.55f)
        {
            rend.enabled = true;
        }
        else
        {
            rend.enabled = false;
        }
    }
}
