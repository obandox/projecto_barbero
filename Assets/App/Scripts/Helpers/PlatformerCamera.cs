using UnityEngine;
using System.Collections;

public class PlatformerCamera : MonoBehaviour {

	public Transform target;

	public float distance = 10.0f;

	public float height = 5.0f;
	public float cameraHeight = 2.0f;

	public float heightDamping = 2.0f;
	public float rotationDamping = 3.0f;

	void Start(){
		if(!target){
			target = GameObject.FindWithTag("Player").transform;
		}
	}
	
	void  LateUpdate (){

		if (!target)
		return;
		
		float wantedHeight= target.position.y + height;
		
		float currentRotationAngle= transform.eulerAngles.y;
		float currentHeight= transform.position.y;
		

		currentHeight = Mathf.Lerp (currentHeight, wantedHeight, heightDamping * Time.deltaTime);

		Quaternion currentRotation= Quaternion.Euler (0, currentRotationAngle, 0);
		

		transform.position = target.position;
		transform.position -= currentRotation * Vector3.forward * distance;

		transform.position = new Vector3 (transform.position.x , currentHeight , transform.position.z);
		

		Vector3 lookOn = new Vector3(target.position.x , target.position.y + cameraHeight , target.position.z);
		transform.LookAt (lookOn);

	}
}
