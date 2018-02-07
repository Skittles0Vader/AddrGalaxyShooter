using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AddressableAssets;

public class LoaderScript : MonoBehaviour {

    public string enemyPrefabName;

	// Use this for initialization
	void Start () {
        if (enemyPrefabName != null)
            Addressables.InstantiateAsync<GameObject>(enemyPrefabName);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
