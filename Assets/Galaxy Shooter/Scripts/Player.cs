using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
    // √ Player has 3 lives
    // √ Collision btwn Enemy & Player, enemy is completely destroyed + 
    // √ Player loses 1 life each time...0 lives and the player is completely destroyed too.
    // Everytime the laser hits the Enemy, destroy both the Enemy & the laser
    // other.laser, other.player -- i am in the enemy when I am in the AI script.

    [SerializeField]
    private GameObject _explosionPrefab;

    /* Add Rigid bodies on who is responsible for the collision (Player has rigid body, PowerUps do not.)
     * Laser is responsible for hitting the enemy, so added Rigid Body to that.
     */
	public bool canTripleShot = false;	// powerUp variable
    public bool canSpeedUp = false;     // Speed powerUp variable
    public bool shieldsAreUp = false;   // shieldsUp variable
    public int  Lives = 3;              // Player has 3 lives. 

	// https://docs.unity3d.com/ScriptReference/SerializeField.html
	[SerializeField]
	private float _speed = 5.0f;	// 5 meters / sec

	[SerializeField]
	private GameObject _laserPrefab;

    [SerializeField]
    private GameObject _tripleShotPrefab;

    [SerializeField]
    private GameObject _shieldsGameObject;

	[SerializeField]
	private float _fireRate = 0.25f;	// Cooldown System: Firerate
	private float _canFire = 0.0f;		// how much time has passed, keep private.



	// Use this for initialization
	void Start () {
        // Debug.Log("Name:" + name + "\tPosition:" +transform.position);
		// current pos = new pos
		transform.position = new Vector3(0, 0, 0);

	}
	
	// Update is called once per frame (60fps)
	void Update ()
	{
		Movement ();

        // if space key pressed, then spawn laser at player position
		// (11/06/2017) Addding get Left Mouse Button, Apple mouse has no Left Button? what happens - any Mouse Press works. 
		if (Input.GetKeyDown (KeyCode.Space) || Input.GetMouseButton (0)) { Shoot(); }
	}

	private void Shoot ()
	{
		if (Time.time > _canFire) {

            //spawn my laser: if triple shot, shoot 3 lasers
			if (canTripleShot) {
                // creating a triple shot prefab instead of 3 lasers each time.
                Instantiate(_tripleShotPrefab, transform.position, Quaternion.identity);
                /*// basic way to approach
                Instantiate (_laserPrefab, transform.position + new Vector3 (-0.55f, 0.06f, 0f), Quaternion.identity);  // left (side)
				Instantiate (_laserPrefab, transform.position + new Vector3 (0f,     0.88f, 0f),     Quaternion.identity);	// center (top)
				Instantiate (_laserPrefab, transform.position + new Vector3 (0.55f,  0.06f, 0f),  Quaternion.identity);	// right (side)
				*/
			} else {
				Instantiate (_laserPrefab, transform.position + new Vector3 (0f, 0.88f, 0f), Quaternion.identity);		// center (top)
			}
			_canFire = Time.time + _fireRate; // sets  1/4 of a second cooldown time between each laser 

		}

	}

	private void Movement ()
	{
		// Debug.Log("Audrey! " + Time.time.ToString());

		float horizontalInput = Input.GetAxis ("Horizontal");	// wrap the movement on the x axis
		float verticalInput = Input.GetAxis ("Vertical");	// 0 4.2

        // If speed boost enabled, Move 1.5x normal speed
        // else move normal speed
        float newSpeed = _speed;
        if (canSpeedUp){ newSpeed *= 5.0f;}


		// transform.Translate(new Vector3(1,0,0));
        transform.Translate (Vector3.right * newSpeed * horizontalInput * Time.deltaTime); 
        transform.Translate (Vector3.up * newSpeed * verticalInput * Time.deltaTime); 


		// player on Y > 0, set pos to 0 (hold at 0)
		// player on Y < -4.2f, set pos to -4.2f (hold at -4.2f)
		float topbound    =  0.0f;
		float bottombound = -4.2f;

		if (transform.position.y > topbound)        { transform.position = new Vector3 (transform.position.x, topbound,    0); } 
		else if (transform.position.y < bottombound) { transform.position = new Vector3 (transform.position.x, bottombound, 0); }


		// player on X > 9.5 (offscreen), set pos to Left (wrap)
		// player on X < 9.5 (offscreen), set pos to Right (wrap)
		float leftbound = -9.5f;
		float rightbound = 9.5f;

		if (transform.position.x > rightbound)    { transform.position = new Vector3 (leftbound,  transform.position.y, 0); }
		else if (transform.position.x < leftbound) { transform.position = new Vector3 (rightbound, transform.position.y, 0); }

	}

    // TRIPLE SHOT
    public void TripleShotPowerUpOn() 
    {
        // turn on the triple shot and the timer
        canTripleShot = true;
        StartCoroutine(TripleShotPowerDownRoutine());
    }

    public IEnumerator TripleShotPowerDownRoutine()
    {
        // suspend execution for 5 seconds
        yield return new WaitForSeconds(5.0f);
        canTripleShot = false;
    }

    // SPEED BOOST
    public void SpeedBoostPowerUpOn()
    {
        // turn on the triple shot and the timer
        canSpeedUp = true;
        StartCoroutine(SpeedBoostPowerDownRoutine());
    }

    public IEnumerator SpeedBoostPowerDownRoutine()
    {
        // suspend execution for 5 seconds
        yield return new WaitForSeconds(5.0f);
        canSpeedUp = false;
    }

    // SHEILDS
    public void ShieldsPowerUpOn()
    {
        // turn on the shield ability == 1 free hit from the enemy, 
        // if hit no damage to player, just turn shields off
        shieldsAreUp = true;
        _shieldsGameObject.SetActive(true); // turn on visual
    }

    public void Damage()
    {
        Debug.Log("Player has been shot, shields are " + shieldsAreUp + ", # lives =" + Lives);

        // Shields up, player gets a free hit = no damage, turn shields off, and exit
        if (shieldsAreUp) {
            //1 free hit from the enemy, no damage to player, just turn shields off
            shieldsAreUp = false;
            _shieldsGameObject.SetActive(false); // turn off visual

            return;
        }

        // subtract 1 life from the player
        Lives--;

        if (Lives < 1) { 
            // show the explosion animation b4 destroying self
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
            Destroy(this.gameObject); 
        }
    }
}
