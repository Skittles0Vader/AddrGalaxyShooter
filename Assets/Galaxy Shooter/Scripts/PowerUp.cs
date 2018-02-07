using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour {
    
    [SerializeField]
    private float _speed = 3.0f;

    [SerializeField]
    private int powerUpID;  // 0 = powerUp, 1 = speedBoost, 2 - shields

	// Update is called once per frame
	void Update () {
		
        transform.Translate(Vector3.down * _speed * Time.deltaTime);
	}

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("PowerUp: " + this.name + " collided with " + other.name);

        if (other.tag == "Player") {
            // access the player 
            Player player = other.GetComponent<Player>();

            // check handle to the component for the PowerUp
            if (player){

                switch (powerUpID){
                    case 0: 
                        // Enable triple shot
                        Debug.Log("Triple Shot On");
                        player.TripleShotPowerUpOn();
                        break;

                    case 1: // Enable Speed boost
                        Debug.Log("Speed Boost On");
                        player.SpeedBoostPowerUpOn();
                        break;

                    case 2: // Enable Shields
                        Debug.Log("Shields On");
                        player.ShieldsPowerUpOn();
                        break;

                    default: // no default action...yet.
                        break;
                }


            }

            // destroy ourselves (aka the PowreUp) at end.
            Destroy(this.gameObject);            
        }

    }

}
