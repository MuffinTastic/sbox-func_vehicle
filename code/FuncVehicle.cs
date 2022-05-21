using System;
using System.Linq;
using Sandbox;
using SandboxEditor;

namespace FuncVehicle;

/// <summary>
/// FUNC_VEHICLE FUNC_VEHICLE FUNC_VEHICLE FUNC_VEHICLE FUNC_VEHICLE FUNC_VEHICLE FUNC_VEHICLE
/// </summary>
[Library( "func_vehicle" )]
[HammerEntity, Solid]
[RenderFields]
public partial class FuncVehicle : AnimatedEntity, IUse
{
	public const float VEHICLE_SPEED0_ACCELERATION  = 0.005000000000000000f;
	public const float VEHICLE_SPEED1_ACCELERATION  = 0.002142857142857143f;
	public const float VEHICLE_SPEED2_ACCELERATION  = 0.003333333333333334f;
	public const float VEHICLE_SPEED3_ACCELERATION  = 0.004166666666666667f;
	public const float VEHICLE_SPEED4_ACCELERATION  = 0.004000000000000000f;
	public const float VEHICLE_SPEED5_ACCELERATION  = 0.003800000000000000f;
	public const float VEHICLE_SPEED6_ACCELERATION  = 0.004500000000000000f;
	public const float VEHICLE_SPEED7_ACCELERATION  = 0.004250000000000000f;
	public const float VEHICLE_SPEED8_ACCELERATION  = 0.002666666666666667f;
	public const float VEHICLE_SPEED9_ACCELERATION  = 0.002285714285714286f;
	public const float VEHICLE_SPEED10_ACCELERATION = 0.001875000000000000f;
	public const float VEHICLE_SPEED11_ACCELERATION = 0.001444444444444444f;
	public const float VEHICLE_SPEED12_ACCELERATION = 0.001200000000000000f;
	public const float VEHICLE_SPEED13_ACCELERATION = 0.000916666666666666f;

	public const float VEHICLE_STARTPITCH = 60.0f;
	public const float VEHICLE_MAXPITCH   = 200.0f;
	public const float VEHICLE_MAXSPEED   = 1500.0f;

	[Net] public FuncVehiclePlayer Driver { get; private set; }

	/// <summary>
	/// Amount of damage this entity can take before breaking
	/// </summary>
	[Property( "health", Title = "Health" )]
	protected float _health { get; set; }

	[Property( "vehicleControlsName", Title = "Vehicle Controls Name" )]
	protected string ControlsEntityName { get; set; }

	[Property( "distanceFromGround", Title = "Distance From Ground" )]
	protected float DistanceFromGround { get; set; }

	[Property( "speed", Title = "Vehicle Speed" )]
	protected float VehicleSpeed { get; set; }

	[Property( "acceleration", Title = "Vehicle Acceleration")]
	protected float VehicleAcceleration { get; set; }

	[Property( "engineSoundFile", Title = "Engine Sound" )]
	protected string EngineSoundFile { get; set; }

	[Property( "engineVolume", Title = "Engine Volume" )]
	protected float EngineVolume { get; set; }

	FuncVehicleControls ControlsEntity;
	Vector3 SpawnPosition;
	Rotation SpawnRotation;

	Sound? EngineSound;
	TimeSince LastSoundUpdate;

	Vector3 FrontLeft;
	Vector3 FrontRight;
	Vector3 Front;
	Vector3 BackLeft;
	Vector3 BackRight;
	Vector3 Back;
	Vector3 SurfaceNormal;
	float LastNormalZ;

	int TurnAngle;
	TimeSince SteeringWheelDecay;

	float Speed;
	TimeSince AcceleratorDecay;

	Vector3 VehicleDirection;
	Angles TargetAngle;

	float LaunchTime = 0.0f;
	float TurnStartTime;

	Vector3 GravityVector;

	TimeSince CanTurnNow;

	public override void Spawn()
	{
		base.Spawn();

		if ( _health > 0 ) Health = _health;

		CreatePhysics();

		CameraMode = new FuncVehicleCamera();
		SpawnPosition = Position;
		SpawnRotation = Rotation;

		LinkControls();
	}

	[Input]
	public void Respawn()
	{
		Position = SpawnPosition;
		Rotation = SpawnRotation;
		Velocity = Vector3.Zero;
	}

