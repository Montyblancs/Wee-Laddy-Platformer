using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


// static class for extending enumeration methods.
public static class StatExtensions
{
	// bitwise stat variables for defining centralized stat types and conditions
    public static StatType RegenStats = StatType.HP | StatType.MP;
    
    // this may not be the best method or used in the future, but i wanted to keep it here for future referance.
    // This would allow one to do something like stat.canAutoRegen
    public static bool canAutoRegen(StatType stat)
    {
    	// bitwise AND operation to determine if the flag is in the defined stat set.
    	return (RegenStats & stat) == stat ? true : false;
    }
}

// NOTE: referance here: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/enumeration-types
// define the stats as flags to they can easily be for centralized condition/requirements checking.
[Flags]
public enum StatType {
	NONE = 0x0,
	HP = 0x1,
	MP = 0x2,
	SPD = 0x4,
	STR = 0x8,
	DEX = 0x10,
	INT = 0x20
};

// define the different conditions this character can be in
public enum ConditionType {HEALTHY, KNOCKOUT, DEAD};

// TODO: figure out the proper components needed
public class CharacterStats : MonoBehaviour {
	public string name;
	public int level;
	public int xp;
	public ConditionType condition;
	// Store all stats using a Dictonary for more clear code.
	private Dictionary<StatType, float> baseStats = new Dictionary<StatType, float>();
	// make getters and setters for the main stat types for easier controlled access.
	public float MaxHitPoints
	{
		get { return this.getBaseStatOrZero(StatType.HP); }
		set {
			this.setOrAddBaseStat(StatType.HP, value);
		}
	}
	public float MaxMagicPoints
	{
		get { return this.getBaseStatOrZero(StatType.MP); }
		set {
			this.setOrAddBaseStat(StatType.MP, value);
		}
	}
	public float Speed
	{
		get { return this.getBaseStatOrZero(StatType.SPD); }
		set {
			this.setOrAddBaseStat(StatType.SPD, value);
		}
	}
	public float Strength
	{
		get { return this.getBaseStatOrZero(StatType.STR); }
		set {
			this.setOrAddBaseStat(StatType.STR, value);
		}
	}
	public float Dexterity
	{
		get { return this.getBaseStatOrZero(StatType.DEX); }
		set {
			this.setOrAddBaseStat(StatType.DEX, value);
		}
	}
	public float Intelligence
	{
		get { return this.getBaseStatOrZero(StatType.INT); }
		set {
			this.setOrAddBaseStat(StatType.INT, value);
		}
	}

	// All essential intialization after the component is instantiated and before the component gets enabled.
	void Awake () {
		// initialize all stats in the baseStats dictionary to a 4d6 dice roll, dropping the lowest
		this.randomizeBaseStats();
	}

	// Initialization after the component first gets enabled. This happens on the very next Update() call.
	void Start () {
		// if there is no name, give it a name manually for now.
		if (this.name == "") {
			this.name = "FartFace";
		}
		// For now, show thats in the debug log.
		this.debugLogBaseStats();
		// TODO: display stats on screen somehow.
	}

	// return the stat if it exists, otherwise return 0
	public float getBaseStatOrZero(StatType type)
	{
		float thisStat;
		if (this.baseStats.TryGetValue(type, out thisStat)) {
			return thisStat;
		}
		return 0f;
	}

	// set a base stat to a value ONLY if they have this stat. otherwise don't do anything.
	public void setBaseStatIfExists(StatType type, float value)
	{
		if (this.baseStats.ContainsKey(type)) {
			this.baseStats[type] = value;
		}
	}

	// set a base stat to a value OR add it if it doesn't exist
	public void setOrAddBaseStat(StatType type, float value)
	{
		if (this.baseStats.ContainsKey(type)) {
			this.baseStats[type] = value;
		} else {
			this.baseStats.Add(type, value);
		}
	}

	public static int rollDiceDropLowest(int diceAmount, int diceSides, int dropAmount)
	{
		List<int> diceResults = new List<int>();
		int total = 0;
		int count = 0;
		string output = "Dice Results: ";
		for (int i = 0; i < diceAmount; i++) {
			diceResults.Add(UnityEngine.Random.Range(1, diceSides+1));
		}
		diceResults.Sort((a, b) => -1* a.CompareTo(b)); // descending sort
		// add up everything except the ones we will drop
		foreach (int value in diceResults) {
			if (count < diceAmount-dropAmount) {
				total += value;
			}
			output += value.ToString()+" ";
			count++;
		}
		output += "with a toal result of "+total.ToString();
		Debug.Log(output);
		return total;
	}

	// randomize all stats. For now we can do a 4d6, drop the lowest for each stat.
	public void randomizeBaseStats()
	{
		// loop through all types of stats and set a value in the dictionary for each
		foreach(StatType stat in Enum.GetValues(typeof(StatType))) {
			if (stat != StatType.NONE) {
				// check if the stat exists in the baseStat dictionary first. If not, add it.
				this.setOrAddBaseStat(stat, (float)rollDiceDropLowest(4, 6, 1));
			}
		}
	}

	// set all base stats to zero
	public void zeroBaseStats()
	{
		// loop through all types of stats and set a value in the dictionary for each
		foreach(StatType stat in Enum.GetValues(typeof(StatType))) {
			if (stat != StatType.NONE) {
				// check if the stat exists in the baseStat dictionary first. If not, add it.
				this.setOrAddBaseStat(stat, 0f);
			}
		}
	}

	// write a line in the debug window showing each stat for this character
	public void debugLogBaseStats()
	{
		Debug.Log("Base Stats for Character with the name \""+this.name+"\" will be displayed next.");
		foreach (KeyValuePair<StatType, float> stat in this.baseStats) {
			Debug.Log(""+stat.Key.ToString()+": "+stat.Value.ToString());
		}
	}
	
	// TODO: create a function to get the effective stat, after all effects from outside sources
	
	// TODO: function to apply a temporary stat boost or debuff with different such as time or turns or ticks, whatever.
	// NOTE: consider how this will work with HP and MP. To keep things generic im going to consider it like a debuff on health.

	// TODO: function to check for effect of a stat lowering or raising. For example if health goes to zero, the player is dead or knocked out.
	
	// TODO: function to check if the player is dead?

	// TODO: function to rise this objects statistics to the next "level"
}