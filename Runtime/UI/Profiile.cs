using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Profiile : MonoBehaviour
{public  Image ProfilePreview ;
 public Sprite[] photos;
    // Start is called before the first frame update
    void Start()
    {
        
    }

public void changeSprite(int number){
ProfilePreview.sprite =photos[number];
}
}
