using UnityEngine;
using System.Collections;

public class AttackStatus : MonoBehaviour {
	
	public int Damage = 10;
	public int DamageMax = 20;
	
	private int CharacterAttack = 5;
	public int TotalDamage = 0;
	public int Variance = 15;
	public string ShooterTag = "Player";

	public GameObject Shooter;
	
	public Transform Popout;
	
	public GameObject HitEffect;
	public bool  IsFear = false;
	public bool  Penetrate = false;
	private int PopDamage = 0;

	public enum AtkType {
		Range = 0,
		Melee = 1,
	}
	
	public AtkType AttackType = AtkType.Range;

	public enum Elemental{
		Normal = 0,
		Fire = 1,
		Ice = 2,
		Earth = 3,
		Lightning = 4,
	}
	public Elemental element = Elemental.Normal;
	
	void  Start (){
		if(Variance >= 100){
			Variance = 100;
		}
		if(Variance <= 1){
			Variance = 1;
		}

	}
	
	public void  Setting ( int rangeAttack  ,   int meleeAttack  ,   string tag  ,   GameObject owner  ){
		if(AttackType == AtkType.Range){
			CharacterAttack = meleeAttack;
		}else{
			CharacterAttack = rangeAttack;
		}
		ShooterTag = tag;
		Shooter = owner;
		int varMin = 100 - Variance;
		int varMax = 100 + Variance;
		int randomDmg = Random.Range(Damage, DamageMax);
		TotalDamage = (randomDmg + CharacterAttack) * Random.Range(varMin ,varMax) / 100;
	}

	
	void  OnTriggerEnter ( Collider other  ){  	
		
		if(ShooterTag == "Player" && other.tag == "Enemy"){	  
			Transform damgeOut = Instantiate(Popout, other.transform.position , transform.rotation) as Transform;
			
			if(AttackType == AtkType.Range){
				PopDamage = other.GetComponent<CharacterStatus>().OnRangeDamage(TotalDamage , (int)element);
			}else{
				PopDamage = other.GetComponent<CharacterStatus>().OnMeleeDamage(TotalDamage , (int)element);
			}
			if(PopDamage < 1){
				PopDamage = 1;
			}
			damgeOut.GetComponent<DamagePopout>().Damage = PopDamage.ToString();				
			if(HitEffect){
				Instantiate(HitEffect, transform.position , transform.rotation);
			}
			if(IsFear){
				other.GetComponent<CharacterStatus>().Fear();
			}
			if(!Penetrate){
				Destroy (gameObject);
			}
		}else if(ShooterTag == "Enemy" && other.tag == "Player"){
			if(AttackType == AtkType.Range){
				PopDamage = other.GetComponent<CharacterStatus>().OnRangeDamage(TotalDamage , (int)element);
			}else{
				PopDamage = other.GetComponent<CharacterStatus>().OnMeleeDamage(TotalDamage , (int)element);
			}
			Transform damgeOut = Instantiate(Popout, transform.position , transform.rotation) as Transform;	
			if(PopDamage < 1){
				PopDamage = 1;
			}
			damgeOut.GetComponent<DamagePopout>().Damage = PopDamage.ToString();
			
			if(HitEffect){
				Instantiate(HitEffect, transform.position , transform.rotation);
			}
			if(IsFear){
				other.GetComponent<CharacterStatus>().Fear();
			}
			if(!Penetrate){
				Destroy (gameObject);
			}
		}else if(ShooterTag == "Player" && other.tag == "Guard"){
			Transform damgeOut = Instantiate(Popout, transform.position , transform.rotation) as Transform;	
			damgeOut.GetComponent<DamagePopout>().Damage = "Guard";
			if(HitEffect){
				Instantiate(HitEffect, transform.position , transform.rotation);
			}
			Destroy (gameObject);
		}
	}
}