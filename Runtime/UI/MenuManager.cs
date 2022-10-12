using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Ui.module.manager
{
public class MenuManager : MonoBehaviour
{    [Header("Panel")]
    [SerializeField] public CanvasGroup termsAndConditions;
    [SerializeField] public CanvasGroup autentication;
    [SerializeField] public CanvasGroup mainPanels;
     [Header("backround")]
     [SerializeField] public GameObject backgroundSplash;
    [Header("Character")]
     [SerializeField] public GameObject character;
    // Start is called before the first frame update

    public void termsAndConditionsToAutentication(){
      //  panelOutAndPanelIn(termsAndConditions,autentication,0.45f);
        panelOutAndPanelIn(termsAndConditions,autentication,0.45f);
        autentication.GetComponent<Canvas>().enabled=true;
        Debug.Log("entro functions");
    }
    public void autenticationToMainPanels(){
        mainPanels.blocksRaycasts=true;
        panelOutAndPanelIn(autentication,mainPanels,0.45f);
        backgroundSplash.SetActive(false);
       StartCoroutine(FadeOncharacter());

    }

        public void panelOutAndPanelIn(CanvasGroup panelOut,CanvasGroup panelIn,float time){
        fadeOut(panelOut,time);
        fadeIn(panelIn,time);
    }


    public void fadeOut(CanvasGroup panel,float time){
         Debug.Log("entro fadeout");
	    StartCoroutine(FadeToAlpha(panel.GetComponent<CanvasGroup>(), 0,time));
	}
    public void fadeIn(CanvasGroup panel,float time){
        Debug.Log("entro fadein");
	StartCoroutine(FadeToAlpha(panel.GetComponent<CanvasGroup>(), 1,time));
        
	}

    
 
  IEnumerator FadeToAlpha(CanvasGroup canvasGroup, float targetAlpha, float fadeTime)
 {
         float startingAlpha = canvasGroup.alpha;
 
         for (float i = 0; i < 1; i+= Time.deltaTime/fadeTime)
         {
             canvasGroup.alpha = Mathf.Lerp(startingAlpha, targetAlpha, i);
 
             yield return new WaitForFixedUpdate();
         }
 
 
         canvasGroup.alpha = targetAlpha;
 }

 public void onCharacter(){
     StartCoroutine(FadeOncharacter());
 }
  public void offCharacter(){
     StartCoroutine(FadeOffcharacter());
 }
   IEnumerator FadeOncharacter(){
       yield return new WaitForSeconds(0.22f);
           character.SetActive(true);

       
   }
      IEnumerator FadeOffcharacter(){
       yield return new WaitForSeconds(0.22f);
           character.SetActive(false);

       
   }
 
}
}
