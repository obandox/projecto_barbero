using UnityEngine;
using System.Collections;

[RequireComponent (typeof (CharacterStatus))]
[RequireComponent (typeof (CharacterEngine))]

public class ZombieAI : MonoBehaviour {
	
	public enum ZombieAIState { Moving = 0, Pausing = 1 , Idle = 2 , Patrol = 3}

	private ZombieAIState FollowState;

	public bool SnapToPlayerX = true;

	public Transform FollowTarget;
	public float ApproachDistance = 2.0f;
	public float DetectRange = 15.0f;
	public float LostSight = 100.0f;
	public float Speed = 4.0f;
	
	public bool  Stability = false;
	
	public bool  Freeze = false;
	
	public Transform AttackPrefab;
	public Transform AttackPoint;

	public float AttackCast = 0.3f;
	public float AttackDelay = 0.5f;
	
	private float Distance = 0.0f;
	private int RangeAtk = 0;
	private int MeleeAtk = 0;

	public bool  CancelAttack = false;
	private bool  Attacking = false;
	private bool  CastSkill = false;
	private GameObject[] PlayerObjects;

	public AudioClip AttackVoice;
	public AudioClip HurtVoice;
	private GameObject Closest;
	void  Start (){
		Closest = GameObject.FindWithTag("Player"); 
		PlayerObjects = GameObject.FindGameObjectsWithTag("Player"); 
		gameObject.tag = "Enemy"; 
		FollowTarget = GameObject.FindWithTag("Player").transform;
		if(SnapToPlayerX){
			transform.position = new Vector3(FollowTarget.transform.position.x , transform.position.y , transform.position.z);
		}
		if(!AttackPoint){
			AttackPoint = this.transform;
		}

		if(HurtVoice){
			GetComponent<CharacterStatus>().HurtVoice = HurtVoice;
		}

		RangeAtk = GetComponent<CharacterStatus>().RangeAtk;
		MeleeAtk = GetComponent<CharacterStatus>().MeleeAtk;
		
		FollowState = ZombieAIState.Idle;
		
		//TODO PLAY IDLE ANIMATION
		
	}
	
	Vector3  GetDestination (){
		Vector3 destination = FollowTarget.position;
		destination.y = transform.position.y;
		return destination;
	}
	
	void  Update (){
		var status = GetComponent<CharacterStatus>();
		CharacterController controller = GetComponent<CharacterController>();
		PlayerObjects = GameObject.FindGameObjectsWithTag("Player");  
			if (PlayerObjects.Length > 0) {
				FollowTarget = FindClosest().transform;
			}
		
		if (status.IsFear){
			CancelAttack = true;
			Vector3 lui = transform.TransformDirection(Vector3.back);
			controller.Move(lui * 5* Time.deltaTime);
			return;
		}
		
		if(Freeze || status.Freeze){
			return;
		}
		
		if(!FollowTarget){
			return;
		}
		if (FollowState == ZombieAIState.Moving) {
			if ((FollowTarget.position - transform.position).magnitude <= ApproachDistance) {
				FollowState = ZombieAIState.Pausing;
				//TODO CALL IDLE ANIMATION
				//
				StartCoroutine(Attack());
			}else if ((FollowTarget.position - transform.position).magnitude >= LostSight)
			{
				status.Health = status.MaxHealth;
				FollowState = ZombieAIState.Idle;
				//TODO CALL IDLE ANIMATION
			}else {
				Vector3 forward = transform.TransformDirection(Vector3.forward);
				controller.Move(forward * Speed * Time.deltaTime);
				
				Vector3 destiny = FollowTarget.position;
				destiny.y = transform.position.y;
				transform.LookAt(destiny);
			}
		}
		else if (FollowState == ZombieAIState.Pausing){
			Vector3 destinya = FollowTarget.position;
			destinya.y = transform.position.y;
			transform.LookAt(destinya);
			
			Distance = (transform.position - GetDestination()).magnitude;
			if (Distance > ApproachDistance) {
				FollowState = ZombieAIState.Moving;
				//TODO WALK ANIMATION
			}
		}
		//----------------Idle Mode--------------
		else if (FollowState == ZombieAIState.Idle){
			Vector3 destinyheight = FollowTarget.position;
			destinyheight.y = transform.position.y - destinyheight.y;
			int getHealth = status.MaxHealth - status.Health;
			
			Distance = (transform.position - GetDestination()).magnitude;
			if (Distance < DetectRange && Mathf.Abs(destinyheight.y) <= 4 || getHealth > 0){
				FollowState = ZombieAIState.Moving;
				//TODO CALL WALK ANIMATION
			}
		}
		//-----------------------------------
	}
	
		
	IEnumerator  Attack (){
		CancelAttack = false;
		Transform bulletShootout;
		var status = GetComponent<CharacterStatus>();
		if(!status.IsFear || !status.Freeze || !Freeze || !Attacking){
			Freeze = true;
			Attacking = true;
			//TODO ANIMATION ATACK
			if(AttackVoice){
				GetComponent<AudioSource>().clip = AttackVoice;
				GetComponent<AudioSource>().Play();
			}
			yield return new WaitForSeconds(AttackCast);		
			if(!CancelAttack){
				if(AttackPrefab){
					bulletShootout = Instantiate(AttackPrefab, AttackPoint.transform.position , AttackPoint.transform.rotation) as Transform;
				}
				//bulletShootout.GetComponent<AttackStatus>().Setting(RangeAtk , MeleeAtk , "Enemy" , this.gameObject);
				yield return new WaitForSeconds(AttackDelay);
				Freeze = false;
				Attacking = false;
				//TODO CALL WALK ANIM
				CheckDistance();
			}else{
				Freeze = false;
				Attacking = false;
			}

		}
		
	}
	
