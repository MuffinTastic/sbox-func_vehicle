
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace FuncVehicle;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class FuncVehicleGame : Sandbox.Game
{
	public FuncVehicleGame()
	{
		if ( IsServer )
		{
			// Create the HUD
			_ = new FuncVehicleHud();

			BaseTrigger.ToggleDrawTriggers();
		}
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( Client client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new FuncVehiclePlayer();
		client.Pawn = pawn;
		pawn.Respawn();

		ForceRespawn( pawn );
	}

	[ConCmd.Server]
	public static void ForceRespawn( Entity pawn = null )
	{
		pawn ??= ConsoleSystem.Caller?.Pawn;

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
			pawn.Position = tx.Position;
			pawn.Velocity = 0;
		}
	}

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );
		ViewModel.UpdateAllPostCamera( ref camSetup );
	}

	[ConCmd.Server("spawn_weapon")]
	public static void SpawnWeapon( string className )
	{
		var pawn = ConsoleSystem.Caller.Pawn as FuncVehiclePlayer;
		if ( pawn is null )
			return;

		var weapon = TypeLibrary.Create<Weapon>( className );
		if ( weapon is null )
			return;

		pawn.Inventory.Add( weapon );
	}

	/// <summary>
	/// Player typed noclip in the console.
	/// </summary>
	public override void DoPlayerNoclip( Client player )
	{
		if ( !player.HasPermission( "noclip" ) )
			return;

		if ( player.Pawn is FuncVehiclePlayer basePlayer )
		{
			if ( basePlayer.DevController is NoclipController )
			{
				Log.Info( "Noclip Mode Off" );
				basePlayer.DevController = null;
			}
			else
			{
				Log.Info( "Noclip Mode On" );
				basePlayer.DevController = new NoclipController();
			}
		}
	}
}

