using UnityEngine;
using System.Collections.Generic;

public class GameController : MonoBehaviour {
	static GameController _instance;
	public static GameController Instance { get { return _instance; } }
	
	//List<GameObject> hoops = new List<GameObject>();
	
	// Use this for initialization
	void Start () {
		_instance = this;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
