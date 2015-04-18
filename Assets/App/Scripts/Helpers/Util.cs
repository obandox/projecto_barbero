using UnityEngine;
using System;
using System.Collections;

public class Util {
	public static string NewUID(){
		var random = new System.Random();                     
         DateTime epochStart = new System.DateTime(1970, 1, 1, 8, 0, 0, System.DateTimeKind.Utc);
         double timestamp = (System.DateTime.UtcNow - epochStart).TotalSeconds;         
         return Application.systemLanguage                            //Language
				+"-"+Application.platform.ToString()                       //Device    
                 +"-"+String.Format("{0:X}", Convert.ToInt32(timestamp))                //Time
                 +"-"+String.Format("{0:X}", Convert.ToInt32(Time.time*1000000))        //Time in game
                 +"-"+String.Format("{0:X}", random.Next(1000000000));                //random number
          
	}
	public static string GetUniqueID(){
		 string uniqueID = NewUID();
         string key = "ID";              
         if(PlayerPrefs.HasKey(key)){
             uniqueID = PlayerPrefs.GetString(key);            
         } else {            
         	 Debug.Log("Generated Unique ID: "+uniqueID);   
             PlayerPrefs.SetString(key, uniqueID);
             PlayerPrefs.Save();    
         }         
         return uniqueID;
     }
}
