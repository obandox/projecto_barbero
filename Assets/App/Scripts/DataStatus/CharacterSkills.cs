using UnityEngine;
using System.Collections;

public class CharacterSkills : MonoBehaviour {

	
	public int[] Skill = new int[3];
	public int[] SkillListSlot = new int[16];

	public LearnSkillLVL[] LearnSkill = new LearnSkillLVL[2];
	
	private bool  Menu = false;
	private bool  ShortcutPage = true;
	private bool  SkillListPage = false;
	private int   SkillSelect = 0;

	private bool  ShowSkillLearned = false;
	private string ShowSkillName = "";

	void  Awake (){
	}
	
	void  Start (){		
		//AssignAllSkill();		
	}
	
	void  Update (){
		
	}
	

	

	public void LearnSkillByLevel(int lvl){
		int index = 0;
		while(index < LearnSkill.Length){
			if(lvl >= LearnSkill[index].Level){
				AddSkill(LearnSkill[index].SkillId);
			}
			index++;
		}
		
	}

	void AddSkill(int id){
		int index = 0;
		while(index < SkillListSlot.Length){
			if(SkillListSlot[index] == id){
				break;
			}else if(SkillListSlot[index] == 0){
				SkillListSlot[index] = id;
				StartCoroutine(ShowLearnedSkill(id));
				break;
			}else{
				index++;
			}
			
		}
		
	}
	
	IEnumerator ShowLearnedSkill(int id){
		ShowSkillLearned = true;
		ShowSkillName = "default";
		yield return new WaitForSeconds(10.5f);
		ShowSkillLearned = false;
		
	}
	[System.Serializable]
	public class LearnSkillLVL {
		public int Level = 1;
		public int SkillId = 1;
	}
}