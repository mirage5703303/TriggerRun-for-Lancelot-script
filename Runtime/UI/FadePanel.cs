using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class FadePanel : MonoBehaviour
{   public GameObject PanelIn;
    public GameObject PanelOut;
    public float Timer=0;
    public bool automatic=false;
    // Start is called before the first frame update
    void Start()
    {

    }
    public void TransitionPanel(){
        Debug.Log("entro TransitionPanel");
        panelOutAndPanelIn(PanelOut,PanelIn,Timer);

    }
    public void TransitionPanelAutomatic(){
        if(PanelOut.GetComponent<CanvasGroup>().alpha==1){ 
            panelOutAndPanelIn(PanelOut,PanelIn,Timer);
        }else{
              panelOutAndPanelIn(PanelIn,PanelOut,Timer);
        }
    }
     public void panelOutAndPanelIn(GameObject panelOut,GameObject panelIn,float time){

        fadeOut(panelOut,time);
        fadeIn(panelIn,time);
       
    }


    public void fadeOut(GameObject panel,float time){
         Debug.Log("entro fadeout");
	    StartCoroutine(FadeToAlpha(panel.GetComponent<CanvasGroup>(), 0,time));
       panel.GetComponent<CanvasGroup>().blocksRaycasts=false;
	}
    public void fadeIn(GameObject panel,float time){
        Debug.Log("entro fadein");
	StartCoroutine(FadeToAlpha(panel.GetComponent<CanvasGroup>(), 1,time));
    panel.GetComponent<CanvasGroup>().blocksRaycasts=true;
        
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
}
