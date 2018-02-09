using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AddressableAssets;

public class SpawnManager : MonoBehaviour {

    public string enemyShipPrefab;
    public string[] powerups;
    public string[] spawnAtStart;

    private float topbound = 7.0f;
    // private float bottombound = -7.0f;
    private float leftbound = -7.0f;
    private float rightbound = 7.0f;
    private int startupSpawnCount = 0;

	// Use this for initialization
	void Start () {
        foreach(var item in spawnAtStart) {
            Addressables.InstantiateAsync<GameObject>(item).completed += OnComplete;
        }
  	}        

    void OnComplete(ResourceManagement.IAsyncOperation<GameObject> obj) {
        startupSpawnCount++;
        if (startupSpawnCount == spawnAtStart.Length)
        {
            StartCoroutine(EnemySpawnRoutine());
            StartCoroutine(PowerUpsSpawnRoutine());
        }
    }
	
    IEnumerator EnemySpawnRoutine() {

        while(true) {
            Addressables.InstantiateAsync<GameObject>(enemyShipPrefab).completed += (obj) => {
                var t = obj.result.transform;
                t.SetPositionAndRotation(new Vector3(Random.Range(leftbound, rightbound), topbound, 0), Quaternion.identity);
            };
            
            //Instantiate(enemyShipPrefab, new Vector3(Random.Range(leftbound, rightbound),topbound,0), Quaternion.identity);
            yield return new WaitForSeconds(7.0f);
        }
    }

    IEnumerator PowerUpsSpawnRoutine()
    {

        while (true)
        {
            var rand = Random.Range(0, 3);
            Addressables.InstantiateAsync<GameObject>(powerups[rand]).completed += (obj) => {
                var t = obj.result.transform;
                t.SetPositionAndRotation(new Vector3(Random.Range(leftbound, rightbound), topbound, 0), Quaternion.identity);
            };

            // Using INT, so need to use the count (3) not the last item in array (2) as you would with Floats
            yield return new WaitForSeconds(5.0f);
        }
    }
}
