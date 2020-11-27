using UnityEngine;
using System.Collections;

public class TestBossTrigger : MonoBehaviour
{
	public BoxCollider2D PlayerCollider;
	public GameObject CameraLockObject;
	public GameObject BossWallL;
	public GameObject BossWallR;
    public GameObject BossWallT;
    public GameObject NearEnemyContainer;
    public GameObject FarEnemyContainer;
	public CameraFollow_v2 CameraScript;
    public Stage1Music MusicScript;
	//public Camera lockAreaCamera;

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
				//lockAreaCamera.enabled = true;
				CameraScript.cameraLockPosition = CameraLockObject.transform.position;
				//Vector3 stageDimensions = lockAreaCamera.ScreenToWorldPoint(CameraLockObject.transform.position);
				//float height = 2f * lockAreaCamera.orthographicSize;
				//float width = height * lockAreaCamera.aspect;
				BossWallL.SetActive(true);
				//BossWallL.transform.position = new Vector3 (stageDimensions.x - 2f, BossWallL.transform.position.y, BossWallL.transform.position.z);
				BossWallR.SetActive(true);
                BossWallT.SetActive(true);
				//BossWallR.transform.position = new Vector3 (stageDimensions.x + width - 2f, BossWallL.transform.position.y, BossWallL.transform.position.z);
				CameraScript.isCameraLocked = true;
                //Index active enemies
                int i = 0;
                GameObject[] nearChildren = new GameObject[NearEnemyContainer.transform.childCount];
                foreach (Transform nearChild in NearEnemyContainer.transform)
                {
                    nearChildren[i] = nearChild.gameObject;
                    i += 1;
                }
                i = 0;
                GameObject[] farChildren = new GameObject[FarEnemyContainer.transform.childCount];
                foreach (Transform farChild in FarEnemyContainer.transform)
                {
                    farChildren[i] = farChild.gameObject;
                    i += 1;
                }

                //Delete all enemies
                foreach (GameObject nearChild in nearChildren)
                {
                    DestroyImmediate(nearChild.gameObject);
                }
                foreach (GameObject farChild in farChildren)
                {
                    DestroyImmediate(farChild.gameObject);
                }

                MusicScript.PlayBossTrack();
				//lockAreaCamera.enabled = false;
				Destroy(gameObject);
			}
		}
	}
}

