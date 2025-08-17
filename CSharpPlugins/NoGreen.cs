using System;
using CompanionServer;
using ConVar;
using Facepunch;
using Facepunch.Math;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("No Green", "Iv Misticos", "1.3.10")]
    [Description("Remove admins' green names")]
    class NoGreen : RustPlugin
    {
	    private bool OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
	    {
		    if (Chat.serverlog)
		    {
			    ServerConsole.PrintColoured(ConsoleColor.DarkYellow,
				    string.Concat("[", channel, "] ", player.displayName, ": "), ConsoleColor.DarkGreen, message);
			    
			    var str = (player != null ? player.ToString() : null) ??
			              $"{player.displayName}[{player.userID}]";
			    
			    if (channel == Chat.ChatChannel.Team)
			    {
				    DebugEx.Log("[TEAM CHAT] " + str + " : " + message);
			    }
			    else
			    {
				    DebugEx.Log("[CHAT] " + str + " : " + message);
			    }
		    }

		    var color = "#5af";
		    var displayName = player.displayName.EscapeRichText();
		    var chatEntry = new Chat.ChatEntry
		    {
			    Channel = channel,
			    Message = message,
			    UserId = player.UserIDString,
			    Username = displayName,
			    Color = color,
			    Time = Epoch.Current
		    };

		    RCon.Broadcast(RCon.LogType.Chat, chatEntry);
		    if (channel != Chat.ChatChannel.Global)
		    {
			    if (channel == Chat.ChatChannel.Team)
			    {
				    var playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(player.userID);
				    if (playerTeam == null)
				    {
					    return false;
				    }

				    var onlineMemberConnections = playerTeam.GetOnlineMemberConnections();
				    if (onlineMemberConnections != null)
				    {
					    ConsoleNetwork.SendClientCommand(onlineMemberConnections, "chat.add2", 1, player.userID,
						    message, displayName, color, 1f);
				    }

				    playerTeam.BroadcastTeamChat(player.userID, displayName, message, color);
				    return true;
			    }
		    }
		    else if (Chat.globalchat)
		    {
			    ConsoleNetwork.BroadcastToAllClients("chat.add2", 0, player.userID, message, displayName, color, 1f);
			    return true;
		    }

		    if (player == null)
			    return true;
		    
		    var radius = 2500f;
		    foreach (var basePlayer in BasePlayer.activePlayerList)
		    {
			    var sqrMagnitude = (basePlayer.transform.position - player.transform.position).sqrMagnitude;
			    if (sqrMagnitude <= radius)
			    {
				    ConsoleNetwork.SendClientCommand(basePlayer.net.connection, "chat.add2", 0, player.userID,
					    message,
					    displayName, color, Mathf.Clamp01(radius - sqrMagnitude + 0.2f));
			    }
		    }

		    return true;

	    }
    }
}