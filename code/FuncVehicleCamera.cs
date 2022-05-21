using Sandbox;

namespace FuncVehicle;

class FuncVehicleCamera : CameraMode
{
	public override void Activated()
	{
		var vehicle = (Local.Pawn as FuncVehicle);
		var pawn = (Local.Pawn as FuncVehicle)?.Driver;
		if ( vehicle == null ) return;
		if ( pawn == null ) return;

		Position = pawn.EyePosition;
		var noRoll = vehicle.Rotation.Angles().WithRoll( 0.0f ).ToRotation();
		Rotation = noRoll * pawn.LocalRotation.Inverse * pawn.EyeRotation;
	}

	public override void Update()
	{
		var vehicle = (Local.Pawn as FuncVehicle);
		var pawn = (Local.Pawn as FuncVehicle)?.Driver;
		if ( vehicle == null ) return;
		if ( pawn == null ) return;

		Position = pawn.EyePosition;
		var noRoll = vehicle.Rotation.Angles().WithRoll( 0.0f ).ToRotation();
		Rotation = noRoll * pawn.LocalRotation.Inverse * pawn.EyeRotation;

		Viewer = pawn;
	}
}
