//////////// Movement Script

using UnityEngine;
using Mirror;

public class Movement : NetworkBehaviour {
	
	int moveSpeed = 8;
	float horiz = 0;
	float vert = 0;
	public bool haveControl = false;
	
	public override void OnStartLocalPlayer()
	{
		base.OnStartLocalPlayer();
		Debug.Log("Local player started");
		haveControl = true;  // Automatically give control to local player
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		Debug.Log("Client started");
	}

	void Start()
	{
		Debug.Log("Object started");
		Debug.Log($"Network State - isServer: {isServer}, isClient: {isClient}, isLocalPlayer: {isLocalPlayer}, isOwned: {isOwned}");
		
		// Request authority if we're the local player
		if (isLocalPlayer && !isOwned)
		{
			Debug.Log("Requesting authority");
			NetworkIdentity identity = GetComponent<NetworkIdentity>();
			identity.AssignClientAuthority(connectionToClient);
		}

		if (NetworkServer.active && NetworkClient.active)
		{
			Debug.Log("We are the host");
		}
		else if (NetworkClient.active)
		{
			Debug.Log("We are just a client");
		}
	}

	void FixedUpdate(){
		if(!isLocalPlayer) return;
		
		if(haveControl){
			vert = Input.GetAxis("Vertical");
			horiz = Input.GetAxis("Horizontal");
			Vector3 newVelocity = (transform.right * horiz * moveSpeed) + (transform.forward * vert * moveSpeed);
			Vector3 myVelocity = GetComponent<Rigidbody>().linearVelocity;
			myVelocity.x = newVelocity.x;
			myVelocity.z = newVelocity.z;
			
			if(myVelocity != GetComponent<Rigidbody>().linearVelocity){
				if(isServer){
					CmdMovePlayer(myVelocity);
				}
				else{
					CmdMovePlayer(myVelocity);
				}
			}
		}
	}
	
	[Command]
	void CmdMovePlayer(Vector3 playerVelocity){
		GetComponent<Rigidbody>().linearVelocity = playerVelocity;
		RpcUpdatePlayer(transform.position);
	}
	[ClientRpc]
	void RpcUpdatePlayer(Vector3 playerPos){
		transform.position = playerPos;
	}
}