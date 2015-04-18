using UnityEngine;
using System.Collections;


[RequireComponent (typeof (CharacterInventory))]
[RequireComponent (typeof (CharacterSkills))]
public class CharacterStatus : MonoBehaviour {
	public static CharacterStatus Singleton;

	public string CharacterUID;
	public string PlayerName = "Player";



	public int Level = 1;
	public int MeleeAtk = 0;
	public int MeleeDef = 0;
	public int RangeAtk = 0;
	public int RangeDef = 0;

	public int Exp = 0;
	public int MaxExp = 100;
	public float ExpIncrement = 1.37f;

	public int MaxHealth = 100;
	public int Health = 100;
	public int MaxMana = 100;
	public int Mana = 100;

	public int StatusPoint = 0;
	private bool  Dead = false;
	
	public int FinalRangeAtk = 0;
	public int FinalRangeDef = 0;
	public int FinalMeleeAtk = 0;
	public int FinalMeleeDef = 0;
	public int FinalHPpercent = 0;
	public int FinalMPpercent = 0;


	public Transform DeathBody;
	
	public string SpawnPointName = ""; 
	
	//---------States----------
	public int BuffRangeAtk = 0;
	public int BuffRangeDef = 0;
	public int BuffMeleeAtk = 0;
	public int BuffMeleeDef = 0;
	
	public int WeaponRangeAtk = 0;
	public int WeaponMeleeAtk = 0;
	
	//Negative Buffs
	public bool Poison = false;
	public bool Blind = false;
	public bool Web = false;
	public bool Stun = false;
	public bool Freeze = false; 
	
	//Positive Buffs
	public bool  Brave = false; 
	public bool  RangeBarrier = false;
	public bool  Meleebarrier = false;
	public bool  Faith = false; 
	
	public bool  IsFear = false;

	
	//Effect
	public GameObject PoisonEffect;
	public GameObject BlindEffect;
	public GameObject StunEffect;
	

	// 0 = Normal , 1 = Fire , 2 = Ice , 3 = Earth , 4 = Lightning
	public NaturalElement[] ElementEffective = new NaturalElement[5]{
		new NaturalElement(){ Name = "Normal"},
		new NaturalElement(){ Name = "Fire"},
		new NaturalElement(){ Name = "Ice"},
		new NaturalElement(){ Name = "Earth"},
		new NaturalElement(){ Name = "Lightning"}
	};

	public Resistence StatusResist;

	public AudioClip HurtVoice;
	
	void  Awake (){
		CharacterUID = Util.GetUniqueID();
		if(!Singleton){
			Singleton = this;
		}
	}
	
	public int OnRangeDamage ( int amount  ,   int element  ){
		if (!Dead) {
			if(HurtVoice){
				GetComponent<AudioSource>().clip = HurtVoice;
				GetComponent<AudioSource>().Play();
			}
			amount -= RangeDef;
			amount -= FinalRangeDef;
			amount -= BuffRangeDef;
		
			amount *= ElementEffective [element].Effective;
			amount /= 100;
		
			if (amount < 1) {
					amount = 1;
			}
		
			Health -= amount;
		
			if (Health <= 0) {
					Health = 0;
					enabled = false;
					Dead = true;
					Death ();
			}

		}
		return amount;
	}

	public int OnMeleeDamage ( int amount   ,   int element  ){
		if(!Dead){
			if(HurtVoice){
				GetComponent<AudioSource>().clip = HurtVoice;
				GetComponent<AudioSource>().Play();
			}
			amount -= MeleeDef;
			amount -= FinalMeleeDef;
			amount -= BuffMeleeDef;
		
			amount *= ElementEffective[element].Effective;
			amount /= 100;
		
			if(amount < 1){
				amount = 1;
			}
		
			Health -= amount;
		
			if (Health <= 0){
				Health = 0;
				enabled = false;
				Dead = true;
				Death();
			}
		}
		return amount;
	}
	
	public void  Heal ( int hp  ,   int mp  ){
		Health += hp;
		if (Health >= MaxHealth){
			Health = MaxHealth;
		}		
		Mana += mp;
		if (Mana >= MaxMana){
			Mana = MaxMana;
		}
	}
	
	
	void  Death (){
		if(gameObject.tag == "Player"){
			SaveTempData();
		}
		Destroy(gameObject);
		if(DeathBody){
			Instantiate(DeathBody, transform.position , transform.rotation);
		}else{
			print("This Object didn't assign the Death Body");
		}
	}

	public void  GainEXP ( int gain  ){
		Exp += gain;
		if(Exp >= MaxExp){
			int remain = Exp - MaxExp;
			LevelUp(remain);
		}
	}
	
