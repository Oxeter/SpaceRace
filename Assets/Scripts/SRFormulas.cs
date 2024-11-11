namespace Assets.Scripts.SpaceRace.Formulas
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using System.IO;
    using UnityEngine;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Craft.Parts.Modifiers;

    using Assets.Scripts.SpaceRace.Modifiers;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Projects;
    using ModApi.Common.Extensions;

    public static class SRFormulas
    {
        public const double SecondsPerYear = 31536000.0;
        public const double YearsPerSecond = 1/31536000.0;
        public const double SecondsPerDay = 86400.0;
        public const double DaysPerSecond = 1/86400.0;
        public const double MechanicalGuidanceTime = 180.0;
        public const float NovelIntegrations = 6F;
        public const int ExternalTestInterval = 10;

        public const double ClawbackFactor = -10.0;
        public const long SpawnCrewPrice = 1578L;

        public static float RushRate(bool i)
        {
            return i? Game.Instance.GameState.Career.TechTree.GetItemValue("SpaceRace.RushRate")?.ValueAsFloat ?? 1F : 1F;
        }    
        public static float RushPrice(bool i)
        {
            return i? 2F * Game.Instance.GameState.Career.TechTree.GetItemValue("SpaceRace.RushRate")?.ValueAsFloat -1F ?? 1F: 1F;
        }  
        public static float FailingDamage(float rand)
        {
            if (rand > 0.999) return 100F;
            if (rand > 0.99) return 10F;
            if (rand > 0.9) return 1F;
            return 0.01F;
        }
        public static double InventoryLimit(double rate)
        {
            return rate * 183.0 / Math.Log(2);
        }
        public const long TechHire = 100000;
        public const long TechProgress = 500;

        public const long TechUpkeep = 250;
        public const long AstroHire = 250000;

        public const long AstroUpkeep = 500;

        public static long ContractUpkeep(int num)
        {
            if (num >0) num -= 1;
            return 5000L * num * num;
        }

        public static readonly List<string> AstroNames = new List<string>()
        {
            "Alan Shepard", "Scott Carpenter", "John Glenn", "Gus Grissom", "Wally Shirra", "Deke Slatyon", "Gordon Cooper", 
            "Edward White", "Neil Armstrong", "Jim McDivitt", "John Young", "Elliot See", "Pete Conrad", "Frank Borman", "Thomas Stafford", "Jim Lovell"
        };
    }
}