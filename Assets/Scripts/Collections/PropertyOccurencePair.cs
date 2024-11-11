namespace Assets.Scripts.SpaceRace.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;
    using Unity.Mathematics;


    /// <summary>
    /// Dictionaries with more work but readily serializable and an appropraite "addition" method
    /// </summary>
    public class PropertyOccurencePair
    {
        [XmlAttribute]        
        public string Id;
        [XmlAttribute]
        public int Occurences;

        public PropertyOccurencePair(){}

        public PropertyOccurencePair(string id, int occurences)
        {
            Id = id;
            Occurences = occurences;
        }
        public override string ToString()
        {
            return (Occurences > 1)? string.Format("{0} x{1:D}", Id, Occurences) : Id;
        }
    }

    public class PropertyOccurenceList : List<PropertyOccurencePair>
    {
        public PropertyOccurenceList(){}
        public PropertyOccurenceList(IEnumerable<PropertyOccurencePair> pairs)
        {

            foreach (PropertyOccurencePair pop in pairs) AddPair(pop);

        }

        public PropertyOccurenceList(IEnumerable<PropertyOccurenceList> list)
        {
            foreach (PropertyOccurenceList pol in list)
            {
                foreach (PropertyOccurencePair pop in pol) AddPair(pop);
            }
        }
        public bool IsSame(PropertyOccurenceList list)
        {
            if (Count != list.Count) return false;
            // note this check assumes no duplicate entries
            foreach (PropertyOccurencePair pop in this)
            {
                if (list.Find(h => h.Id == pop.Id && h.Occurences == pop.Occurences) == null) return false;
            }
            return true;
        }
        public void AddPair(PropertyOccurencePair pair)
        {
            if (pair.Occurences == 0) return;
            if (pair.Occurences < 0) throw new ArgumentException("Addpair used for a negative number of occurences");
            PropertyOccurencePair hop = Find(p => p.Id == pair.Id);
            if (hop == null) Add(new PropertyOccurencePair(pair.Id, pair.Occurences));
            else hop.Occurences += pair.Occurences;
        }
        public void AddPair(string id, int occurences)
        {
            if (occurences == 0) return;
            if (occurences < 0) throw new ArgumentException("Addpair used for a negative number of occurences");
            PropertyOccurencePair hop = Find(p => p.Id == id);
            if (hop == null) Add(new PropertyOccurencePair(id, occurences));
            else hop.Occurences += occurences;
        }
        public void IncreaseTo(string id, int occurences)
        {
            if (occurences == 0) return;
            if (occurences < 0) throw new ArgumentException("SetToMax used for a negative number of occurences");
            PropertyOccurencePair hop = Find(p => p.Id == id);
            if (hop == null) Add(new PropertyOccurencePair(id, occurences));
            else if (hop.Occurences < occurences) hop.Occurences = occurences;
        }
        public int TotalOccurences()
        {
            return this.Sum(pop => pop.Occurences);
        }
        public override string ToString()
        {
            return string.Join(", ", this.Select(pop => pop.ToString()));
        }
    }


}
