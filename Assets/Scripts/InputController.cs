using UnityEngine;
using System.Collections;

public class InputController : MonoBehaviour {
	static InputController _instance;
	public static InputController Instance { get { return _instance; } }
	
	// Use this for initialization
	void Start () {
		_instance = this;
	}
	
	// Update is called once per frame
	void Update () {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    //Debug.Log("Throw Plane");
        //    FlightController fc = FlightController.Instance;
        //    fc.Throw();
        //}
	}
}
