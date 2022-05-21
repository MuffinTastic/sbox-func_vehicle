using Sandbox;
using SandboxEditor;
using System.Linq;

namespace FuncVehicle;

[Library( "func_vehiclecontrols" )]
[HammerEntity, Solid]
public class FuncVehicleControls : BaseTrigger
{
	public override void Spawn()
	{
		base.Spawn();

		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		SetInteractsAs( CollisionLayer.Debris );
		CollisionGroup = CollisionGroup.Default;
		Tags.Add( "vehiclecontrols" );
	}
}
