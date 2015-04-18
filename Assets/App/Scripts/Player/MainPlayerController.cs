using UnityEngine;
using System.Collections;

[RequireComponent (typeof (CharacterEngine))]
[RequireComponent (typeof (CharacterStatus))]
public class MainPlayerController : MonoBehaviour {

	private CharacterEngine Engine;

	public float WalkSpeed = 6.0f;
	public float DashSpeed = 12.0f;
	public float DashDuration = 0.5f;

	public bool GroundDash = true;
	public bool IsAirDash = false;
	public bool DoubleJump = false;
	public bool WallKick = false;

	public GameObject DashEffect;
	public GameObject WallKickEffect;
	public GameObject WallSlideEffect;
	

	public bool Dashing = false;
	private bool JumpingDash = false;
	private bool JumpingDown = false;

	public bool OnWallSlide = false;
	private bool AirJump = false;

	public bool AirMove = false;

	public bool OnWallKick = false;

	public float OriginalX = 0.0f;
	private float Gravity = 20.0f;

	public MovementSound Sound;
	
	// Use this for initialization
	void  Start (){
		Engine = GetComponent<CharacterEngine>();
		Gravity = Engine.Movement.Gravity;
		OriginalX = transform.position.x;
		if(WallSlideEffect){
			WallSlideEffect.SetActive(false);
		}
		if(DashEffect){
			DashEffect.SetActive(false);
		}
	}
	
	// Update is called once per frame
	void  Update (){
		var stat = GetComponent<CharacterStatus>();
		if(transform.position.x != OriginalX){
			transform.position = new Vector3(OriginalX , transform.position.y , transform.position.z);
		}
		if(stat.Freeze || stat.IsFear){
			Engine.SetInputMoveDirection = new Vector3(0,0,0);
			return;
		}
		if(Time.timeScale == 0.0f){
			return;
		}
		CharacterController controller = GetComponent<CharacterController>();
		
		if(JumpingDash){
			//Jump while Dashing
			if(Input.GetButtonUp("Jump")){
				CancelDashJump();
			}
			Vector3 jd = transform.TransformDirection(new Vector3(0 , 2 , Mathf.Abs(Input.GetAxis("Horizontal")) +1));
			controller.Move(jd * 5 * Time.deltaTime);
			return;
		}
		if(JumpingDown){
			Vector3 jdown = transform.TransformDirection(new Vector3(0 , 0 , Mathf.Abs(Input.GetAxis("Horizontal")) +1));
			controller.Move(jdown * 4 * Time.deltaTime);
			return;
		}
		if(OnWallKick){
			//Wall Kick
			Vector3 wk = transform.TransformDirection(new Vector3(0 , 2.5f , -1));
			controller.Move(wk * 6 * Time.deltaTime);
			return;
		}
		
		if(AirJump){
			//Double Jump
			Vector3 aj = transform.TransformDirection(new Vector3(0 , 2.5f , Mathf.Abs(Input.GetAxis("Horizontal"))));
			controller.Move(aj * 4 * Time.deltaTime);
			return;
		}
		
		if (Dashing){
			//Dash
			if(Input.GetKeyUp(KeyCode.LeftShift) || !Engine.CanControl){
				CancelDash();
			}
			if(Input.GetButtonDown("Jump") && (controller.collisionFlags & CollisionFlags.Below) != 0){
				StartCoroutine("DashJump");
			}
			Vector3 fwd = transform.TransformDirection(Vector3.forward);
			controller.Move(fwd * DashSpeed * Time.deltaTime);

			return;
		}

		Vector3 directionVector = new Vector3(0 , 0, Input.GetAxis("Horizontal"));
		
		if (directionVector != Vector3.zero) {
			float directionLength = directionVector.magnitude;
			directionVector = directionVector / directionLength;
			directionLength = Mathf.Min(1, directionLength);
			directionLength = directionLength * directionLength;
			directionVector = directionVector * directionLength;
		}

		if(Input.GetAxis("Horizontal") > 0.1){
			transform.eulerAngles = new Vector3 (transform.eulerAngles.x, 0, transform.eulerAngles.z);
		}else if(Input.GetAxis("Horizontal") < -0.1){
			transform.eulerAngles = new Vector3 (transform.eulerAngles.x, 180, transform.eulerAngles.z);
		}
		Engine.SetInputMoveDirection = new Vector3(0 , 0, Input.GetAxis("Horizontal"));
		
		//Double Jump
		if(Input.GetButtonDown("Jump") && !Engine.Grounded && DoubleJump){
			StartCoroutine("DoubleJumping");
		}
		if (controller.collisionFlags == CollisionFlags.None && OnWallSlide){
			CancelWallSlide();
		}

		if(Input.GetButtonDown("Jump") && Sound.JumpVoice && Engine.Grounded){
			GetComponent<AudioSource>().clip = Sound.JumpVoice;
			GetComponent<AudioSource>().Play();
		}
		if(Input.GetAxis("Horizontal") != 0 && Sound.WalkingSound && !GetComponent<AudioSource>().isPlaying){
			GetComponent<AudioSource>().clip = Sound.WalkingSound;
			GetComponent<AudioSource>().Play();
		}

		//Activate Sprint
		if(Input.GetKeyDown(KeyCode.LeftShift)  && !Dashing && GroundDash){
			//Dash
			if((controller.collisionFlags & CollisionFlags.Below) != 0){
				//StartCoroutine(Dash());
				if(Sound.DashVoice){
					GetComponent<AudioSource>().clip = Sound.DashVoice;
					GetComponent<AudioSource>().Play();
				}
				StartCoroutine("Dash");
			}else if(IsAirDash && !OnWallSlide){
				if(Sound.DashVoice){
					GetComponent<AudioSource>().clip = Sound.DashVoice;
					GetComponent<AudioSource>().Play();
				}
				StartCoroutine("AirDash");
			}
		}
		
		Engine.SetInputJump = Input.GetButton("Jump");
	}
	
