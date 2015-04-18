using UnityEngine;
using System.Collections;

public class DamagePopout : MonoBehaviour {
	
	Vector3 TargetScreenPosition;
	public string Damage = "0";
	public GUIStyle fontStyle;
	
	public float Duration = 0.5f;
	
	private int Glide = 50;
	private int Height = 100;
	private int Width = 100;
	
	void  Start (){
		Destroy (gameObject, Duration);
		StartCoroutine(RoutineGlide());
		
	}
	
	IEnumerator RoutineGlide(){
		int timer = 0;
		while(timer < 100){
			Glide += 2;
			yield return new WaitForSeconds(0.03f); 
		}
	}
	
	void  OnGUI (){
		TargetScreenPosition = Camera.main.WorldToScreenPoint(transform.position);
		TargetScreenPosition.y = Screen.height - TargetScreenPosition.y - Glide + 6;
		TargetScreenPosition.x = TargetScreenPosition.x - 30;
		if(TargetScreenPosition.z > 0){
			GUI.Label (new Rect (TargetScreenPosition.x,TargetScreenPosition.y,Height,Width), Damage.ToString(),fontStyle);
		}
	}
}