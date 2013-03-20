using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour {
	static FollowCamera _instance;
	public static FollowCamera Instance { get { return _instance; } }
	
	GameObject player;
	
	public Vector3 dollyDistance;
	
	// Use this for initialization
	void Start () {
		_instance = this;
		player = GameObject.FindGameObjectWithTag(TagsAndLayers.Player);
	}
	
	// Update is called once per frame
	void Update () {
		if (player != null)
		{
			transform.rotation = player.transform.rotation;
			transform.position = player.transform.position + (dollyDistance.z * transform.forward) + (dollyDistance.y * transform.up) + (dollyDistance.x * transform.right);
		}
	}
}