	void  CheckDistance (){
		if(!FollowTarget){
			//TODO CALL IDLE ANIM
			FollowState = ZombieAIState.Idle;
			return;
		}
		float Distancea = (FollowTarget.position - transform.position).magnitude;
		if (Distancea <= ApproachDistance){
			Vector3 destinya = FollowTarget.position;
			destinya.y = transform.position.y;
			transform.LookAt(destinya);
			StartCoroutine(Attack());
			//Attack();
		}else{
			FollowState = ZombieAIState.Moving;
			//TODO CALL WALK ANIM
		}
	}
	
	
	GameObject FindClosest (){ 
		if(PlayerObjects.Length > 0){
			float Distance = Mathf.Infinity; 
			Vector3 position = transform.position; 			
			foreach(GameObject go in PlayerObjects) { 
				Vector3 diff = (go.transform.position - position); 
				float curDistance = diff.sqrMagnitude; 
				if (curDistance < Distance) { 
					Closest = go; 
					Distance = curDistance;
				} 
			} 
		}
		return Closest; 
	}

	public void ActivateSkill( Transform skill  ,   float castTime  ,   float delay  ,   string anim  ,   float dist  ){
		StartCoroutine(UseSkill(skill ,AttackCast, AttackDelay , anim , dist));
	}


	public IEnumerator  UseSkill ( Transform skill  ,   float castTime  ,   float delay  ,   string anim  ,   float dist  ){
		CancelAttack = false;
		var status = GetComponent<CharacterStatus>();
		if(!status.IsFear && FollowTarget && (FollowTarget.position - transform.position).magnitude < dist && !status.Blind && !status.Freeze  && !CastSkill){
			Freeze = true;
			CastSkill = true;
			//TODO LAUNCH ANIM IN SKILL

			yield return new WaitForSeconds(castTime);

			if(!CancelAttack){
				Transform bulletShootout = Instantiate(skill, AttackPoint.transform.position , AttackPoint.transform.rotation) as Transform;
				//bulletShootout.GetComponent<AttackStatus>().Setting(RangeAtk , MeleeAtk , "Enemy" , this.gameObject);
				yield return new WaitForSeconds(delay);
				Freeze = false;
				CastSkill = false;
				//TODO WALK ANIM 
			}else{
				Freeze = false;
				CastSkill = false;
			}
			

		}

		
	}
	

}