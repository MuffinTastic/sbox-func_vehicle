using Sandbox;

namespace FuncVehicle;

partial class BasePlayer
{
	public Entity Using { get; protected set; }

	/// <summary>
	/// This should be called somewhere in your player's tick to allow them to use entities
	/// </summary>
	protected virtual void TickPlayerUse()
	{
		// This is serverside only
		if ( !Host.IsServer ) return;

		// Turn prediction off
		using ( Prediction.Off() )
		{
			if ( Input.Pressed( InputButton.Use ) )
			{
				Using = FindUsable();

				if ( Using == null )
				{
					UseFail();
					return;
				}
			}

			if ( !Input.Down( InputButton.Use ) )
			{
				StopUsing();
				return;
			}

			if ( !Using.IsValid() )
				return;

			// If we move too far away or something we should probably ClearUse()?

			//
			// If use returns true then we can keep using it
			//
			if ( Using is IUse use && use.OnUse( this ) )
				return;

			StopUsing();
		}
	}

	/// <summary>
	/// If we're using an entity, stop using it
	/// </summary>
	protected virtual void StopUsing()
	{
		Using = null;
	}

	/// <summary>
	/// Returns if the entity is a valid usaable entity
	/// </summary>
	protected bool IsValidUseEntity( Entity e )
	{
		if ( e == null ) return false;
		if ( e is not IUse use ) return false;
		if ( !use.IsUsable( this ) ) return false;

		return true;
	}

	public bool IsUseDisabled()
	{
		return ActiveChild is IUse use && use.IsUsable( this );
	}

	protected Entity FindUsable()
	{
		if ( IsUseDisabled() )
			return null;

		// First try a direct 0 width line
		var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * (85 * Scale) )
			.HitLayer( CollisionLayer.Debris )
			.Ignore( this )
			.Run();

		// See if any of the parent entities are usable if we ain't.
		var ent = tr.Entity;
		while ( ent.IsValid() && !IsValidUseEntity( ent ) )
		{
			ent = ent.Parent;
		}

		// Nothing found, try a wider search
		if ( !IsValidUseEntity( ent ) )
		{
			tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * (85 * Scale) )
			.Radius( 2 )
			.HitLayer( CollisionLayer.Debris )
			.Ignore( this )
			.Run();

			// See if any of the parent entities are usable if we ain't.
			ent = tr.Entity;
			while ( ent.IsValid() && !IsValidUseEntity( ent ) )
			{
				ent = ent.Parent;
			}
		}

		// Still no good? Bail.
		if ( !IsValidUseEntity( ent ) ) return null;

		return ent;
	}


	/// <summary>
	/// Player tried to use something but there was nothing there.
	/// Tradition is to give a dissapointed boop.
	/// </summary>
	protected void UseFail()
	{
		if ( IsUseDisabled() )
			return;

		PlaySound( "player_use_fail" );
	}
}