	public void  LevelUp ( int remainingEXP  ){
		
		Exp = remainingEXP;
		Level++;
		StatusPoint += 5;


		MaxExp = (int)( MaxExp  * ExpIncrement);
		MaxHealth += 20;
		MaxMana += 10;



		Health = MaxHealth;
		Mana = MaxMana;

		GainEXP(0);

		if(GetComponent<CharacterSkills>()){
			GetComponent<CharacterSkills>().LearnSkillByLevel(Level);
		}

	}
	
	void  SaveTempData(){
		string temp_prefix = "_temp_";

		PlayerPrefs.SetString(temp_prefix+"MapName", Application.loadedLevelName);
		PlayerPrefs.SetInt(temp_prefix+"PreviousSave", 10);
		PlayerPrefs.SetInt(temp_prefix+"PlayerLevel", Level);

		PlayerPrefs.SetInt(temp_prefix+"PlayerRangaATK", RangeAtk);
		PlayerPrefs.SetInt(temp_prefix+"PlayerRangaDEF", RangeDef);

		PlayerPrefs.SetInt(temp_prefix+"PlayerMeleeATK", MeleeAtk);
		PlayerPrefs.SetInt(temp_prefix+"PlayerMeleeDEF", MeleeDef);

		PlayerPrefs.SetInt(temp_prefix+"PlayerEXP", Exp);
		PlayerPrefs.SetInt(temp_prefix+"PlayerMaxEXP", MaxExp);
		PlayerPrefs.SetInt(temp_prefix+"PlayerMaxHP", MaxHealth);
		PlayerPrefs.SetInt(temp_prefix+"PlayerMaxMP", MaxMana);

		PlayerPrefs.SetInt(temp_prefix+"PlayerSP", StatusPoint);
		
		PlayerPrefs.SetInt(temp_prefix+"Cash", GetComponent<CharacterInventory>().Cash);

		int itemSize = GetComponent<CharacterInventory>().ItemSlot.Length;
		int index = 0;
		if(itemSize > 0){
			while(index < itemSize){
				PlayerPrefs.SetInt(temp_prefix+"Item_" + index.ToString(), GetComponent<CharacterInventory>().ItemSlot[index]);
				PlayerPrefs.SetInt(temp_prefix+"Item_Qty_" + index.ToString(), GetComponent<CharacterInventory>().ItemQuantity[index]);
				index++;
			}
		}
		
		int equipSize = GetComponent<CharacterInventory>().Equipment.Length;
		index = 0;
		if(equipSize > 0){
			while(index < equipSize){
				PlayerPrefs.SetInt(temp_prefix+"Equipment_" + index.ToString(), GetComponent<CharacterInventory>().Equipment[index]);
				index++;
			}
		}
		PlayerPrefs.SetInt(temp_prefix+"Weapon_Equip", GetComponent<CharacterInventory>().WeaponEquip);
		PlayerPrefs.SetInt(temp_prefix+"Armor_Equip", GetComponent<CharacterInventory>().ArmorEquip);

		//Save Skill Slot
		index = 0;
		while(index <= 2){
			PlayerPrefs.SetInt(temp_prefix+"Skill_" + index.ToString(), GetComponent<CharacterSkills>().Skill[index]);
			index++;
		}
		//Skill List Slot
		index = 0;
		while(index < GetComponent<CharacterSkills>().SkillListSlot.Length){
			PlayerPrefs.SetInt(temp_prefix+"SkillList_" + index.ToString(), GetComponent<CharacterSkills>().SkillListSlot[index]);
			index++;
		}
		print("Saved");
	}
	
	public void  CalculateStatus (){

		FinalRangeAtk = RangeAtk + BuffRangeAtk + WeaponRangeAtk;

		FinalMeleeAtk = MeleeAtk + BuffMeleeAtk + WeaponMeleeAtk;

		int hpPer = MaxHealth * FinalHPpercent / 100;
		int mpPer = MaxMana * FinalMPpercent / 100;
		MaxHealth += hpPer;
		MaxMana += mpPer;
		
		if (Health >= MaxHealth){
			Health = MaxHealth;
		}
		if (Mana >= MaxMana){
			Mana = MaxMana;
		}
	}

