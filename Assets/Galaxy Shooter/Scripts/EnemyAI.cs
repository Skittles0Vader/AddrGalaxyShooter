using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour {

    [SerializeField]
    private GameObject _enemyExplosionPrefab = null;

    // variable for speed
    [SerializeField]
    private float _speed = 5.0f;    // 5 meters / sec


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        
        float topbound = 7.0f;
        float bottombound = -7.0f;
        float leftbound = -7.0f;
        float rightbound = 7.0f;

        // Move Down
        transform.Translate(Vector3.down * _speed * Time.deltaTime);

        // when off the screen on the bottom
        // respawn back on top with the new RANDOM X position with the new bounds on the screen
        if (transform.position.y < bottombound) { 
            // transform.position = new Vector3(transform.position.x, topbound, 0); 
            transform.position = new Vector3(Random.Range(leftbound, rightbound), topbound, 0); 
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("EnemyAI: " + this.name + " collided with " + other.name);

        if (other.tag == "Player")
        {
            // access the player 
            Player player = other.GetComponent<Player>();

            // Collision btwn Enemy & Player, 
            //   Enemy is completely destroyed.
            //   Damage the Player: Player loses 1 life each time
            if (player) { player.Damage(); }

            // show the explosion animation b4 destroying self
            Instantiate(_enemyExplosionPrefab, transform.position, Quaternion.identity);
            // destroy ourselves (aka the Enemy) at end.
            Destroy(this.gameObject);

        }
        else if (other.tag == "Laser") {
            
            // Everytime the laser hits the Enemy, 
            //   Destroy both the laser and Enemy
            // the Laser's parent (L-C-R Lasers for Triple_Shot)
			if (other.transform.parent && other.transform.parent.gameObject) { Destroy(other.transform.parent.gameObject); }
            // the Laser
            Destroy(other.gameObject);

            // show the explosion animation b4 destroying self
            Instantiate(_enemyExplosionPrefab, transform.position, Quaternion.identity);

            // destroy ourselves (aka the Enemy) at end.
            Destroy(this.gameObject);

        }
        else {
            
        }

    }
}
