using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class CharacterEngine : MonoBehaviour
{

    public enum MovementTransferOnJump
    {
        None,
        InitTransfer,
        PermaTransfer,
        PermaLocked
    }


    public bool CanControl = true;
	public bool FreezeGravity = false;
    public Vector3 SetInputMoveDirection = Vector3.zero;
    public bool SetInputJump = false;
    public bool Grounded = true;
    public Vector3 GroundNormal = Vector3.zero;
    private Vector3 LastGroundNormal = Vector3.zero;

    public CharacterMovement Movement = new CharacterMovement();
    public CharacterEngineJumping Jumping = new CharacterEngineJumping();
    public CharacterEngineMovingPlatform MovingPlatform = new CharacterEngineMovingPlatform();
    public CharacterEngineSetup Sliding = new CharacterEngineSetup();

    private CharacterController Controller;

    void Awake()
    {
        Controller = GetComponent<CharacterController>();
    }

    private void _Update()
    {
        Vector3 Velocity = Movement.Velocity;
        Velocity = ApplyInputVelocityChange(Velocity);
        Velocity = ApplyGravityAndJumping(Velocity);

        Vector3 moveDistance = Vector3.zero;
        if(MoveWithPlatform())
        {
            Vector3 newGlobalPoint = MovingPlatform.ActivePlatform.TransformPoint(MovingPlatform.ActiveLocalPoint);
            moveDistance = (newGlobalPoint - MovingPlatform.ActiveGlobalPoint);
            if(moveDistance != Vector3.zero)
                Controller.Move(moveDistance);

            Quaternion newGlobalRotation = MovingPlatform.ActivePlatform.rotation * MovingPlatform.ActiveLocalRotation;
            Quaternion rotationDiff = newGlobalRotation * Quaternion.Inverse(MovingPlatform.ActiveGlobalRotation);

            var yRotation = rotationDiff.eulerAngles.y;
            if(yRotation != 0)
            {
                transform.Rotate(0, yRotation, 0);
            }
        }

        Vector3 lastPosition = transform.position;
        Vector3 currentMovementOffset = Velocity * Time.deltaTime;

        float pushDownOffset = Mathf.Max(Controller.stepOffset, new Vector3(currentMovementOffset.x, 0, currentMovementOffset.z).magnitude);
        if(Grounded)
            currentMovementOffset -= pushDownOffset * Vector3.up;

        MovingPlatform.HitPlatform = null;
        GroundNormal = Vector3.zero;

        Movement.CollisionFlags = Controller.Move(currentMovementOffset);

        Movement.LastHitPoint = Movement.HitPoint;
        LastGroundNormal = GroundNormal;

        if(MovingPlatform.Enabled && MovingPlatform.ActivePlatform != MovingPlatform.HitPlatform)
        {
            if(MovingPlatform.HitPlatform != null)
            {
                MovingPlatform.ActivePlatform = MovingPlatform.HitPlatform;
                MovingPlatform.LastMatrix = MovingPlatform.HitPlatform.localToWorldMatrix;
                MovingPlatform.IsNewPlatform = true;
            }
        }

        Vector3 oldHVelocity = new Vector3(Velocity.x, 0, Velocity.z);
        Movement.Velocity = (transform.position - lastPosition) / Time.deltaTime;
        Vector3 newHVelocity = new Vector3(Movement.Velocity.x, 0, Movement.Velocity.z);

        if(oldHVelocity == Vector3.zero)
        {
            Movement.Velocity = new Vector3(0, Movement.Velocity.y, 0);
        }
        else
        {
            float projectedNewVelocity = Vector3.Dot(newHVelocity, oldHVelocity) / oldHVelocity.sqrMagnitude;
            Movement.Velocity = oldHVelocity * Mathf.Clamp01(projectedNewVelocity) + Movement.Velocity.y * Vector3.up;
        }

        if(Movement.Velocity.y < Velocity.y - 0.001)
        {
            if(Movement.Velocity.y < 0)
            {
                Movement.Velocity.y = Velocity.y;
            }
            else
            {
                Jumping.HoldingJumpButton = false;
            }
        }

        if(Grounded && !IsGroundedTest())
        {
            Grounded = false;

            if(MovingPlatform.Enabled &&
                (MovingPlatform.MovementTransfer == MovementTransferOnJump.InitTransfer ||
                MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaTransfer)
            )
            {
                Movement.FrameVelocity = MovingPlatform.PlatformVelocity;
                Movement.Velocity += MovingPlatform.PlatformVelocity;
            }

            SendMessage("OnFall", SendMessageOptions.DontRequireReceiver);
            transform.position += pushDownOffset * Vector3.up;
        }
        else if(!Grounded && IsGroundedTest())
        {
            Grounded = true;
            Jumping.Jumping = false;
            SubtractNewPlatformVelocity();

            SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
        }

        if(MoveWithPlatform())
        { 
            MovingPlatform.ActiveGlobalPoint = transform.position + Vector3.up * (Controller.center.y - Controller.height * 0.5f + Controller.radius);
            MovingPlatform.ActiveLocalPoint = MovingPlatform.ActivePlatform.InverseTransformPoint(MovingPlatform.ActiveGlobalPoint);

            MovingPlatform.ActiveGlobalRotation = transform.rotation;
            MovingPlatform.ActiveLocalRotation = Quaternion.Inverse(MovingPlatform.ActivePlatform.rotation) * MovingPlatform.ActiveGlobalRotation;
        }
    }

    void FixedUpdate()
    {
        if(MovingPlatform.Enabled)
        {
            if(MovingPlatform.ActivePlatform != null)
            {
                if(!MovingPlatform.IsNewPlatform)
                {
                    MovingPlatform.PlatformVelocity = (
                        MovingPlatform.ActivePlatform.localToWorldMatrix.MultiplyPoint3x4(MovingPlatform.ActiveLocalPoint)
                        - MovingPlatform.LastMatrix.MultiplyPoint3x4(MovingPlatform.ActiveLocalPoint)
                    ) / Time.deltaTime;
                }
                MovingPlatform.LastMatrix = MovingPlatform.ActivePlatform.localToWorldMatrix;
                MovingPlatform.IsNewPlatform = false;
            }
            else
            {
                MovingPlatform.PlatformVelocity = Vector3.zero;
            }
        }

        _Update();
    }


    private Vector3 ApplyInputVelocityChange(Vector3 Velocity)
    {
        if(!CanControl)
            SetInputMoveDirection = Vector3.zero;

        Vector3 desiredVelocity;
        if(Grounded && TooSteep())
        {
            desiredVelocity = new Vector3(GroundNormal.x, 0, GroundNormal.z).normalized;
            var projectedMoveDir = Vector3.Project(SetInputMoveDirection, desiredVelocity);
            desiredVelocity = desiredVelocity + projectedMoveDir * Sliding.SpeedControl + (SetInputMoveDirection - projectedMoveDir) * Sliding.SidewaysControl;
            desiredVelocity *= Sliding.SlidingSpeed;
        }
        else
            desiredVelocity = GetDesiredHorizontalVelocity();

        if(MovingPlatform.Enabled && MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaTransfer)
        {
            desiredVelocity += Movement.FrameVelocity;
            desiredVelocity.y = 0;
        }

        if(Grounded)
            desiredVelocity = AdjustGroundVelocityToNormal(desiredVelocity, GroundNormal);
        else
            Velocity.y = 0;

        float maxVelocityChange = GetMaxAcceleration(Grounded) * Time.deltaTime;
        Vector3 VelocityChangeVector = (desiredVelocity - Velocity);
        if(VelocityChangeVector.sqrMagnitude > maxVelocityChange * maxVelocityChange)
        {
            VelocityChangeVector = VelocityChangeVector.normalized * maxVelocityChange;
        }

        if(Grounded || CanControl)
            Velocity += VelocityChangeVector;

        if(Grounded)
        {
            Velocity.y = Mathf.Min(Velocity.y, 0);
        }

        return Velocity;
    }

    private Vector3 ApplyGravityAndJumping(Vector3 Velocity)
    {
		if(FreezeGravity){
			Velocity.y = 0;
			return Velocity;
		}
        if(!SetInputJump || !CanControl)
        {
            Jumping.HoldingJumpButton = false;
            Jumping.LastButtonDownTime = -100;
        }

        if(SetInputJump && Jumping.LastButtonDownTime < 0 && CanControl)
            Jumping.LastButtonDownTime = Time.time;

        if(Grounded)
            Velocity.y = Mathf.Min(0, Velocity.y) - Movement.Gravity * Time.deltaTime;
        else
        {
            Velocity.y = Movement.Velocity.y - Movement.Gravity * Time.deltaTime;

            if(Jumping.Jumping && Jumping.HoldingJumpButton)
            {
                if(Time.time < Jumping.LastStartTime + Jumping.ExtraHeight / CalculateJumpVerticalSpeed(Jumping.BaseHeight))
                {
                    Velocity += Jumping.Direction * Movement.Gravity * Time.deltaTime;
                }
            }
            Velocity.y = Mathf.Max(Velocity.y, -Movement.MaxFallSpeed);
        }

        if(Grounded)
        {

            if(Jumping.Enabled && CanControl && (Time.time - Jumping.LastButtonDownTime < 0.2))
            {
                Grounded = false;
                Jumping.Jumping = true;
                Jumping.LastStartTime = Time.time;
                Jumping.LastButtonDownTime = -100;
                Jumping.HoldingJumpButton = true;

                if(TooSteep())
                    Jumping.Direction = Vector3.Slerp(Vector3.up, GroundNormal, Jumping.SteepPerpAmount);
                else
                    Jumping.Direction = Vector3.Slerp(Vector3.up, GroundNormal, Jumping.PerpAmount);

                Velocity.y = 0;
                Velocity += Jumping.Direction * CalculateJumpVerticalSpeed(Jumping.BaseHeight);

                if(MovingPlatform.Enabled &&
                    (MovingPlatform.MovementTransfer == MovementTransferOnJump.InitTransfer ||
                    MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaTransfer)
                )
                {
                    Movement.FrameVelocity = MovingPlatform.PlatformVelocity;
                    Velocity += MovingPlatform.PlatformVelocity;
                }

                SendMessage("OnJump", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                Jumping.HoldingJumpButton = false;
            }
        }

        return Velocity;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(hit.normal.y > 0 && hit.normal.y > GroundNormal.y && hit.moveDirection.y < 0)
        {
            if((hit.point - Movement.LastHitPoint).sqrMagnitude > 0.001 || LastGroundNormal == Vector3.zero)
                GroundNormal = hit.normal;
            else
                GroundNormal = LastGroundNormal;

            MovingPlatform.HitPlatform = hit.collider.transform;
            Movement.HitPoint = hit.point;
            Movement.FrameVelocity = Vector3.zero;
        }
    }

    private IEnumerator SubtractNewPlatformVelocity()
    {
        if(MovingPlatform.Enabled &&
          (MovingPlatform.MovementTransfer == MovementTransferOnJump.InitTransfer ||
           MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaTransfer))
        {
            if(MovingPlatform.IsNewPlatform)
            {
                Transform platform = MovingPlatform.ActivePlatform;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                if(Grounded && platform == MovingPlatform.ActivePlatform)
                    yield break;
            }
            Movement.Velocity -= MovingPlatform.PlatformVelocity;
        }
    }

    private bool MoveWithPlatform()
    {
        return (MovingPlatform.Enabled
            && (Grounded || MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaLocked)
            && MovingPlatform.ActivePlatform != null
        );
    }

    private Vector3 GetDesiredHorizontalVelocity()
    {
        Vector3 desiredLocalDirection = transform.InverseTransformDirection(SetInputMoveDirection);
        float maxSpeed = MaxSpeedInDirection(desiredLocalDirection);
        if(Grounded)
        {
            var MovementSlopeAngle = Mathf.Asin(Movement.Velocity.normalized.y) * Mathf.Rad2Deg;
            maxSpeed *= Movement.SlopeSpeedMultiplier.Evaluate(MovementSlopeAngle);
        }
        return transform.TransformDirection(desiredLocalDirection * maxSpeed);
    }

    private Vector3 AdjustGroundVelocityToNormal(Vector3 hVelocity, Vector3 GroundNormal)
    {
        Vector3 sideways = Vector3.Cross(Vector3.up, hVelocity);
        return Vector3.Cross(sideways, GroundNormal).normalized * hVelocity.magnitude;
    }

    private bool IsGroundedTest()
    {
        return (GroundNormal.y > 0.01);
    }

    float GetMaxAcceleration(bool Grounded)
    {
        if(Grounded)
            return Movement.MaxGroundAcceleration;
        else
            return Movement.MaxAirAcceleration;
    }

    float CalculateJumpVerticalSpeed(float targetJumpHeight)
    {
        return Mathf.Sqrt(2 * targetJumpHeight * Movement.Gravity);
    }

    bool IsJumping()
    {
        return Jumping.Jumping;
    }

    bool IsSliding()
    {
        return (Grounded && Sliding.Enabled && TooSteep());
    }

    bool IsTouchingCeiling()
    {
        return (Movement.CollisionFlags & CollisionFlags.CollidedAbove) != 0;
    }

    bool IsGrounded()
    {
        return Grounded;
    }

    bool TooSteep()
    {
        return (GroundNormal.y <= Mathf.Cos(Controller.slopeLimit * Mathf.Deg2Rad));
    }

    Vector3 GetDirection()
    {
        return SetInputMoveDirection;
    }

    void SetControllable(bool controllable)
    {
        CanControl = controllable;
    }

    float MaxSpeedInDirection(Vector3 desiredMovementDirection)
    {
        if(desiredMovementDirection == Vector3.zero)
            return 0;
        else
        {
            float zAxisEllipseMultiplier = (desiredMovementDirection.z > 0 ? Movement.MaxForwardSpeed : Movement.MaxBackwardsSpeed) / Movement.MaxSidewaysSpeed;
            Vector3 temp = new Vector3(desiredMovementDirection.x, 0, desiredMovementDirection.z / zAxisEllipseMultiplier).normalized;
            float length = new Vector3(temp.x, 0, temp.z * zAxisEllipseMultiplier).magnitude * Movement.MaxSidewaysSpeed;
            return length;
        }
    }

    void SetVelocity(Vector3 velocity)
    {
        Grounded = false;
        Movement.Velocity = velocity;
        Movement.FrameVelocity = Vector3.zero;
        SendMessage("OnExternalVelocity");
    }

    //CLASES PARA MANEJAR EL PERSONAJE

    [System.Serializable]
    public class CharacterEngineSetup
    {
        public bool Enabled = false;
        public float SlidingSpeed = 15.0f;
        public float SidewaysControl = 1.0f;
        public float SpeedControl = 0.4f;
    }

    [System.Serializable]
    public class CharacterMovement
    {
        public float MaxForwardSpeed = 6.0f;
        public float MaxSidewaysSpeed = 6.0f;
        public float MaxBackwardsSpeed = 6.0f;
        public AnimationCurve SlopeSpeedMultiplier = new AnimationCurve(new Keyframe(-90, 1), new Keyframe(0, 1), new Keyframe(90, 0));
        public float MaxGroundAcceleration = 30.0f;
        public float MaxAirAcceleration = 20.0f;
        public float Gravity = 20.0f;
        public float MaxFallSpeed = 20.0f;
        public CollisionFlags CollisionFlags;
        public Vector3 Velocity;
        public Vector3 FrameVelocity = Vector3.zero;
        public Vector3 HitPoint = Vector3.zero;
        public Vector3 LastHitPoint = new Vector3(Mathf.Infinity, 0, 0);
    }


    [System.Serializable]
    public class CharacterEngineJumping
    {
        public bool Jumping = false;
        public bool HoldingJumpButton = false;

        public bool Enabled = true;
        public float BaseHeight = 1.0f;
        public float ExtraHeight = 4.1f;
        public float PerpAmount = 0.0f;
        public float SteepPerpAmount = 0.5f;

        public float LastStartTime = 0.0f;
        public float LastButtonDownTime = -100.0f;
        public Vector3 Direction = Vector3.up;
    }



    [System.Serializable]
    public class CharacterEngineMovingPlatform
    {
        public bool Enabled = true;
        public MovementTransferOnJump MovementTransfer = MovementTransferOnJump.None;
        public Transform HitPlatform;
        public Transform ActivePlatform;
        public Vector3 ActiveLocalPoint;
        public Vector3 ActiveGlobalPoint;
        public Quaternion ActiveLocalRotation;
        public Quaternion ActiveGlobalRotation;
        public Matrix4x4 LastMatrix;
        public Vector3 PlatformVelocity;
        public bool IsNewPlatform;
    }
}