namespace Assets.Scripts
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using System.Xml.Linq;

    using ModApi;
    using ModApi.Common;
    using ModApi.Mods;
    using ModApi.Ui.Inspector;
    using ModApi.Flight;
    using ModApi.Flight.Events;
    using Assets.Scripts.Flight;
    using Assets.Scripts.Flight.UI;
    using Assets.Scripts.Design;
    using ModApi.Scenes.Events;
    using ModApi.Ui;
    using ModApi.Math;

    using Jundroo.ModTools;   
    using UnityEngine;

    using HarmonyLib;
    using Assets.Scripts.Ui;
    using Jundroo.ModTools.Core;
    using System.Drawing.Printing;


    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod
    {
 

        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() : base()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the mod object.
        /// </summary>
        /// <value>The singleton instance of the mod object.</value>
        public static Mod Instance { get; } = GetModInstance<Mod>();

        protected override void OnModInitialized()
        {
            Harmony harmony = new Harmony("com.oxeter.spacerace");
            harmony.PatchAll();
        }
        public override void OnModLoaded()
        {

        }



    }


}
