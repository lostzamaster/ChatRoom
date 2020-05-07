using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Showname : MonoBehaviour
{
    private TextMeshPro textMesh;

    public string nametext;
   void Awake()
   {
       textMesh = GetComponent<TextMeshPro>();
   }
    void Start()
   {
      textMesh.text= nametext;
   }
    
        

}
