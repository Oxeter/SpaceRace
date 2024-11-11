namespace Assets.Scripts.SpaceRace.Collections
{
    using System.Collections.Generic;
    using System;
    using ModApi.Craft.Parts;
    using System.Xml.Linq;
    using ModApi.Common.Extensions;

    public class TankRadiusOccurenceTriple
    {
        public string Construction;

        public float Radius;
        public int Occurences;
        public TankRadiusOccurenceTriple(){}
        public TankRadiusOccurenceTriple(string construction, float radius, int occurences){
            Construction = construction;
            Radius = radius;
            Occurences = occurences;
        }
        public TankRadiusOccurenceTriple(XElement partdata)
        {
            XElement tankdata = partdata.Element("SpaceRace.SRFuelTank");
            if (tankdata == null) throw new ArgumentException("This is not a spacerace tank");
            Construction = tankdata.GetStringAttribute("ConstructionTypeId");
            XElement fuselage = partdata.Element("Fuselage");
            Radius = Math.Max(fuselage.GetVector2Attribute("bottomScale").x, fuselage.GetVector2Attribute("topScale").x);
            Occurences = 1;
        }
        public bool IsSame(TankRadiusOccurenceTriple trot)
        {
            return Construction == trot.Construction && Radius == trot.Radius && Occurences == trot.Occurences;
        }
    
    }

    public class TankList : List<TankRadiusOccurenceTriple>
    {
        public TankList(){}
        public TankList(IEnumerable<TankList> list)
        {
            foreach (TankList tl in list)
            {
                foreach (TankRadiusOccurenceTriple trot in tl) AddTriple(trot);
            }
        }
        public bool IsSame(TankList list)
        {
            if (Count != list.Count) return false;
            // note this check assumes no duplicate entries
            foreach (TankRadiusOccurenceTriple trot in this)
            {
                if (list.Find(h => h.IsSame(trot)) == null) return false;
            }
            return true;
        }
        public void AddTriple(TankRadiusOccurenceTriple trot)
        {
            if (trot.Occurences == 0) return;
            if (trot.Occurences < 0) throw new ArgumentException("Addtriple used for a negative number of occurences");
            TankRadiusOccurenceTriple match = Find(p => p.Construction == trot.Construction && p.Radius == trot.Radius);
            if (match == null) Add(new TankRadiusOccurenceTriple(trot.Construction, trot.Radius, trot.Occurences));
            else match.Occurences += trot.Occurences;
        }
    }

}