	[Input]
	public void KickOutDriver()
	{
		if ( Driver is not null )
			RemoveDriver( Driver );
	}

	private async void LinkControls()
	{
		await Task.DelaySeconds( 0.5f ); // give entities time to spawn

		ControlsEntity = Entity.All.OfType<FuncVehicleControls>()
			.Where( e => e.Name == ControlsEntityName )
			.FirstOrDefault();

		Log.Info( ControlsEntity );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		CreatePhysics();
	}

	void CreatePhysics()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}

	public override void Simulate( Client client )
	{
		SimulateDriver( client );

		if ( !IsServer ) return;

		UpdateInput();

	}

	[Event.Tick.Server]
	public void Tick()
	{
		DoPhysics();

		if ( Driver is FuncVehiclePlayer player && player.LifeState != LifeState.Alive )
		{
			RemoveDriver( player );
		}

		return;

		DebugOverlay.Sphere( Position, 6.0f, Color.Orange );
		DebugOverlay.Sphere( FrontLeft, 6.0f, Color.Blue );
		DebugOverlay.Sphere( FrontRight, 6.0f, Color.Cyan );
		DebugOverlay.Sphere( Front, 6.0f, Color.Magenta );
		DebugOverlay.Sphere( BackLeft, 6.0f, Color.Yellow );
		DebugOverlay.Sphere( BackRight, 6.0f, Color.White );
		DebugOverlay.Sphere( Back, 6.0f, Color.Green );
		
		int line = 0;
		DebugOverlay.ScreenText( $"          Position: {Position}", line++ );
		DebugOverlay.ScreenText( $"          Velocity: {Velocity}", line++ );
		DebugOverlay.ScreenText( $"   AngularVelocity: {AngularVelocity}", line++ );
		DebugOverlay.ScreenText( $"          Rotation: {Rotation.Angles()}", line++ );
		DebugOverlay.ScreenText( $"       EngineSound: {EngineSound}", line++ );
		DebugOverlay.ScreenText( $"   LastSoundUpdate: {LastSoundUpdate}", line++ );
		DebugOverlay.ScreenText( $"         FrontLeft: {FrontLeft}", line++ );
		DebugOverlay.ScreenText( $"        FrontRight: {FrontRight}", line++ );
		DebugOverlay.ScreenText( $"             Front: {Front}", line++ );
		DebugOverlay.ScreenText( $"          BackLeft: {BackLeft}", line++ );
		DebugOverlay.ScreenText( $"         BackRight: {BackRight}", line++ );
		DebugOverlay.ScreenText( $"              Back: {Back}", line++ );
		DebugOverlay.ScreenText( $"     SurfaceNormal: {SurfaceNormal}", line++ );
		DebugOverlay.ScreenText( $"       LastNormalZ: {LastNormalZ}", line++ );
		DebugOverlay.ScreenText( $"         TurnAngle: {TurnAngle}", line++ );
		DebugOverlay.ScreenText( $"SteeringWheelDecay: {SteeringWheelDecay}", line++ );
		DebugOverlay.ScreenText( $"             Speed: {Speed}", line++ );
		DebugOverlay.ScreenText( $"  AcceleratorDecay: {AcceleratorDecay}", line++ );
		DebugOverlay.ScreenText( $"  VehicleDirection: {VehicleDirection}", line++ );
		DebugOverlay.ScreenText( $"       TargetAngle: {TargetAngle}", line++ );
		DebugOverlay.ScreenText( $"        LaunchTime: {LaunchTime}", line++ );
		DebugOverlay.ScreenText( $"     TurnStartTime: {TurnStartTime}", line++ );
		DebugOverlay.ScreenText( $"     GravityVector: {GravityVector}", line++ );
		DebugOverlay.ScreenText( $"        CanTurnNow: {CanTurnNow}", line++ );

	}

	private void UpdateInput()
	{
		float speedRatio = Speed / VehicleSpeed;

		if ( Input.Forward != 0.0f )
		{
			if ( Input.Forward > 0.0f )
			{
				if		( Speed < 0.0f )	speedRatio = VehicleAcceleration * 0.0005f + speedRatio + VEHICLE_SPEED0_ACCELERATION;
				else if ( Speed < 10.0f )	speedRatio = VehicleAcceleration * 0.0006f + speedRatio + VEHICLE_SPEED1_ACCELERATION;
				else if ( Speed < 20.0f )	speedRatio = VehicleAcceleration * 0.0007f + speedRatio + VEHICLE_SPEED2_ACCELERATION;
				else if ( Speed < 30.0f )	speedRatio = VehicleAcceleration * 0.0007f + speedRatio + VEHICLE_SPEED3_ACCELERATION;
				else if ( Speed < 45.0f )	speedRatio = VehicleAcceleration * 0.0007f + speedRatio + VEHICLE_SPEED4_ACCELERATION;
				else if ( Speed < 60.0f )	speedRatio = VehicleAcceleration * 0.0008f + speedRatio + VEHICLE_SPEED5_ACCELERATION;
				else if ( Speed < 80.0f )	speedRatio = VehicleAcceleration * 0.0008f + speedRatio + VEHICLE_SPEED6_ACCELERATION;
				else if ( Speed < 100.0f )	speedRatio = VehicleAcceleration * 0.0009f + speedRatio + VEHICLE_SPEED7_ACCELERATION;
				else if ( Speed < 150.0f )	speedRatio = VehicleAcceleration * 0.0008f + speedRatio + VEHICLE_SPEED8_ACCELERATION;
				else if ( Speed < 225.0f )	speedRatio = VehicleAcceleration * 0.0007f + speedRatio + VEHICLE_SPEED9_ACCELERATION;
				else if ( Speed < 300.0f )	speedRatio = VehicleAcceleration * 0.0006f + speedRatio + VEHICLE_SPEED10_ACCELERATION;
				else if ( Speed < 400.0f )	speedRatio = VehicleAcceleration * 0.0005f + speedRatio + VEHICLE_SPEED11_ACCELERATION;
				else if ( Speed < 550.0f )	speedRatio = VehicleAcceleration * 0.0005f + speedRatio + VEHICLE_SPEED12_ACCELERATION;
				else if ( Speed < 800.0f )	speedRatio = VehicleAcceleration * 0.0005f + speedRatio + VEHICLE_SPEED13_ACCELERATION;
			}
			else if ( Input.Forward < 0.0f )
			{
				if		( speedRatio > 0.0f )							speedRatio = speedRatio - 0.0125f;
				else if ( speedRatio <= 0.0f && speedRatio > -0.05f )	speedRatio = speedRatio - 0.0075f;
				else if ( speedRatio <= 0.05f && speedRatio > -0.1f )	speedRatio = speedRatio - 0.01f;
				else if ( speedRatio <= 0.15f && speedRatio > -0.15f )	speedRatio = speedRatio - 0.0125f;
				else if ( speedRatio <= 0.15f && speedRatio > -0.22f )	speedRatio = speedRatio - 0.01375f;
				else if ( speedRatio <= 0.22f && speedRatio > -0.3f )	speedRatio = speedRatio - 0.0175f;
				else if ( speedRatio <= 0.3f )							speedRatio = speedRatio - 0.0125f;
			}

			if ( speedRatio > 1.0f )
			{
				speedRatio = 1.0f;
			}
			else if ( speedRatio < -0.35f )
			{
				speedRatio = -0.35f;
			}

			Speed = speedRatio * VehicleSpeed;
			AcceleratorDecay = 0.0f;
		}

		if ( CanTurnNow > 0.05f )
		{
			if ( Input.Left < 0.0f )
			{
				TurnAngle++;
				SteeringWheelDecay = 0;

				if ( TurnAngle > 8 )
				{
					TurnAngle = 8;
				}
			}
			else if ( Input.Left > 0.0f )
			{
				TurnAngle--;
				SteeringWheelDecay = 0;

				if ( TurnAngle < -8 )
				{
					TurnAngle = -8;
				}
			}
			 
			CanTurnNow = 0.0f;
		}
	}

	private void DoPhysics()
	{
		var forward = Rotation.Forward * Model.Bounds.Size.x * 0.5f;
		var right = Rotation.Right * Model.Bounds.Size.y * 0.5f;
		var up = Rotation.Up * 16.0f;

		FrontLeft = Position + forward - right + up;
		FrontRight = Position + forward + right + up;
		Front = Position + forward + up;
		BackLeft = Position - forward - right + up;
		BackRight = Position - forward + right + up;
		Back = Position - forward + up;
		SurfaceNormal = Vector3.Zero;

		CheckTurning();

		if ( SteeringWheelDecay >= 0.1f )
		{
			if ( TurnAngle < 0 )
				TurnAngle++;

			else if ( TurnAngle > 0 )
				TurnAngle--;

			SteeringWheelDecay = 0.0f;
		}

		if ( AcceleratorDecay >= 0.1f )
		{
			if ( Speed < 0.0f )
			{
				Speed += 20.0f;

				if ( Speed > 0.0f )
					Speed = 0.0f;
			}
			else if ( Speed > 0.0f )
			{
				Speed -= 20.0f;

				if ( Speed < 0.0f )
					Speed = 0.0f;
			}

			AcceleratorDecay = 0.0f;
		}

		if ( Speed == 0 )
		{
			TurnAngle = 0;
			AngularVelocity = Angles.Zero;
			Velocity = Vector3.Zero;
			return;
		}

		FollowTerrain();
		CollisionDetection();

		if ( !SurfaceNormal.AlmostEqual( Vector3.Zero ) )
		{
			VehicleDirection = SurfaceNormal.Cross( Rotation.Forward );
			VehicleDirection = SurfaceNormal.Cross( VehicleDirection );

			var vehicleRoll = SurfaceNormal.Cross( Rotation.Left );
			vehicleRoll = SurfaceNormal.Cross( vehicleRoll );

			var vdAngles = VehicleDirection.EulerAngles;
			vdAngles.pitch *= -1.0f;
			vdAngles.yaw += 180.0f;

			TargetAngle = vdAngles.ToRotation().Angles();
			if ( TurnAngle != 0 )
			{
				TargetAngle.yaw -= TurnAngle;
			}

			var angle = Rotation.Angles();

			float vx = TargetAngle.pitch - angle.pitch;
			float vy = TargetAngle.yaw - angle.yaw;
			var rot = -vehicleRoll.EulerAngles.pitch;
			if ( rot < -180.0f ) rot += 360.0f;
			float vz = -rot - angle.roll;

			// DebugOverlay.ScreenText( $"{Rotation.Left}", -3 );
			// DebugOverlay.ScreenText( $"{vehicleRoll.EulerAngles}", -2 );
			// DebugOverlay.ScreenText( $"{vz}", -1 );

			if ( vx > 10 )
				vx = 10;
			else if ( vx < -10 )
				vx = -10;

			if ( vy > 10 )
				vy = 10;
			else if ( vy < -10 )
				vy = -10;

			if ( vz > 10 )
				vz = 10;
			else if ( vz < -10 )
				vz = -10;

			AngularVelocity = new Angles( vx * 10.0f, vy * 10.0f, vz * 10.0f );

			LaunchTime = -1.0f;
			LastNormalZ = SurfaceNormal.z;
		}
		else
		{
			//if ( LaunchTime != -1.0f )
			//{
				GravityVector = Vector3.Zero.WithZ( (Time.Now - LaunchTime) * -35.0f );

				if ( GravityVector.z < -400 )
				{
					GravityVector.z = -400;
				}
			//}
			//else
			//{
			//	LaunchTime = Time.Now;
			//	GravityVector = Vector3.Zero;
			//	Velocity = Velocity * 1.5f;
			//}

		}
		
		VehicleDirection = Rotation.Forward;

		if ( LastSoundUpdate >= 1.0f )
		{
			UpdateSound();

			LastSoundUpdate = 0.0f;
		}

		if ( SurfaceNormal != Vector3.Zero )
		{
			Velocity = VehicleDirection.Normal * Speed;
		}
		else
		{
			Velocity = Velocity + GravityVector * Time.Delta;
		}

		if ( Velocity.LengthSquared > 0.01f )
		{
			Position += Velocity * Time.Delta;
		}

		Rotation *= (AngularVelocity * Time.Delta).ToRotation();
	}

	private void CheckTurning()
	{
		float maxspeed;
		TraceResult? tr = null;
		var turnIntoWall = false;

		if ( TurnAngle > 0 )
		{
			if ( Speed > 0 )
			{
				tr = Trace.Ray( new Ray( FrontRight, Rotation.Right ), 16.0f ).WorldOnly().Run();
			}
			else if ( Speed < 0 )
			{
				tr = Trace.Ray( new Ray( BackLeft, -Rotation.Right ), 16.0f ).WorldOnly().Run();
			}

			if ( tr?.Fraction != 1.0f )
			{
				TurnAngle = 1;
			}
		}
		else if ( TurnAngle < 0 )
		{
			if ( Speed > 0 )
			{
				tr = Trace.Ray( new Ray( FrontLeft, Rotation.Left ), 16.0f ).WorldOnly().Run();
			}
			else if ( Speed < 0 )
			{
				tr = Trace.Ray( new Ray( BackRight, -Rotation.Left ), 16.0f ).WorldOnly().Run();
			}

			if ( tr?.Fraction != 1.0f )
			{
				TurnAngle = -1;
			}
		}

		if ( Speed > 0 )
		{
			int countTurn = Math.Abs( TurnAngle );

			if ( countTurn > 4 )
			{
				if ( TurnStartTime != -1.0f )
				{
					float turnTime = Time.Now - TurnStartTime;

					if ( turnTime >= 0.0f ) maxspeed = VehicleSpeed * 0.98f;
					else if ( turnTime > 0.3f ) maxspeed = VehicleSpeed * 0.95f;
					else if ( turnTime > 0.6f ) maxspeed = VehicleSpeed * 0.9f;
					else if ( turnTime > 0.8f ) maxspeed = VehicleSpeed * 0.8f;
					else if ( turnTime > 1.0f ) maxspeed = VehicleSpeed * 0.7f;
					else if ( turnTime > 1.2f ) maxspeed = VehicleSpeed * 0.5f;
					else maxspeed = turnTime;
				}
				else
				{
					TurnStartTime = Time.Now;
					maxspeed = VehicleSpeed;
				}
			}
			else
			{
				TurnStartTime = -1.0f;

				if ( countTurn > 2 )
				{
					maxspeed = VehicleSpeed * 0.9f;
				}
				else
				{
					maxspeed = VehicleSpeed;
				}
			}

			if ( maxspeed < Speed )
			{
				Speed -= VehicleSpeed * 0.1f;
			}
		}
	}

	private void FollowTerrain()
	{
		var tr = Trace.Ray( new Ray( Position, Vector3.Down ), DistanceFromGround + 4.0f )
			.WorldOnly()
			.Ignore( this )
			.Run();

		if ( tr.Fraction < 1.0f )
		{
			SurfaceNormal = tr.Normal;
		}
		//else if ( Map.Physics.IsPointWater( tr.EndPosition ) )
		//{
		//	SurfaceNormal = Vector3.Up;
		//}
	}

	private void CollisionDetection()
	{
		TraceResult tr = Trace.Ray(0, 0).WithTag("").Run();
		var hitSomething = false;

		if ( Speed < 0 )
		{
			tr = Trace.Ray( new Ray( BackLeft, Rotation.Backward ), 16.0f ).WorldOnly().Run();

			if ( tr.Fraction == 1.0f )
			{
				tr = Trace.Ray( new Ray( BackRight, Rotation.Backward ), 16.0f ).WorldOnly().Run();

				if ( tr.Fraction == 1.0f )
				{
					tr = Trace.Ray( new Ray( Back, Rotation.Backward ), 16.0f ).WorldOnly().Run();

					if ( tr.Fraction == 1.0f )
					{
						return;
					}
				}

				if ( Rotation.Backward.Dot( tr.Normal ) > 0.7f && tr.Normal.z < 0.1f )
				{
					SurfaceNormal = tr.Normal;
					SurfaceNormal.z = 0.0f;

					Speed *= 0.99f;
				}
				else if ( tr.Normal.z < 0.65f || tr.StartedSolid )
				{
					Speed *= -1.0f;
				}
				else
				{
					SurfaceNormal = tr.Normal;
				}
			}
			else
			{
				if ( Rotation.Backward.Dot( tr.Normal ) > 0.7f && tr.Normal.z < 0.1f )
				{
					SurfaceNormal = tr.Normal;
					SurfaceNormal.z = 0.0f;

					Speed *= 0.99f;
				}
				else if ( tr.Normal.z < 0.65f || tr.StartedSolid )
				{
					Speed *= -1.0f;
				}
				else
				{
					SurfaceNormal = tr.Normal;
				}

				if ( tr.Entity is FuncVehicle vehicle )
				{
					hitSomething = true;
					Log.Info( "I hit another vehicle! " );
				}
			}
		}
		else if ( Speed > 0 )
		{
			tr = Trace.Ray( new Ray( FrontLeft, Rotation.Forward ), 16.0f ).WorldOnly().Run();

			if ( tr.Fraction == 1.0f )
			{
				tr = Trace.Ray( new Ray( FrontRight, Rotation.Forward ), 16.0f ).WorldOnly().Run();

				if ( tr.Fraction == 1.0f )
				{
					tr = Trace.Ray( new Ray( Front, Rotation.Forward ), 16.0f ).WorldOnly().Run();

					if ( tr.Fraction == 1.0f )
					{
						return;
					}
				}
			}

			if ( Rotation.Forward.Dot( tr.Normal ) > 0.7f && tr.Normal.z < 0.1f )
			{
				SurfaceNormal = tr.Normal;
				SurfaceNormal.z = 0.0f;

				Speed *= 0.99f;
			}
			else if ( tr.Normal.z < 0.65f || tr.StartedSolid )
			{
				Speed *= -1.0f;
			}
			else
			{
				SurfaceNormal = tr.Normal;
			}
		}


		// DebugOverlay.Sphere( tr.StartPosition, 4.0f, Color.Red );
		// DebugOverlay.TraceResult( tr );
	}

	private void UpdateSound()
	{
		EngineSound?.SetPitch( 1.0f + Velocity.Length / 1500.0f );
	}

	void SimulateDriver( Client client )
	{
		if ( !Driver.IsValid() ) return;

		if ( IsServer && Input.Pressed( InputButton.Use ) )
		{
			RemoveDriver( Driver );
			return;
		}

		Driver.ActiveChild?.Simulate( client );

		Driver.SetAnimParameter( "b_grounded", true );
		Driver.SetAnimParameter( "b_sit", true );

		var aimRotation = Input.Rotation.Clamp( Driver.Rotation, 90 );

		var aimPos = Driver.EyePosition + aimRotation.Forward * 200;
		var localPos = new Transform( Driver.EyePosition, Driver.Rotation ).PointToLocal( aimPos );

		Driver.SetAnimParameter( "aim_eyes", localPos );
		Driver.SetAnimParameter( "aim_head", localPos );
		Driver.SetAnimParameter( "aim_body", localPos );

		if ( Driver.ActiveChild is Carriable carry )
		{
			// carry.SimulateAnimator( null );
		}
		else
		{
			Driver.SetAnimParameter( "holdtype", 0 );
			Driver.SetAnimParameter( "aim_body_weight", 0.5f );
		}
	}

	public override void FrameSimulate( Client client )
	{
		base.FrameSimulate( client );

		Driver?.FrameSimulate( client );
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( Driver is FuncVehiclePlayer player )
		{
			RemoveDriver( player );
		}
	}

	public bool IsUsable( Entity user )
	{
		if ( !ControlsEntity.TouchingEntities.Contains( user ) )
			return false;

		return Driver is null;
	}

	public bool OnUse( Entity user )
	{
		if ( user is FuncVehiclePlayer player )
		{
			player.Parent = this;
			player.LocalPosition = (player.Position - Position) * Rotation.Inverse;
			player.LocalRotation = Rotation;
			player.LocalScale = 1;
			player.PhysicsBody.Enabled = false;

			Driver = player;

			player.Client.Pawn = this;

			PlaySound( "vehicle_start1" );

			var startEngineSound = async () =>
			{
				await Task.DelaySeconds( 0.15f );

				if ( Driver is null ) // if they get out before the delay time
					return;

				EngineSound?.Stop();
				EngineSound = PlaySound( EngineSoundFile );
			};

			startEngineSound();
		}

		return false;
	}

	private void RemoveDriver( FuncVehiclePlayer player )
	{
		Driver = null;

		//ResetInput();

		if ( !player.IsValid() )
			return;

		player.Parent = null;

		if ( player.PhysicsBody.IsValid() )
		{
			player.PhysicsBody.Enabled = true;
			player.PhysicsBody.Position = player.Position;
			player.Rotation = Rotation.Identity; //player.Rotation * Rotation;
		}

		player.Client.Pawn = player;

		EngineSound?.Stop();
		EngineSound = null;
	}

	/// <summary>
	/// Provides an easy way to switch our current cameramode component
	/// </summary>
	public CameraMode CameraMode
	{
		get => Components.Get<CameraMode>();
		set => Components.Add( value );
	}
}
