using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climable : MonoBehaviour
{
   //reference - full climbing system in 10 minutes - unity tutorial - Dave/GameDevelopment

   [Header("TARGET FOR CLIMBING")] [Space(5)]
   public float climb = 2f;
   
   void Start()
    {
        
    }
    
    void Update()
    {
        
    }

    public void StartClimb(float amount)
    {
        climb -= amount;
        if (climb<=0f)
        {
            Climb();
        }
    }

    void Climb()
    {
        Destroy(gameObject);
    }
    
}
