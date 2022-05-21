using Sandbox;
using System.Collections.Generic;

namespace FuncVehicle;

[Title( "ViewModel" ), Icon( "pan_tool" )]
public class ViewModel : AnimatedEntity
{
	protected float SwingInfluence => 0.05f;
	protected float ReturnSpeed => 5.0f;
	protected float MaxOffsetLength => 10.0f;
	protected float BobCycleTime => 7;
	protected Vector3 BobDirection => new Vector3( 0.0f, 1.0f, 0.5f );

	private Vector3 swingOffset;
	private float lastPitch;
	private float lastYaw;
	private float bobAnim;

	private bool activated = false;

	public bool EnableSwingAndBob = true;

	public float YawInertia { get; private set; }
	public float PitchInertia { get; private set; }
	public static List<ViewModel> AllViewModels = new List<ViewModel>();

	public ViewModel()
	{
		AllViewModels.Add( this );
	}

	public override void Spawn()
	{
		base.Spawn();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		AllViewModels.Remove( this );
	}

	public override void OnNewModel( Model model )
	{
		base.OnNewModel( model );

		//
		// TODO - read FOV from model data?
		//

	}

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		Position = camSetup.Position;
		Rotation = camSetup.Rotation;

		if ( !Local.Pawn.IsValid() )
			return;

		if ( !activated )
		{
			lastPitch = camSetup.Rotation.Pitch();
			lastYaw = camSetup.Rotation.Yaw();

			YawInertia = 0;
			PitchInertia = 0;

			activated = true;
		}

		Position = camSetup.Position;
		Rotation = camSetup.Rotation;

		var cameraBoneIndex = GetBoneIndex( "camera" );
		if ( cameraBoneIndex != -1 )
		{
			camSetup.Rotation *= (Rotation.Inverse * GetBoneTransform( cameraBoneIndex ).Rotation);
		}

		var newPitch = Rotation.Pitch();
		var newYaw = Rotation.Yaw();

		PitchInertia = Angles.NormalizeAngle( newPitch - lastPitch );
		YawInertia = Angles.NormalizeAngle( lastYaw - newYaw );

		if ( EnableSwingAndBob )
		{
			var playerVelocity = Local.Pawn.Velocity;

			if ( Local.Pawn is Player player )
			{
				var controller = player.GetActiveController();
				if ( controller != null && controller.HasTag( "noclip" ) )
				{
					playerVelocity = Vector3.Zero;
				}
			}

			var verticalDelta = playerVelocity.z * Time.Delta;
			var viewDown = Rotation.FromPitch( newPitch ).Up * -1.0f;
			verticalDelta *= (1.0f - System.MathF.Abs( viewDown.Cross( Vector3.Down ).y ));
			var pitchDelta = PitchInertia - verticalDelta * 1;
			var yawDelta = YawInertia;

			var offset = CalcSwingOffset( pitchDelta, yawDelta );
			offset += CalcBobbingOffset( playerVelocity );

			Position += Rotation * offset;
		}
		else
		{
			SetAnimParameter( "aim_yaw_inertia", YawInertia );
			SetAnimParameter( "aim_pitch_inertia", PitchInertia );
		}

		lastPitch = newPitch;
		lastYaw = newYaw;
	}

	public static void UpdateAllPostCamera( ref CameraSetup camSetup )
	{
		foreach ( var vm in AllViewModels )
		{
			vm.PostCameraSetup( ref camSetup );
		}
	}

	public override Sound PlaySound( string soundName, string attachment )
	{
		if ( Owner.IsValid() )
			return Owner.PlaySound( soundName, attachment );

		return base.PlaySound( soundName, attachment );
	}

	protected Vector3 CalcSwingOffset( float pitchDelta, float yawDelta )
	{
		Vector3 swingVelocity = new Vector3( 0, yawDelta, pitchDelta );

		swingOffset -= swingOffset * ReturnSpeed * Time.Delta;
		swingOffset += (swingVelocity * SwingInfluence);

		if ( swingOffset.Length > MaxOffsetLength )
		{
			swingOffset = swingOffset.Normal * MaxOffsetLength;
		}

		return swingOffset;
	}

	protected Vector3 CalcBobbingOffset( Vector3 velocity )
	{
		bobAnim += Time.Delta * BobCycleTime;

		var twoPI = System.MathF.PI * 2.0f;

		if ( bobAnim > twoPI )
		{
			bobAnim -= twoPI;
		}

		var speed = new Vector2( velocity.x, velocity.y ).Length;
		speed = speed > 10.0 ? speed : 0.0f;
		var offset = BobDirection * (speed * 0.005f) * System.MathF.Cos( bobAnim );
		offset = offset.WithZ( -System.MathF.Abs( offset.z ) );

		return offset;
	}
}
