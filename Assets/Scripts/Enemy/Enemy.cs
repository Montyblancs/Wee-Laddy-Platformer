using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterStats))]
public class Enemy : MonoBehaviour
{
	public float deadBodyLastTime;
	bool DeadBodyTimerStarted;

    public AudioClip[] deathSounds;

    Renderer rend;

	[HideInInspector]
	public Animator enemyAnimator;

	CharacterStats stats;
	[HideInInspector]
	public bool statCanMove = true;
	[HideInInspector]
	public bool statCanFire = true;

	// holds a list of all active coroutines started by this object
	private List<string> activeCoroutines = new List<string> { };

    void Start()
    {
        rend = GetComponent<Renderer>();

		DeadBodyTimerStarted = false;

		enemyAnimator = GetComponent<Animator>();

		stats = GetComponent<CharacterStats>();

		// constantly check stats
		StartCoroutine("MonitorCondition");
    }

    void Update()
    {

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

	private IEnumerator DeadBodyTimer(float duration)
	{
		if (!DeadBodyTimerStarted)
		{
			DeadBodyTimerStarted = true;
			yield return new WaitForSeconds(duration);
			Destroy (gameObject);
		}
	}
}
