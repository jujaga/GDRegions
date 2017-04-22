using System;
using System.IO;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace GDRegions
{
    [ApiVersion(2, 1)]
    public class GDRegions : TerrariaPlugin
    {
        #region PluginInfo
        public override string Name { get { return "GDRegions"; } }
        public override string Author { get { return "Zaicon"; } }
        public override string Description { get { return "Region management using The Grand Design."; } }
        public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        public GDRegions(Main game)
            : base(game)
        {
            base.Order = 1;
        }
        #endregion

        #region Init/Dispose
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
            ServerApi.Hooks.NetGreetPlayer.Register(this, onGreet);
            ServerApi.Hooks.NetGetData.Register(this, onGetData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, onInitialize);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, onGreet);
                ServerApi.Hooks.NetGetData.Deregister(this, onGetData);
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Hooks
        private void onInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("tshock.admin.region", GDRegion, "setregion"));
        }

        private void onGreet(GreetPlayerEventArgs args)
        {
            var plr = TShock.Players[args.Who];

            if (plr == null)
                return;

            plr.SetData<bool>("awaitGD", false);
        }

        private void onGetData(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.MassWireOperation)
                return;
#if DEBUG
            TSPlayer.Server.SendInfoMessage("MassWireOperation Received");
#endif
            var plr = TShock.Players[args.Msg.whoAmI];

            if (plr == null)
                return;
#if DEBUG
            TSPlayer.Server.SendInfoMessage("Valid Player Found");
#endif
            if (!plr.GetData<bool>("awaitGD"))
                return;
#if DEBUG
            TSPlayer.Server.SendInfoMessage("Awaiting GD Found");
#endif
            string rname = plr.GetData<string>("GDname");

            short startX, startY, endX, endY;

            using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
            {
                startX = reader.ReadInt16();
                startY = reader.ReadInt16();
                endX = reader.ReadInt16();
                endY = reader.ReadInt16();
            }

            if (endX < startX)
            {
                var oldEndx = endX;
                endX = startX;
                startX = oldEndx;
            }

            if (endY < startY)
            {
                var oldEndy = endY;
                endY = startY;
                startY = oldEndy;
            }

            if (endX == startX || endY == startY)
            {
                plr.SendErrorMessage("Invalid region selection. Try again or use '/setregion cancel' to cancel.");
                return;
            }

            TShock.Regions.AddRegion(startX, startY, (endX - startX), (endY - startY), rname, plr.User.Name, Main.worldID.ToString());
            plr.SendSuccessMessage("Set region " + rname);
            plr.SetData<bool>("awaitGD", false);
            args.Handled = true;
        }
#endregion

#region Command
        private void GDRegion(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax: /setregion <region name>");
                return;
            }

            if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "cancel")
            {
                args.Player.SetData<bool>("awaitGD", false);
                args.Player.SendSuccessMessage("Canceled region selection.");
                return;
            }

            string rname = string.Join(" ", args.Parameters);

            if (TShock.Regions.GetRegionByName(rname) != null)
            {
                args.Player.SendErrorMessage($"Region {rname} already exists.");
                return;
            }

            args.Player.SetData<bool>("awaitGD", true);
            args.Player.SetData<string>("GDname", rname);
            args.Player.SendSuccessMessage("Use The Grand Design to select a region area.");
        }
#endregion
    }
}
