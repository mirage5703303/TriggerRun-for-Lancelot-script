using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingCreateRoom : MonoBehaviour
{   
     List<string> m_DropOptions = new List<string> 
     
     { "MAUSOLEUM", "VOYAGER","FACTORY","DEEPWELL","ROOFTOP","DAMLOOK","PALACE","DUGEON","DRACULA","ALTAR","GLORY","STARRY","MONASTERIAN","ANGELS","ARENA","HALL"};
    
    [Header("DropDown")]
    public TMPro.TMP_Dropdown listMap;
     [Header("PreviwMap")]
      public  Image mapPreview ;
    [Header("ListPreviws")]
    public Sprite[] maps;
    // Start is called before the first frame update
    void Start(){
           // mapPreview = GetComponent<Image>();
            //Fetch the Dropdown GameObject the script is attached to
            listMap.ClearOptions();
            //Add the options created in the List above
            listMap.AddOptions(m_DropOptions);
        }
    public void changeMapPreviw(int number){
        Debug.Log(m_DropOptions[number].ToString() );
        mapPreview.sprite =maps[number];

    }
}
