using UnityEngine;
using System.Collections;

[RequireComponent (typeof (MainPlayerController))]
[RequireComponent (typeof (CharacterEngine))]
[RequireComponent (typeof (CharacterInventory))]
[RequireComponent (typeof (CharacterSkills))]
public class PlayerAttack : MonoBehaviour {

	public enum WhileAttackState{
		MeleeForward = 0,
		Immobile = 1,
		WalkFree = 2
	}

	public Transform AttackPoint; 
	public Transform LowerAttackPoint; 
	public Transform OnWallAttackPoint; 
	public bool CanCharge = false;

	public Transform AttackPrefab;
	public Transform JumpAttackPrefab;
	public WhileAttackState WhileAttack = WhileAttackState.MeleeForward;
	
	public ChargeAttack[] Charge = new ChargeAttack[1];
	private bool Charging = false;
	private GameObject ChargingEffect;
	
	private bool AttackDelay = false;
	public float AttackDelayTime = 0.1f;
	public bool  Freeze = false;
	
	public float AttackSpeed = 0.15f;
	private float NextFire = 0.0f;
	
	public float AttackAnimationSpeed = 1.0f;
	
	public SkillAttack[] Skill = new SkillAttack[3];
	public Texture2D SkillSelectIcon;
	
	private bool  MeleeForward = false;
	public bool  IsCasting = false;
	
	private int ComboIndex = 0;
	private int ConCombo = 0;
	
	public Transform MainCamera;
	public GameObject MainCameraPrefab;
	
	public int RangeAttack = 0;
	public int MeleeAttack = 0;
	public int ChargeIndex;

	public int SkillEquip  = 0;

	public AttackSound DefaultSound;
	
	void  Awake (){
		gameObject.tag = "Player";
		if(DefaultSound.HurtVoice){
			GetComponent<CharacterStatus>().HurtVoice = DefaultSound.HurtVoice;
		}

		RangeAttack = GetComponent<CharacterStatus>().FinalRangeAtk;
		MeleeAttack = GetComponent<CharacterStatus>().FinalMeleeAtk;
		
		//--------------------------------
		//Spawn new Attack Point if you didn't assign it.
		if(!AttackPoint){
			GameObject newAtkPoint = new GameObject();
			newAtkPoint.transform.position = this.transform.position;
			newAtkPoint.transform.rotation = this.transform.rotation;
			newAtkPoint.transform.parent = this.transform;
			AttackPoint = newAtkPoint.transform;	
		}
		if(!LowerAttackPoint){
			LowerAttackPoint = AttackPoint;
		}
		if(!OnWallAttackPoint){
			OnWallAttackPoint = AttackPoint;
		}
		if(!JumpAttackPrefab){
			JumpAttackPrefab = AttackPrefab;
		}

	}
	
	
	void  Update (){
		CharacterStatus status = GetComponent<CharacterStatus>();
		if(Freeze || AttackDelay || Time.timeScale == 0.0f || status.Freeze){
			//Cancel Charge
			if(Input.GetButtonUp("Fire1") && Charging || Input.GetKeyUp("j") && Charging){
				Charging = false;
				if(ChargingEffect){
					Destroy(ChargingEffect.gameObject);
				}
			}
			return;
		}
		CharacterController controller = GetComponent<CharacterController>();
		if (status.IsFear){
			Vector3 lui = transform.TransformDirection(Vector3.back);
			controller.Move(lui * 6* Time.deltaTime);
			if(Input.GetButtonUp("Fire1") && Charging || Input.GetKeyUp("j") && Charging){
				Charging = false;
				if(ChargingEffect){
					Destroy(ChargingEffect.gameObject);
				}
			}
			return;
		}
		if (MeleeForward){
			Vector3 lui = transform.TransformDirection(Vector3.forward);
			controller.Move(lui * 3 * Time.deltaTime);
		}

		if (Input.GetButton("Fire1") && Time.time > NextFire && !IsCasting && !Charging || Input.GetKey("j") && Time.time > NextFire && !IsCasting && !Charging) {
			if(Time.time > (NextFire + 0.5f)){
				ComboIndex = 0;
			}
			//Attack Combo
			ConCombo++;
			if(controller.collisionFlags == CollisionFlags.None){
				StartCoroutine(AttackCombo(JumpAttackPrefab));
			}else{
				StartCoroutine(AttackCombo(AttackPrefab));
			}

			//Charging Weapon if the Weapon can Charge and player hold the Attack Button
			if(CanCharge && !Charging && Time.time > NextFire /2){
				Charging = true;
				int index = Charge.Length -1;
				while(index >= 0){
					Charge[index].CurrentChargeTime = Time.time + Charge[index].ChargeTime;
					index--;
				}
			}
		}
		
		//Charging Effect
		if(Input.GetButton("Fire1") && Charging || Input.GetKey("j") && Charging) {
			int index = Charge.Length -1;
			while(index >= 0){
				if(Time.time > Charge[index].CurrentChargeTime){
					if(Charge[index].ChargeEffect && ChargingEffect != Charge[index].ChargeEffect){
						
						if(!ChargingEffect || ChargeIndex != index){
							if(ChargingEffect){
								Destroy(ChargingEffect.gameObject);
							}
							ChargingEffect = Instantiate(Charge[index].ChargeEffect , transform.position, transform.rotation) as GameObject;
							ChargingEffect.transform.parent = this.transform;
							ChargeIndex = index;
						}
					}
					index = -1;
				}else{
					index--;
				}
			}

		}
		
		//Release Charging
		if(Input.GetButtonUp("Fire1") && Charging || Input.GetKeyUp("j") && Charging){
			Charging = false;
			int index = Charge.Length -1;
			if(ChargingEffect){
				Destroy(ChargingEffect.gameObject);
			}
			while(index >= 0){
				if(Time.time > Charge[index].CurrentChargeTime){
						//Charge Shot!!
					if(Time.time > (NextFire + 0.5f)){
						ComboIndex = 0;
					}
					ConCombo = 1;
					StartCoroutine(AttackCombo(Charge[index].ChargeAttackPrefab));
					index = -1;
				}else{
					index--;
				}
			}
		}

		if(Charging && !Input.GetKey("j") && !Input.GetButton("Fire1")){
			Charging = false;
		}

		//Range
		if(Input.GetKeyDown("1") && !IsCasting && Skill[0].SkillPrefab){
			SkillEquip = 0;
			Debug.Log("EQUIPED 1");
		}
		if(Input.GetKeyDown("2") && !IsCasting && Skill[1].SkillPrefab){
			SkillEquip = 1;
			Debug.Log("EQUIPED 2");
		}
		if(Input.GetKeyDown("3") && !IsCasting && Skill[2].SkillPrefab){
			SkillEquip = 2;
			Debug.Log("EQUIPED 3");
		}
		if (Input.GetButtonDown("Fire2") && Time.time > NextFire && !IsCasting && Skill[SkillEquip].SkillPrefab && !status.Blind || Input.GetKeyDown("i") && Time.time > NextFire && !IsCasting && Skill[SkillEquip].SkillPrefab && !status.Blind) {
			StartCoroutine(RangeSkill(SkillEquip));
		}
		
		//Stop Stand Attack Animation While Moving
		if(WhileAttack == WhileAttackState.WalkFree && IsCasting){
			if(Input.GetButton("Horizontal") || controller.collisionFlags == CollisionFlags.None || GetComponent<MainPlayerController>().Dashing){
				//TODO stop combo attack animation
			}
		}
		
		if(!GetComponent<MainPlayerController>().Dashing){
			//TODO Stop Dashing Animation
		}
		
	}


