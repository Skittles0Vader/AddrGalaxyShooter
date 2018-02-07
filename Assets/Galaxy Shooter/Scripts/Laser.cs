using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour {

	[SerializeField]
	private float _speed = 10.0f;

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		// move up at 10 speed
		transform.Translate (Vector3.up * _speed * Time.deltaTime);

		// 5.40376 -- laser goes off screen (>=6.0f), clean up that memory that the laser takes up (destroy the laser)
		if (transform.position.y >= 6.0f) 
		{
			// Debug.Log("Destroying Laser >" + this.name +"<");
			Destroy(this.gameObject);
		}

	}


    /*
    // Everytime the laser hits the Enemy, destroy both the Enemy & the laser
    // NOT USED 12-2017 - Done in EnemyAI 
    public void hitEnemy() 
    {
        // If this is one of the Triple_Shot L-C-R Lasers it has a parent, Triple_Shot
        // we need to destroy the parent too? (Seems like we should have better memory mgmt.)
		if (transform.parent && transform.parent.gameObject) { Destroy(transform.parent.gameObject); }
        Destroy(this.gameObject); 
    }
    */
}
