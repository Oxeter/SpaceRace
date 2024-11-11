namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    /// <summary>
    /// The code and the Harmony patches that go with it don't seem to work.  Don't use for now.
    /// </summary>
    public class SRCommandPodScript : PartModifierScript<SRCommandPodData>
    {
        public IFuelSource HPNitrogenFuelSource { get; set; }
        public IFuelSource HPMonoFuelSource { get; set; }
        public IFuelSource HPMMHFuelSource { get; set; }
    }
}