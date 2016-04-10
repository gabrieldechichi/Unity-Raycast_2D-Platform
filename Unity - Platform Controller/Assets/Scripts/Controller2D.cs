using UnityEngine;
using System.Collections;

[RequireComponent (typeof (BoxCollider2D))]
public class Controller2D : MonoBehaviour {

	public LayerMask collisionMask;
	
	const float skinWidth = .015f;
	public int horizontalRayCount = 4;
	public int verticalRayCount = 4;
	
	float horizontalRaySpacing;
	float verticalRaySpacing;
	
	float maxClimpAngle = 80f;
	float maxDescentAngle = 75f;
	
	BoxCollider2D collider;
	RaycastOrigins raycastOrigins;
	public CollisionInfo collisions;
	
	struct RaycastOrigins {
		public Vector2 topLeft, topRight;
		public Vector2 bottomLeft, bottomRight;
	}
	
	public struct CollisionInfo {
		public bool above, below;
		public bool left, right;
		
		public bool climbingSlope;
		public float slopeAngle, slopeAngleOld;
		
		public void Reset(){
			above = below = false;
			left = right = false;
			climbingSlope = false;
			
			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}
	
	void Start(){
		collider = GetComponent<BoxCollider2D>();
		CalculateRaySpacing();
	}
	
	public void Move(Vector3 velocity){
		UpdateRaycastOrigins();
		collisions.Reset();
		
		if (velocity.y < 0){
			DescendSlope(ref velocity);
		}
		
		if(velocity.x != 0){
			HorizontalCollisions (ref velocity);
		}
		if(velocity.y != 0){
			VerticalCollisions(ref velocity);	
		}		
		
		transform.Translate(velocity);
	}
	
	void VerticalCollisions(ref Vector3 velocity){ //ref takes exactly the velocity, not a copy of it
		float directionY = Mathf.Sign (velocity.y);
		float rayLength = Mathf.Abs (velocity.y) + skinWidth;
		
		for (int i = 0; i< verticalRayCount; i++){
			Vector2 rayOrigin = (directionY == -1)?raycastOrigins.bottomLeft : raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x); //changes the rayOrigin for each iteration, considering the velocity
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up*directionY, rayLength, collisionMask); //Check if there is a collision
			
			Debug.DrawRay(rayOrigin, Vector2.up*directionY*rayLength,Color.red);
			
			if(hit){
				velocity.y = (hit.distance - skinWidth)*directionY;
				rayLength = hit.distance; //So the ray doesn't hit something else there is further away
				
				if(collisions.climbingSlope){
					velocity.x = velocity.y/Mathf.Tan (collisions.slopeAngle * Mathf.Deg2Rad)*Mathf.Sign(velocity.x);
				}
				
				collisions.below = directionY == -1;
				collisions.above = directionY == 1;
				
			}
		}
		
		if (collisions.climbingSlope){
			float directionX = Mathf.Sign (velocity.x);
			rayLength = Mathf.Abs(velocity.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1)?raycastOrigins.bottomLeft:raycastOrigins.bottomRight) + Vector2.up*velocity.y;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.right*directionX,rayLength,collisionMask);
			
			if(hit){
				float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);
				if (slopeAngle != collisions.slopeAngle){
					velocity.x = (hit.distance - skinWidth)*directionX;
				}
			}
		}
	}
	
	void HorizontalCollisions(ref Vector3 velocity){ //ref takes exactly the velocity, not a copy of it
		float directionX = Mathf.Sign (velocity.x);
		float rayLength = Mathf.Abs (velocity.x) + skinWidth;
		
		for (int i = 0; i< horizontalRayCount; i++){
			Vector2 rayOrigin = (directionX == -1)?raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (horizontalRaySpacing * i); //changes the rayOrigin for each iteration, considering the velocity
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right*directionX, rayLength, collisionMask); //Check if there is a collision
			
			Debug.DrawRay(rayOrigin, Vector2.right*directionX*rayLength,Color.red);
			
			if(hit){
			
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				
				if (i== 0 && slopeAngle <= maxClimpAngle){
					float distanceToSlopeStart = 0;
					
					//Make sure we have contact with slope
					if (slopeAngle != collisions.slopeAngleOld){ //Climbing a new slope
						distanceToSlopeStart = hit.distance-skinWidth;
						velocity.x -= distanceToSlopeStart*directionX;				
					}
					ClimbSlope(ref velocity, slopeAngle);
					velocity.x += distanceToSlopeStart *directionX;
				}
				
				if (!collisions.climbingSlope || slopeAngle > maxClimpAngle){
					velocity.x = (hit.distance - skinWidth)*directionX;
					rayLength = hit.distance; //So the ray doesn't hit something else there is further away
					
					if (collisions.climbingSlope){
						velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad)*Mathf.Abs(velocity.x);
					}
					
					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}
			}
		}
	}
	
	void ClimbSlope(ref Vector3 velocity, float slopeAngle){
		float moveDistance = Mathf.Abs(velocity.x);
		float climpVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad)*moveDistance;
		if (velocity.y <= climpVelocityY){
			velocity.y = climpVelocityY	;
			velocity.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad)*moveDistance * Mathf.Sign (velocity.x);
			collisions.below = true; //To enable jump		
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;
		}
	}
	
	void DescendSlope(ref Vector3 velocity, float slopeAngle){
		float directionX = Mathf.Sign (velocity.x);	
						
	}
	
	void UpdateRaycastOrigins(){
		Bounds bounds = collider.bounds	;
		bounds.Expand(skinWidth*-2)	;
		
		raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y) ;
		raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y) ;
		raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y) ;
		raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y) ;		
	}
	
	void CalculateRaySpacing(){
		Bounds bounds = collider.bounds;
		bounds.Expand (skinWidth*-2);
		
		horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
		verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);
		
		horizontalRaySpacing = bounds.size.y/(horizontalRayCount - 1);
		verticalRaySpacing = bounds.size.x/(verticalRayCount - 1);
	}
	
}
