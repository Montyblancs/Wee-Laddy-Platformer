using UnityEngine;
using System.Collections;

public class TestBossTrigger : MonoBehaviour
{
	public BoxCollider2D PlayerCollider;
	public GameObject CameraLockObject;
	public GameObject BossWallL;
	public GameObject BossWallR;
	public CameraFollow_v2 CameraScript;

	BoxCollider2D ThisCollider;

	//When player hits trigger :
	//Enable L and R walls
	//Lock camera to position of CameraLockObject
	//Start Boss
	//Delete Trigger

	// Use this for initialization
	void Start ()
	{
		ThisCollider = GetComponent<BoxCollider2D>();
	}
	
	// Update is called once per frame
	void Update ()
	{
		Vector3 inView = Camera.main.WorldToViewportPoint(gameObject.transform.position);
		if (inView.x > -0.15f && inView.x < 1.15f && inView.y > -0.15f && inView.y < 1.15f)
		{
			if(ThisCollider.bounds.Intersects(PlayerCollider.bounds))
			{
				//Trigger Hit
				BossWallL.SetActive(true);
				BossWallR.SetActive(true);
				CameraScript.cameraLockPosition = CameraLockObject.transform.position;
				CameraScript.isCameraLocked = true;
				Destroy(gameObject);
			}
		}
	}
}