	IEnumerator  AttackCombo (Transform atkBullet){

		RangeAttack = GetComponent<CharacterStatus>().FinalRangeAtk;
		MeleeAttack = GetComponent<CharacterStatus>().FinalMeleeAtk;

		Transform bulletShootout;
		IsCasting = true;


		if(WhileAttack == WhileAttackState.MeleeForward && !GetComponent<MainPlayerController>().OnWallSlide){
			GetComponent<CharacterEngine>().CanControl = false;
			StartCoroutine(MeleeDash());
		}
			// If Immobile
		if(WhileAttack == WhileAttackState.Immobile && !GetComponent<MainPlayerController>().OnWallSlide){
			GetComponent<CharacterEngine>().CanControl = false;
		}
		if(DefaultSound.AttackComboVoice.Length > ComboIndex && DefaultSound.AttackComboVoice[ComboIndex]){
			GetComponent<AudioSource>().clip = DefaultSound.AttackComboVoice[ComboIndex];
			GetComponent<AudioSource>().Play();
		}
		CharacterController controller = GetComponent<CharacterController>();
		float Wait = 0.0f;
		while(ConCombo > 0){

			if(GetComponent<MainPlayerController>().OnWallSlide){
						// TODO Play Attack Animation while player on the wall
						// AttackAnimationSpeed
			}else if(controller.collisionFlags == CollisionFlags.None){
						// TODO Play Attack Animation while player in Mid Air
						// AttackAnimationSpeed
			}else if(GetComponent<MainPlayerController>().Dashing){
						// TODO Play Attack Animation while player dashing
						// AttackAnimationSpeed
			}else{
						// TODO Play Attack Animation while player standing
						// AttackAnimationSpeed
				if(WhileAttack == WhileAttackState.WalkFree){
							// TODO MOVING ATTACK
							// AttackAnimationSpeed;
				}
			}

					//TODO WAIT UNTIL ANIMATION END
			Wait = 0.02f;

			yield return new WaitForSeconds(AttackDelayTime);
			ComboIndex++;

			NextFire = Time.time + AttackSpeed;
			if(GetComponent<MainPlayerController>().Dashing){
						//Spawn Bullet in Lower Attack Point (While Dashing)
				bulletShootout = Instantiate(atkBullet, LowerAttackPoint.transform.position , LowerAttackPoint.transform.rotation) as Transform;
				bulletShootout.GetComponent<AttackStatus>().Setting(RangeAttack , MeleeAttack , "Player" , this.gameObject);
			}else if(GetComponent<MainPlayerController>().OnWallSlide){
						//Spawn Bullet in On Wall Attack Point
				bulletShootout = Instantiate(atkBullet, OnWallAttackPoint.transform.position , OnWallAttackPoint.transform.rotation) as Transform;
				bulletShootout.GetComponent<AttackStatus>().Setting(RangeAttack , MeleeAttack , "Player" , this.gameObject);
			}else{
						//Spawn Bullet in Attack Point
				bulletShootout = Instantiate(atkBullet, AttackPoint.transform.position , AttackPoint.transform.rotation) as Transform;
				bulletShootout.GetComponent<AttackStatus>().Setting(RangeAttack , MeleeAttack , "Player" , this.gameObject);
			}
			ConCombo -= 1;

			if(ComboIndex >= ConCombo){
				ComboIndex = 0;
				AttackDelay = true;
				yield return new WaitForSeconds(Wait);
				AttackDelay = false;
			}else{
				yield return new WaitForSeconds(AttackSpeed);
			}

		}

		IsCasting = false;
		GetComponent<CharacterEngine>().CanControl = true;


	}

	
	IEnumerator  MeleeDash (){
		MeleeForward = true;
		yield return new WaitForSeconds(0.2f);
		MeleeForward = false;
		
	}
	
