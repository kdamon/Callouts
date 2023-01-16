using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using FivePD.API;
using FivePD.API.Utils;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace FirstCallout
{
    [CalloutProperties(name:"Street Fight", author:"Granpa Rex",version:"2.0")]
    public class StreetFight : Callout
    {
        // Declaring the ped variables
        private Ped suspect1, suspect2;

        // Random number genny
        private readonly Random rng = new Random();

        // List of possible locations
        private List<Vector3> Locations = new List<Vector3>()
        {
            new Vector3(1921.76f, 3717.21f, 32.52f),
            new Vector3(1385.81f, 3596f, 34.89f),
            new Vector3(1711.58f, 4942.36f, 42.13f)
        };

        // List of possible weapons
        private WeaponHash[] weapons = new WeaponHash[]
        {
            WeaponHash.Unarmed,
            WeaponHash.Bat,
            WeaponHash.Hammer,
            WeaponHash.KnuckleDuster
        };

        public StreetFight()
        {
            // Callout location
            InitInfo(Locations.SelectRandom());

            // Callout properties
            ShortName = "Street Fight";
            CalloutDescription = "There is a report of two people fighting at the convience store, please respond code 3";
            ResponseCode = 3;
            StartDistance = 150f;
        }

        public async override Task<bool> CheckRequirements()
        {
            if (World.CurrentDayTime.Hours >= 19 && World.CurrentDayTime.Hours < 3)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override async Task OnAccept()
        {
            // Setting the blip on the map
            InitBlip();

            // Updating the callout data
            UpdateData();
        }

        public async override void OnStart(Ped player)
        {
            // Wait for a player to start the callout
            base.OnStart(player);

            // Create a ped variable for assigned players
            Ped unit = AssignedPlayers.FirstOrDefault();

            // Spawn and assign peds to player variables
            suspect1 = await SpawnPed(RandomUtils.GetRandomPed(), Location);
            suspect2 = await SpawnPed(RandomUtils.GetRandomPed(), Location);

            // Give suspects the weapon list
            suspect1.Weapons.Give(weapons.SelectRandom(), 1, true, true);
            suspect2.Weapons.Give(weapons.SelectRandom(), 1, true, true);

            // Keep peds on task
            suspect1.AlwaysKeepTask = true;
            suspect2.AlwaysKeepTask = true;

            // Block world events from affecting peds
            suspect1.BlockPermanentEvents = true;
            suspect2.BlockPermanentEvents = true;

            // Delay
            while (World.GetDistance(unit.Position, suspect1.Position) > 50f) { await BaseScript.Delay(250); }

            // Task peds to fight
            suspect1.Task.FightAgainst(suspect2);
            suspect2.Task.FightAgainst(suspect1);

            // Add blips
            suspect1.AttachBlip();
            suspect2.AttachBlip();

            // Alive checker
            while (suspect1.IsAlive || suspect2.IsAlive) { await BaseScript.Delay(250); }

            // Random event
            int x = rng.Next(1, 100 + 1);
            if (x <= 60)
            {
                try
                {
                    if (suspect1.IsAlive) { suspect1.Task.ReactAndFlee(unit); }
                    if (suspect2.IsAlive) { suspect2.Task.ReactAndFlee(unit); }
                }
                catch { }
            }
            else if (x >= 61)
            {
                try
                {
                    if (suspect1.IsAlive) { suspect1.Task.FightAgainst(unit); }
                    if (suspect2.IsAlive) { suspect2.Task.FightAgainst(unit); }
                }
                catch { }
            }
        }

        public override void OnCancelBefore()
        {
            base.OnCancelBefore();

            try
            {
                if (suspect1.IsAlive && !suspect1.IsCuffed) { suspect1.Task.WanderAround(); suspect1.BlockPermanentEvents = false; suspect1.AlwaysKeepTask = false; }
                if (suspect2.IsAlive && !suspect2.IsCuffed) { suspect2.Task.WanderAround(); suspect2.BlockPermanentEvents = false; suspect2.AlwaysKeepTask = false; }
            }
            catch { }
        }
    }
}
