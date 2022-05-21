using Sandbox;
using Sandbox.UI;

namespace FuncVehicle;

public partial class FuncVehicleHud : HudEntity<RootPanel>
{
	public FuncVehicleHud()
	{
		if ( !IsClient )
			return;

		RootPanel.StyleSheet.Load( $"/ui/{ClassName}.scss" );

		RootPanel.AddChild<Label>( out var label, "title" );
		label.Text = "FUNC_VEHICLE IS A RIGHT NOT A PRIVILEGE";
		RootPanel.AddChild<ChatBox>();
		RootPanel.AddChild<VoiceList>();
		RootPanel.AddChild<KillFeed>();
		RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();
		RootPanel.AddChild<Health>();
		RootPanel.AddChild<InventoryBar>();
		RootPanel.AddChild<Crosshair>();
	}
}