	//---------------------
	IEnumerator  RangeSkill ( int skillIndex  ){

		RangeAttack = GetComponent<CharacterStatus>().FinalRangeAtk;
		MeleeAttack = GetComponent<CharacterStatus>().FinalMeleeAtk;
		CharacterController controller = GetComponent<CharacterController>();
		if(GetComponent<CharacterStatus>().Mana >= Skill[skillIndex].ManaCost && !GetComponent<CharacterStatus>().Blind){
			if(DefaultSound.CastVoice){
				GetComponent<AudioSource>().clip = DefaultSound.CastVoice;
				GetComponent<AudioSource>().Play();
			}
			IsCasting = true;
			if(!GetComponent<MainPlayerController>().OnWallSlide){
				GetComponent<CharacterEngine>().CanControl = false;
			}

			if(GetComponent<MainPlayerController>().OnWallSlide){
					//TODO Play Skill Animation while player on the wall
			}else if(controller.collisionFlags == CollisionFlags.None){
					//TODO Play Skill Animation while player in Mid Air
			}else{
					//TODO Play Skill Animation while on the ground
			}

			NextFire = Time.time + Skill[skillIndex].SkillDelay;
				//Transform bulletShootout;
			yield return new WaitForSeconds(Skill[skillIndex].CastTime);
			if(GetComponent<MainPlayerController>().OnWallSlide){
					//Spawn Bullet in On Wall Attack Point
				Transform bulletShootout = Instantiate(Skill[skillIndex].SkillPrefab, OnWallAttackPoint.transform.position , OnWallAttackPoint.transform.rotation) as Transform;
				bulletShootout.GetComponent<AttackStatus>().Setting(RangeAttack , MeleeAttack , "Player" , this.gameObject);
			}else{
				Transform bulletShootout = Instantiate(Skill[skillIndex].SkillPrefab, AttackPoint.transform.position , AttackPoint.transform.rotation) as Transform;
				bulletShootout.GetComponent<AttackStatus>().Setting(RangeAttack , MeleeAttack , "Player" , this.gameObject);
			}

			yield return new WaitForSeconds(Skill[skillIndex].SkillDelay);
			IsCasting = false;
			GetComponent<CharacterEngine>().CanControl = true;
			GetComponent<CharacterStatus>().Mana -= Skill[skillIndex].ManaCost;
		}



	}

	public void WhileAttackSet(int watk){
		if (watk == 2) {
			WhileAttack = WhileAttackState.WalkFree;
		} else if (watk == 1) {
			WhileAttack = WhileAttackState.Immobile;
		} else {
			WhileAttack = WhileAttackState.MeleeForward;
		}
	}
	
	public void ResetWeapon(int ch){
		Charging = false;
		if(ChargingEffect){
			Destroy(ChargingEffect.gameObject);
		}
		Charge = new ChargeAttack[ch];
	}


	[System.Serializable]
	public class SkillAttack {
		public Texture2D Icon;
		public Transform SkillPrefab;
		public string SkillAnimation;
		public string JumpSkillAnimation;
		public string OnWallSkillAnimation;
		public float CastTime = 0.3f;
		public float SkillDelay = 0.3f;
		public int ManaCost = 10;
	}

	[System.Serializable]
	public class ChargeAttack{
		public GameObject ChargeEffect;
		public Transform ChargeAttackPrefab;
		public float ChargeTime = 1.0f;
		public float CurrentChargeTime = 1.0f;
	}


	//----------Sounds-------------
	[System.Serializable]
	public class AttackSound {
		public AudioClip[] AttackComboVoice = new AudioClip[3];
		public AudioClip CastVoice;
		public AudioClip HurtVoice;
	}

}