	public IEnumerator  OnPoison ( int hurtTime  ){
		return OnPoison(hurtTime , 2);
	}
	//----------States--------
	public IEnumerator  OnPoison ( int hurtTime  , int perc){
		int amount = 0;
		GameObject eff = null;
		if(!Poison){
			int chance= 100;
			chance -= StatusResist.PoisonResist;
			if(chance > 0){
				int per= Random.Range(0, 100);
				if(per <= chance){
					Poison = true;
					amount = MaxHealth * perc / 100; 
				}
			
			}
		//--------------------
			while(Poison && hurtTime > 0){
				if(PoisonEffect){ 
					eff = Instantiate(PoisonEffect, transform.position, PoisonEffect.transform.rotation) as GameObject;
					eff.transform.parent = transform;
				}
				yield return new WaitForSeconds(0.7f); 
				Health -= amount;
			
				if (Health <= 1){
					Health = 1;
				}
				if(eff){ 
					Destroy(eff.gameObject);
				}
				hurtTime--;
				if(hurtTime <= 0){
					Poison = false;
				}
			}
		}
	}

	
	public IEnumerator OnBlind ( float dur  ){
		GameObject eff = null;
		if(!Blind){
			int chance= 100;
			chance -= StatusResist.BlindResist;
			if(chance > 0){
				int per= Random.Range(0, 100);
				if(per <= chance){
						Blind = true;
					if(BlindEffect){
						eff = Instantiate(BlindEffect, transform.position, transform.rotation) as GameObject;
						eff.transform.parent = transform;
					}
						yield return new WaitForSeconds(dur);
						if(eff){ 
							Destroy(eff.gameObject);
						}
						Blind = false;
				}
				
			}

		}
	}

	public IEnumerator  OnStun ( float dur  ){
		GameObject eff = null;
		if(!Stun){
			int chance= 100;
			chance -= StatusResist.StunResist;
			if(chance > 0){
				int per= Random.Range(0, 100);
				if(per <= chance){
					Stun = true;
					Freeze = true; 
					if(StunEffect){
						eff = Instantiate(StunEffect, transform.position, StunEffect.transform.rotation) as GameObject;
						eff.transform.parent = transform;
					}
					//TODO ACTIVE STUN ANIM
					yield return new WaitForSeconds(dur);
					if(eff){ //Destroy Effect if it still on a map
						Destroy(eff.gameObject);
					}
					//TODO DEACTIVE STUN ANIM
					Freeze = false; // Freeze Character Off
					Stun = false;
				}
				
			}

		}
		
	}

	public void  ApplyAbnormalStat ( int statId  ,   float dur  ){
		if(statId == 0){
			StartCoroutine(OnPoison(Mathf.FloorToInt(dur)));
		}
		if(statId == 1){
			StartCoroutine(OnBlind(dur));
		}
		if(statId == 2){
			StartCoroutine(OnStun(dur));
		}
		
	}
	
	public IEnumerator  OnRangeBarrier ( int amount  ,   float dur  ){
		if(!RangeBarrier){
			RangeBarrier = true;
			BuffRangeDef = 0;
			BuffRangeDef += amount;
			CalculateStatus();
			yield return new WaitForSeconds(dur);
			BuffRangeDef = 0;
			RangeBarrier = false;
			CalculateStatus();
		}
		
	}

	public IEnumerator  OnMeleeBarrier ( int amount  ,   float dur  ){
		if(!Meleebarrier){
			Meleebarrier = true;
			BuffMeleeDef = 0;
			BuffMeleeDef += amount;
			CalculateStatus();
			yield return new WaitForSeconds(dur);
			BuffMeleeDef = 0;
			Meleebarrier = false;
			CalculateStatus();
		}

	}

	public IEnumerator  OnBrave ( int amount  ,   float dur  ){
		if(!Brave){
			Brave = true;
			BuffMeleeAtk = 0;
			BuffMeleeAtk += amount;
			CalculateStatus();
			yield return new WaitForSeconds(dur);
			BuffMeleeAtk = 0;
			Brave = false;
			CalculateStatus();
		}
		
	}
	
	public IEnumerator  OnFaith ( int amount  ,   float dur  ){
		if(Faith){
			Faith = true;
			BuffRangeAtk = 0;
			BuffRangeAtk += amount;
			CalculateStatus();
			yield return new WaitForSeconds(dur);
			BuffRangeAtk = 0;
			Faith = false;
			CalculateStatus();
		}
		
	}

	public void  ApplyBuff ( int statId  ,   float dur  ,   int amount  ){
		if(statId == 1){
			//Melee Defense
			StartCoroutine(OnMeleeBarrier(amount , dur));
		}
		if(statId == 2){
			//Range Defense
			StartCoroutine(OnRangeBarrier(amount , dur));
		}
		if(statId == 3){
			//MEELE Attack
			StartCoroutine(OnBrave(amount , dur));
		}
		if(statId == 4){
			//RANGE Attack
			StartCoroutine(OnFaith(amount , dur));
		}
		
		
	}
	
	public void  Fear (){
		GetComponent<CharacterEngine>().CanControl = false;
		StartCoroutine(KnockBack());
		//TODO ANIM HURT
		GetComponent<CharacterEngine>().CanControl = true;
	}
	
	IEnumerator  KnockBack (){
		IsFear = true;
		yield return new WaitForSeconds(0.2f);
		IsFear = false;
	}


	[System.Serializable]
	public class NaturalElement{
		public string Name = "neutral";
		public int Effective = 100;
	}

	[System.Serializable]
	public class Resistence{
		public int PoisonResist = 0;
		public int BlindResist = 0;
		public int StunResist = 0;
	}
}
