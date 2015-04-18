using UnityEngine;
using System.Collections;

public class CharacterInventory : MonoBehaviour {


	private bool  Menu = false;
	private bool  ItemMenu = true;
	private bool  EquipMenu = false;
	
	public int[] ItemSlot = new int[16];
	public int[] ItemQuantity = new int[16];
	public int[] Equipment = new int[8];
	
	public int 	 WeaponEquip = 0;
	public bool  AllowWeaponUnequip = false;
	public int 	 ArmorEquip = 0;
	public bool  AllowArmorUnequip = true;
	public GameObject[] Weapon = new GameObject[1];
	
	
	public int Cash = 500;

	void  Start (){
		/*
			GetComponent<Status>().FinalRangeAtk = 0;
			GetComponent<Status>().FinalRangeDef = 0;
			GetComponent<Status>().FinalMeleeAtk = 0;
			GetComponent<Status>().FinalMeleeDef = 0;
			GetComponent<Status>().WeaponRangeAtk = 0;
			GetComponent<Status>().WeaponMeleeAtk = 0;
		*/

		GetComponent<CharacterStatus>().CalculateStatus();
		
	}

}