using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurnSim {

    public class SimRulesClass {
        public double MinUnitDistance;
        public double EngageRange;
    }

    public class UnitDefClass {
        public string Class;
        public int Strength;
        public int Agility;
        public int Toughness;
        public int WeaponSkill;
        public int BalisticSkill;
        public int BaseHP;
        public int MovementPerSP;
        public int MaxSP;
        public int MaxOP;
    }

    public class WeaponClass {

        public string Name;
        public int OP_cost;
        public int MinDamage;
        public int MaxDamage;
        public double Range;
    }

}
