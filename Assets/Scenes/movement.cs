//////////// Movement Script

using UnityEngine;
using Mirror;

public class Movement : NetworkBehaviour {
	
	public float moveSpeed = 5f;
	public float verticalSpeedMultiplier = 0.3f;
	public float rotateSpeed = 60f;
	public bool haveControl = false;
	
	void Update()
	{
		if (!isLocalPlayer) return;

		// Horizontal movement
		float horizontal = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
		float vertical = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

		// Apply vertical speed multiplier to W/S movement (up/down)
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
		{
			vertical *= verticalSpeedMultiplier;
		}

		transform.Translate(horizontal, vertical, 0);

		// Rotation
		if (Input.GetKey(KeyCode.Q))
			transform.Rotate(Vector3.forward * rotateSpeed * Time.deltaTime);
		if (Input.GetKey(KeyCode.E))
			transform.Rotate(-Vector3.forward * rotateSpeed * Time.deltaTime);
	}
}