using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// static class for extending enumeration methods.
public static class StatExtensions
{
	// bitwise stat variables for defining centralized stat types and conditions
    public static StatType RegenStats = StatType.HP | StatType.MP;
    // default percentage of HP that will be given when a character is revived.
    public static float defaultRevivePercent = 0.1f;
    
    // this may not be the best method or used in the future, but i wanted to keep it here for future referance.
    // This would allow one to do something like stat.canAutoRegen
    public static bool canAutoRegen(this StatType stat)
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
public enum ConditionType {NONE, HEALTHY, KNOCKOUT, LIMBO, DEAD};

public class CharacterStats : MonoBehaviour {
	public string characterName;
	public int level;
	public int xp;
	// the current condition of this player, health-wise
	private ConditionType condition;
	// this will store any manually set condition for use in the updateCondition() function
	private ConditionType pendingCondition;
	// stores all the character's more permanant base statistics
	private Dictionary<StatType, float> baseStats = new Dictionary<StatType, float>();
	// stores all the character's current modifiers to stats
	private Dictionary<StatType, float> modStats = new Dictionary<StatType, float>();
	// stores the highest amount a stat can go, relative to its base stat
	private Dictionary<StatType, float> maxAboveBase = new Dictionary<StatType, float>();
	// stores the lowest amount a stat can go, relative to 0
	private Dictionary<StatType, float> minBelowBase = new Dictionary<StatType, float>();
	// bitwise flags to define the stat types that the condition is dependant upon, so we know when to update it.
	private StatType conditionStats = StatType.HP;
	// flags to define what stats cannot be modified. Initially this is will be no stat types.
	private StatType immutableStats = StatType.NONE;
	// getters and setters for each effective stat
	// these will get changed when we don't want to make less permanant stat mods.
	public float HP
	{
		get { return this.getStat(StatType.HP); }
		set {
			this.setStat(StatType.HP, value);
		}
	}
	public float MP
	{
		get { return this.getStat(StatType.MP); }
		set {
			this.setStat(StatType.MP, value);
		}
	}
	public float SPD
	{
		get { return this.getStat(StatType.MP); }
		set {
			this.setStat(StatType.MP, value);
		}
	}
	public float STR
	{
		get { return this.getStat(StatType.MP); }
		set {
			this.setStat(StatType.MP, value);
		}
	}
	public float DEX
	{
		get { return this.getStat(StatType.MP); }
		set {
			this.setStat(StatType.MP, value);
		}
	}
	public float INT
	{
		get { return this.getStat(StatType.MP); }
		set {
			this.setStat(StatType.MP, value);
		}
	}
	// getters and setters for base stats.
	// This is for more permanant changes.
	public float BaseHP
	{
		get { return this.getBaseStatOrZero(StatType.HP); }
		set {
			this.setOrAddBaseStat(StatType.HP, value);
		}
	}
	public float BaseMP
	{
		get { return this.getBaseStatOrZero(StatType.MP); }
		set {
			this.setOrAddBaseStat(StatType.MP, value);
		}
	}
	public float BaseSPD
	{
		get { return this.getBaseStatOrZero(StatType.SPD); }
		set {
			this.setOrAddBaseStat(StatType.SPD, value);
		}
	}
	public float BaseSTR
	{
		get { return this.getBaseStatOrZero(StatType.STR); }
		set {
			this.setOrAddBaseStat(StatType.STR, value);
		}
	}
	public float BaseDEX
	{
		get { return this.getBaseStatOrZero(StatType.DEX); }
		set {
			this.setOrAddBaseStat(StatType.DEX, value);
		}
	}
	public float BaseINT
	{
		get { return this.getBaseStatOrZero(StatType.INT); }
		set {
			this.setOrAddBaseStat(StatType.INT, value);
		}
	}
	// getter and setter for Condition, so we can be sure to have stats reflect this
	public ConditionType Condition
	{
		get { return this.condition; }
		set {
			// set a pending condition and let updateCondition() function handle it.
			this.pendingCondition = value;
			updateCondition();
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
		// TODO: just use the Unity object name instead?
		if (this.characterName == "") {
			this.characterName = "FartFace";
		}
		// For now, show thats in the debug log.
		var allStats = this.getAllStats();
		this.debugLogStatDictionary(allStats);
		this.Condition = ConditionType.DEAD;
		allStats = this.getAllStats();
		this.debugLogStatDictionary(allStats);
		// TODO: display stats on screen somehow.
	}

	// return the base stat if it exists, otherwise return 0
	public float getBaseStatOrZero(StatType type)
	{
		float thisStat;
		if (this.baseStats.TryGetValue(type, out thisStat)) {
			return thisStat;
		}
		return 0f;
	}

	// return the mod stat if it exists, otherwise return 0
	public float getStatMod(StatType type)
	{
		float thisStat;
		if (this.modStats.TryGetValue(type, out thisStat)) {
			return thisStat;
		}
		return 0f;
	}

	// return the maximum amount a stat can be modified relative to the base stat
	public float getMaxAboveBase(StatType type)
	{
		float thisStat;
		if (this.maxAboveBase.TryGetValue(type, out thisStat)) {
			return thisStat;
		}
		return 0f;
	}

	// return the minimum amount a stat can be modified relative to 0
	public float getMinBelowBase(StatType type)
	{
		float thisStat;
		if (this.minBelowBase.TryGetValue(type, out thisStat)) {
			return thisStat;
		}
		return 0f;
	}

	// return the maximum amount an effective stat can be
	public float getMaxStat(StatType type)
	{
		return this.getBaseStatOrZero(type) + this.getMaxAboveBase(type);
	}

	// return the minimum amount an effective stat can be
	public float getMinStat(StatType type)
	{
		return this.getMinBelowBase(type);
	}

	// get the effective stat, with all modifiers applied.
	public float getStat(StatType type)
	{
		return this.getBaseStatOrZero(type) + this.getStatMod(type);
	}

	// return all effective stats in one Dictionary
	public Dictionary<StatType, float> getAllStats()
	{
		Dictionary<StatType, float> allStats = new Dictionary<StatType, float>();
		foreach(StatType stat in Enum.GetValues(typeof(StatType))) {
			if (stat != StatType.NONE) {
				allStats.Add(stat, this.getStat(stat));
			}
		}
		return allStats;
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

	// set a modifier for a specified stat to the specified value
	public void setStatMod(StatType type, float value)
	{
		// don't do anything if the flag is set for this stat to be immutable.
		if ((immutableStats & type) == type) return;
		// LEFT OFF HEEEEEREEEEEEEEEEEEEEEEEEEEEEEEEEEEEE debug and see if functions are doing what i think they are.
		// make sure the value we set doesn't go beyond either the upper or lower limits
		float newValue = Mathf.Clamp(value, this.getMinStat(type), this.getMinStat(type));
		// don't bother adding a mod if its 0.
		if (newValue == 0f) return;
		// set the mod, add the item in the dictionary if it doesn't exist
		if (this.modStats.ContainsKey(type)) {
			this.modStats[type] = value;
		} else {
			this.modStats.Add(type, value);
		}
	}

	// Add the specified value to a modifier for the specified stat
	public void modStat(StatType type, float value)
	{
		// set the stat mod relative to its current mod value
		this.setStatMod(type, this.getStatMod(type) + value);
	}


	// set a modifier so that the effective stat of that type will equal the given value
	public void setStat(StatType type, float value)
	{
		// don't do anything if the flag is set for this stat to be immutable.
		if ((immutableStats & type) == type) return;
		// set the stat mod based on what the base stat is.
		this.setStatMod(type, value - this.getBaseStatOrZero(type));
		// bitwise AND operation to determine if the flag is in the defined stat set for stats that Condition depends on.
    	if ((this.conditionStats & type) == type) {
    		this.updateCondition();
    	}
	}

	// remove all modifiers on a stat.
	public void removeStatMod(StatType type)
	{
		// don't do anything if the flag is set for this stat to be immutable.
		if ((immutableStats & type) == type) return;
		// remove any items for this stat from the stat mod dictionary
		if (this.modStats.ContainsKey(type)) {
			this.modStats.Remove(type);
		}
	}

	// check all stats which would change the condition of this character
	public void updateCondition()
	{
		// check if a forced condition is pending
		if (this.pendingCondition != ConditionType.NONE) {
			// force the stats to what they should be for this condition
			if (this.pendingCondition == ConditionType.DEAD && this.HP != 0f) {
				Debug.Log("yeah it ded");
				// manually set the effective hp to 0
				this.setStatMod(StatType.HP, 0f - this.getBaseStatOrZero(StatType.HP));
			} else if (pendingCondition != ConditionType.DEAD && this.HP == 0f) {
				// set the hp to the default revive percentage
				this.setStatMod(StatType.HP, this.getBaseStatOrZero(StatType.HP) * StatExtensions.defaultRevivePercent);
			}
			// If all stats were able to be modified, the logic below will update the condition.
		}
		// if HP is below 0, this character is dead no matter what
		if (this.condition != ConditionType.DEAD && this.HP <= 0f) {
			this.condition = ConditionType.DEAD;
			Debug.Log("Oh shit, "+this.characterName+" just deadified.");
			// clear any pending condition
			this.pendingCondition = ConditionType.NONE;
			// TODO: consider making HP immutable, so that the character has to be "revived" before they can heal?
			return;
		}
		// check if the player has has come back from death
		if (this.condition == ConditionType.DEAD && this.HP > 0f && this.pendingCondition == ConditionType.NONE) {
			// if there is no pending condition, default to healthy for now
			this.pendingCondition = ConditionType.HEALTHY;
		}
		// if there is a condition to change to, do it
		if (pendingCondition != ConditionType.NONE) {
			this.condition = this.pendingCondition;
			Debug.Log("Its ok, "+this.characterName+" is back in a "+this.condition.ToString()+" state");
			// clear any pending condition
			this.pendingCondition = ConditionType.NONE;
		}
	}

	// TODO: migrate this to a utilities class
	public static int rollDiceDropLowest(int diceAmount, int diceSides, int dropAmount)
	{
		List<int> diceResults = new List<int>();
		int total = 0;
		int count = 0;
		for (int i = 0; i < diceAmount; i++) {
			diceResults.Add(UnityEngine.Random.Range(1, diceSides+1));
		}
		diceResults.Sort((a, b) => -1* a.CompareTo(b)); // descending sort
		// add up everything except the ones we will drop
		foreach (int value in diceResults) {
			if (count < diceAmount-dropAmount) {
				total += value;
			}
			count++;
		}
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

	// write a line in the debug window showing each element in the dictionary specified
	public void debugLogStatDictionary(Dictionary<StatType, float> theseStats)
	{
		foreach (KeyValuePair<StatType, float> stat in theseStats) {
			Debug.Log(""+stat.Key.ToString()+": "+stat.Value.ToString());
		}
	}

	// write a line in the debug window showing each base stat for this character
	public void debugLogBaseStats()
	{
		Debug.Log("Base Stats for Character with the name \""+this.characterName+"\" will be displayed next.");
		this.debugLogStatDictionary(baseStats);
	}

	/**
	 * --------------------------------------------------
	 * Function for typical actions on a character
	 * --------------------------------------------------
	 */

	// check if this character is dead
	public bool isDead()
	{
		return (this.condition == ConditionType.DEAD) ? true : false;
	}

	// check if this character is dead
	public bool isAlive()
	{
		return !this.isDead();
	}

	// heal this character by a specified amount. A positive value would Incease effective HP.
	public void heal(float amount) {
		this.modStat(StatType.HP, amount);
	}

	// damage this character by a specified amount. A positive value would Decrease effective HP.
	public void damage(float amount) {
		this.modStat(StatType.HP, 0f - amount);
	}

	// return HP to its "full" value
	public void fullHeal() {
		this.removeStatMod(StatType.HP);
	}

	// kill the character if they are not already dead
	public void kill()
	{
		this.HP = 0;
	}

	// revive the character from death using the current revive percentage
	public void revive()
	{

	}
	
	// TODO: function to apply a temporary stat boost or debuff with different such as time or turns or ticks, whatever.

	// TODO: function to rise this objects statistics to the next "level"
}