	IEnumerator Dash(){
		if(!Dashing){
			if(DashEffect){
				DashEffect.SetActive(true);
			}
			Dashing = true;
			//TODO ANIMIMATOR DASH
			yield return new WaitForSeconds(DashDuration);
			CancelDash();
		}
	}
	
	void CancelDash(){
			StopCoroutine("Dash");
			if(DashEffect){
				DashEffect.SetActive(false);
			}
			//TODO ANIMIMATOR DASH
			Engine.FreezeGravity = false;
			Dashing = false;
	}
	
	IEnumerator AirDash(){
		if(!Dashing && !AirMove){
			if(DashEffect){
				DashEffect.SetActive(true);
			}
			AirMove = true;
			Dashing = true;
			Engine.FreezeGravity = true;
			//TODO ANIMATOR DASH
			yield return new WaitForSeconds(DashDuration);
			Engine.FreezeGravity = false;
			CancelDash();
		}
	}
	
	IEnumerator DoubleJumping(){
		if(!AirMove){
			AirMove = true;
			AirJump = true;
			Engine.FreezeGravity = true;
			yield return new WaitForSeconds(0.2f);
			Engine.FreezeGravity = false;
			AirJump = false;
		}
	}
	
	IEnumerator DashJump(){
			CancelDash();
			Engine.FreezeGravity = true;
			JumpingDash = true;
			yield return new WaitForSeconds(0.25f);
			CancelDashJump();
	}
	
	void CancelDashJump(){
			if(JumpingDash){
				JumpingDown = true;
			}
			JumpingDash = false;
			Engine.FreezeGravity = false;
			StopCoroutine("DashJump");
	}
	//void OnCollisionStay(Collision col) {
	void OnControllerColliderHit(ControllerColliderHit col) {
		CharacterController controller = GetComponent<CharacterController>();
		//Debug.Log("TAG: "+col.gameObject.tag);
		if(JumpingDown){
			JumpingDown = false;
		}
			if(AirMove && WallKick && col.gameObject.tag != "Enemy" && Engine.Grounded){
				AirMove = false;
				Engine.FreezeGravity = false;
			}else if(AirMove && WallKick && col.gameObject.tag != "Enemy" && controller.collisionFlags == CollisionFlags.Sides && col.gameObject.tag == "Wall"){
					AirMove = false;
					Engine.FreezeGravity = false;
			}else if(AirMove && (controller.collisionFlags & CollisionFlags.Below) != 0){
				AirMove = false;
				Engine.FreezeGravity = false;
			}
			
        if(col.gameObject.tag == "Wall"){			
			if(Input.GetButton("Horizontal") && !Engine.Grounded && controller.collisionFlags == CollisionFlags.Sides && Input.GetButton("Jump") && !Engine.Jumping.HoldingJumpButton && WallKick){
				StartCoroutine(WallJump());
			}else if(Input.GetButton("Horizontal") && !Engine.Grounded && controller.collisionFlags == CollisionFlags.Sides && Engine.Movement.Velocity.z == 0 && Engine.Movement.Velocity.y <= 0 && WallKick){
				CancelDashJump();
				OnWallSlide = true;
				if(WallSlideEffect){
					WallSlideEffect.SetActive(true);
				}
				//TODO ANIMATOR WALKSLIDE
				Engine.Movement.Gravity = Gravity / 4;
				Engine.Movement.MaxFallSpeed = 5;
			}else if(OnWallSlide){
				CancelWallSlide();
			}
		}else if(OnWallSlide){
			CancelWallSlide();
		}
    }
	
	void CancelWallSlide(){
				if(WallSlideEffect){
					WallSlideEffect.SetActive(false);
				}
				OnWallSlide = false;
				Engine.Movement.Gravity = Gravity;
				Engine.Movement.MaxFallSpeed = 20;
	}
	
	IEnumerator WallJump(){
				CancelWallSlide();
				CancelDashJump();
				//TODO ANIMATOR WALK KICK JUMP
				Engine.FreezeGravity = true;
				OnWallKick = true;
				if(WallKickEffect){
					Instantiate(WallKickEffect , transform.position , WallKickEffect.transform.rotation);
				}
				if(WallSlideEffect){
					WallSlideEffect.SetActive(false);
				}
				yield return new WaitForSeconds(0.15f);
				Engine.FreezeGravity = false;
				OnWallKick = false;
	}


	//----------Sounds-------------
	[System.Serializable]
	public class MovementSound {
		public AudioClip JumpVoice;
		public AudioClip WalkingSound;
		public AudioClip DashVoice;
	}
}
