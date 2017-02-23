using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TurnSim {

    public class SimSession {

        public List<AIPlayerClass> AIPlayers;
        public List<UnitClass> Units;
        public SimRulesClass SimRules;
        public Random RNG;
        public Visualiser Vis;
        public Logger Log;

        dynamic config;

        public SimSession(PictureBox RenderTarget = null, TextBox LogTarget = null, string LogFile = null, int UILogLevel = 0, int FileLogLevel = 0) {
            RNG = new Random();
            Vis = new Visualiser(this, RenderTarget);
            //Log = new Logger(LogTarget,_EnableUI:false);
            Log = new Logger(LogTarget, LogFile, UILogLevel, FileLogLevel);
            config = JsonConvert.DeserializeObject(System.IO.File.ReadAllText("Config.json"));
            SimRules = JsonConvert.DeserializeObject<SimRulesClass>(config.SimSettings.ToString());

            SimSetup();
            SetupMatch();
            Vis.RenderAll();
            
        }


        public void SimSetup() {
            AIPlayers = new List<AIPlayerClass>();

            AIPlayers.Add(new AIPlayerClass { TeamID = 0, Name = "Red", Colour = new SolidBrush(Color.Red), Strategy = "hunt", Sim = this, Log = Log });
            AIPlayers.Add(new AIPlayerClass { TeamID = 1, Name = "Blue", Colour = new SolidBrush(Color.Blue), Strategy = "weakest", Sim = this, Log = Log });
            AIPlayers.Add(new AIPlayerClass { TeamID = 2, Name = "Yellow", Colour = new SolidBrush(Color.Yellow), Strategy = "weakest", Sim = this, Log = Log });
        }

        public void SetupMatch() {

            Units = new List<UnitClass>();

            List<WeaponClass> WeaponList = JsonConvert.DeserializeObject<List<WeaponClass>>(config.Weapons.ToString());
            List<UnitDefClass> UnitList = JsonConvert.DeserializeObject<List<UnitDefClass>>(config.UnitTypes.ToString());

            foreach (var team in AIPlayers) {

                foreach (var member in config.TeamConfig.Units) {
                    Units.Add(new UnitClass {
                        ID = Units.Count,
                        TeamID = team.TeamID,
                        BaseStats = UnitList.Find(o => o.Class == member.Class.Value),
                        Weapon = WeaponList.Find(o => o.Name == member.Weapon.Value),
                        X = member.X.Value + team.TeamID * 16,
                        Y = member.Y.Value,
                        Name = member.Class,
                        Team = team
                    });
                    Units.Last().InitStats();
                }
            }

        }

        public void StartSim() {

            while (true) {

                SetupMatch();

                Log.important("Starting Match -------------------", true);
                int Round = 0;
                bool NotEnded = true;

                Log.debug("Turn order randomised");
                Units.Shuffle(RNG);

                foreach (var unit in Units) {
                    Log.debug(unit.ToString());
                }

                while (NotEnded) {
                    Round++;
                    Log.info("Start Round " + Round.ToString());
                    foreach (var unit in Units) unit.RoundReset();

                    foreach (var unit in Units) {
                        if (!unit.Alive) {
                            continue;
                        }

                        Log.info($"Turn start: " + unit.ToString());
                        unit.Team.TakeTurn(unit);
                        Application.DoEvents();
                    }

                    Log.info("End Round " + Round.ToString());

                    var AliveTeams = Units.Where(o => o.Alive).GroupBy(o => o.TeamID).ToList();

                    if (AliveTeams.Count == 1) {
                        Log.important($"Team {AliveTeams.First().First().Team.Name} is victorious! End of match");
                        AliveTeams.First().First().Team.Wins++;
                        NotEnded = false;
                        break;
                    }

                    if (AliveTeams.Count == 0) {
                        Log.important($"Its a Draw! End of match");
                        NotEnded = false;
                        break;
                    }
                }
                Log.important(String.Join(" ",AIPlayers.Select(o=>o.ToString())));
            }
        }

    }


    public class AIPlayerClass {
        public int TeamID;
        public string Name;
        public Brush Colour;
        public int Wins;

        public SimSession Sim;
        public Logger Log;

        public string Strategy;

        public AIPlayerClass() {
            Wins = 0;
        }

        public override string ToString() {
            return $"{Name} ({TeamID}): {Wins} Wins";
        }

        public void TakeTurn(UnitClass unit) {

            if (Strategy == "hunt") {
                Turn_KillHeros(unit);
            } else if (Strategy == "weakest") {
                Turn_KillWeakest(unit);
            } else {
                Turn_KillNearest(unit);
            }


        }

        public void Turn_KillWeakest(UnitClass unit) {

            while (true) {

                //Log.debug($"SP: {Units[Uidx].SP}, OP: {Units[Uidx].OP}");

                var CanAttack = unit.OP >= unit.Weapon.OP_cost;
                var CanMove = unit.SP > 0;

                if (!CanMove && !CanAttack) {
                    Log.debug("No OP or SP, end turn");
                    break;
                }

                //where are enemies?
                var Enemies = Sim.Units.Where(o => o.TeamID != unit.TeamID && o.Alive)
                    .Select(o => new { unit = o, dist = Helpers.Dist(o.X, o.Y, unit.X, unit.Y) })
                    .OrderBy(o => o.unit.HP).ThenBy(o => o.dist);

                if (!Enemies.Any()) {
                    Log.debug("No enemies, end turn");
                    break;
                }

                var NearestEnemy = Enemies.First();

                // Log.debug($"There are {Enemies.Count} closest is {EnemyDist}m");

                //can we attack?
                if (NearestEnemy.dist <= unit.Weapon.Range) {
                    if (CanAttack) {
                        DoAttack(unit, NearestEnemy.unit);
                        continue;
                    } else {
                        Log.debug("Out of attacks");
                        break;
                    }
                }

                if (!CanMove) {
                    Log.debug("Can attack but out of SP, end turn");
                    break;
                }

                //there is an enemy, we can move
                var MoveAllowed = MoveToAttack(unit, NearestEnemy.unit);
                if (!MoveAllowed) {
                    Log.debug("Not enough room to attack, waiting");
                    break;
                }
            }
        }

        public void Turn_KillHeros(UnitClass unit) {

            while (true) {

                //Log.debug($"SP: {Units[Uidx].SP}, OP: {Units[Uidx].OP}");

                var CanAttack = unit.OP >= unit.Weapon.OP_cost;
                var CanMove = unit.SP > 0;

                if (!CanMove && !CanAttack) {
                    Log.debug("No OP or SP, end turn");
                    break;
                }

                //where are enemies?
                var Enemies = Sim.Units.Where(o => o.TeamID != unit.TeamID && o.Alive)
                    .Select(o => new { unit = o, dist = Helpers.Dist(o.X, o.Y, unit.X, unit.Y), priority = (o.BaseStats.Class == "Henchman" ? 0 : 1) })
                    .OrderByDescending(o => o.priority).ThenBy(o => o.dist);

                if (!Enemies.Any()) {
                    Log.debug("No enemies, end turn");
                    break;
                }

                var NearestEnemy = Enemies.First();

                // Log.debug($"There are {Enemies.Count} closest is {EnemyDist}m");

                //can we attack?
                if (NearestEnemy.dist <= unit.Weapon.Range) {
                    if (CanAttack) {
                        DoAttack(unit, NearestEnemy.unit);
                        continue;
                    } else {
                        Log.debug("Out of attacks");
                        break;
                    }
                }

                if (!CanMove) {
                    Log.debug("Can attack but out of SP, end turn");
                    break;
                }

                //there is an enemy, we can move
                var MoveAllowed = MoveToAttack(unit, NearestEnemy.unit);
                if (!MoveAllowed) {
                    Log.debug("Not enough room to attack, waiting");
                    break;
                }
            }
        }

        public void Turn_KillNearest(UnitClass unit) {

            while (true) {

                //Log.debug($"SP: {Units[Uidx].SP}, OP: {Units[Uidx].OP}");

                var CanAttack = unit.OP >= unit.Weapon.OP_cost;
                var CanMove = unit.SP > 0;

                if (!CanMove && !CanAttack) {
                    Log.debug("No OP or SP, end turn");
                    break;
                }

                //where are enemies?
                var Enemies = Sim.Units.Where(o => o.TeamID != unit.TeamID && o.Alive);

                if (!Enemies.Any()) {
                    Log.debug("No enemies, end turn");
                    break;
                }

                var EnemyDistList = Enemies.Select(o => Helpers.Dist(o.X, o.Y, unit.X, unit.Y)).ToArray();
                int NearestEnemyInd = Array.IndexOf(EnemyDistList, EnemyDistList.Min());
                var NearestEnemy = Enemies.ElementAt(NearestEnemyInd);
                var EnemyDist = EnemyDistList[NearestEnemyInd];

                // Log.debug($"There are {Enemies.Count} closest is {EnemyDist}m");

                //can we attack?
                if (EnemyDist <= unit.Weapon.Range) {
                    if (CanAttack) {
                        DoAttack(unit, NearestEnemy);
                        continue;
                    } else {
                        Log.debug("Out of attacks");
                        break;
                    }
                }

                if (!CanMove) {
                    Log.debug("Can attack but out of SP, end turn");
                    break;
                }

                //there is an enemy, we can move
                var MoveAllowed = MoveToAttack(unit, NearestEnemy);
                if (!MoveAllowed) {
                    Log.debug("Not enough room to attack, waiting");
                    break;
                }
            }
        }

        public bool MoveToAttack(UnitClass unit, UnitClass enemy) {
            bool Madeit = true;

            var MoveTo = FindAttackSpot(unit, enemy);

            if (MoveTo.isValid) {
                Log.debug($"Moving to engage {enemy.ToString()}");
            } else {
                Log.debug($"Too far: Approaching {enemy.ToString()}");
                MoveTo = FindNearestSpot(unit, enemy);
                Madeit = false;
            }

            MoveToSpot(unit, MoveTo.X, MoveTo.Y);

            return Madeit;
        }

        private dblPoint FindNearestSpot(UnitClass unit, UnitClass enemy) {
            var RealDist = Helpers.Dist(unit, enemy);
            var MoveDist = Math.Min(RealDist, unit.MaxMoveDist);

            var xgrad = ((enemy.X - unit.X) / RealDist);
            var ygrad = ((enemy.Y - unit.Y) / RealDist);

            for (var Dist = MoveDist; Dist > 0; Dist -= 1) {
                var test = new dblPoint() { X = unit.X + xgrad * Dist, Y = unit.Y + ygrad * Dist };
                if (LocationValid(unit, test.X, test.Y)) return new dblPoint() { isValid = true, X = test.X, Y = test.Y };
                // Sim.Vis.Dot(test.X, test.Y);
            }
            return new dblPoint() { isValid = false, X = unit.X, Y = unit.Y };
        }

        private dblPoint FindAttackSpot(UnitClass unit, UnitClass enemy) {
            double thstart = Math.Atan2(unit.Y - enemy.Y, unit.X - enemy.X);

            for (double thoff = 0; thoff <= Math.PI; thoff += 0.2) {
                for (int direction = -1; direction <= 1; direction += 2) {
                    double th = thstart + thoff * direction;
                    var test = new dblPoint() {
                        X = enemy.X + Math.Cos(th) * unit.Weapon.Range * Sim.SimRules.EngageRange,
                        Y = enemy.Y + Math.Sin(th) * unit.Weapon.Range * Sim.SimRules.EngageRange
                    };

                    //Sim.Vis.Dot(test.X, test.Y);
                    if (Helpers.Dist(unit, test) < unit.MaxMoveDist) {
                        if (LocationValid(unit, test.X, test.Y)) return new dblPoint() { isValid = true, X = test.X, Y = test.Y };
                    } else {
                        return new dblPoint() { isValid = false, X = 0, Y = 0 };
                    }

                    if (thoff == 0) break;
                }
            }

            return new dblPoint() { isValid = false, X = 0, Y = 0 };
        }


        public void MoveToSpot(UnitClass unit, double X, double Y) {
            var DistToMove = Helpers.Dist(unit, X, Y);

            unit.SP -= (int)Math.Ceiling(Math.Round(DistToMove, 2) / unit.BaseStats.MovementPerSP);
            var steps = Math.Round(DistToMove) + 1;
            if (!Sim.Vis.Enabled) steps = 1;

            var xgrad = (X - unit.X) / steps;
            var ygrad = (Y - unit.Y) / steps;

            for (var i = 0; i < steps; i++) {
                unit.X += xgrad;
                unit.Y += ygrad;
                Sim.Vis.RenderUnit(unit);
            }
            Sim.Vis.RenderAll();
        }

        public bool LocationValid(UnitClass unit, double X, double Y) {
            foreach (var otherunit in Sim.Units.Where(o => o.Alive && (o.ID != unit.ID))) {
                if (Helpers.Dist(X, Y, otherunit.X, otherunit.Y) < Sim.SimRules.MinUnitDistance) {
                    return false;
                }
            }
            return true;
        }

        public void DoAttack(UnitClass atk, UnitClass def) {

            atk.OP -= atk.Weapon.OP_cost;

            var Damage = (int)Math.Round(Sim.RNG.Next(atk.Weapon.MinDamage, atk.Weapon.MaxDamage + 1) * (1 + atk.MeleeDamageMod));
            var AtkRoll = Sim.RNG.Next(0, 100);
            var Hit = AtkRoll >= def.DodgeChange * 100;
            Damage = Hit ? Damage : 0;

            if (Hit) {
                def.HP -= Damage;
                Sim.Vis.RenderAll();
            }

            Sim.Vis.RenderAttack(atk, def, Damage);

            Log.debug($"Attacking {def.ToString()}, Damage: {Damage}, Chance to dodge: {def.DodgeChange * 100}, Rolled: {AtkRoll} - {(Hit ? "HIT" : "MISS")}");

            if (!def.Alive) {
                Log.info($"Unit Killed: " + def.ToString());
            }
        }

        public double TeamHP() {
            return (double)Sim.Units.Where(o => o.TeamID == TeamID).Sum(o => Math.Max(0, o.HP)) / Sim.Units.Where(o => o.TeamID == TeamID).Sum(o => o.MaxHP);
        }

    }

    public class UnitClass {
        public UnitDefClass BaseStats;
        public WeaponClass Weapon;
        public AIPlayerClass Team;

        public int ID;
        public int TeamID;
        public string Name;
        public int MaxHP;
        public double X;
        public double Y;
        public double OldX;
        public double OldY;
        public double DodgeChange;
        public double MeleeDamageMod;
        public bool Alive;
        public double MaxMoveDist;
        public bool HadTurn;

        private int _HP;
        public int HP {
            get { return _HP; }
            set {
                _HP = value;
                Alive = _HP > 0;
            }
        }

        private int _OP;
        public int OP {
            get { return _OP; }
            set {
                _OP = value;
                if (_OP < 0) throw new System.Exception("CHEATER");
                HadTurn = true;
            }
        }

        private int _SP;
        public int SP {
            get { return _SP; }
            set {
                _SP = value;
                if (_SP < 0) throw new System.Exception("CHEATER");
                MaxMoveDist = _SP * BaseStats.MovementPerSP;
                HadTurn = true;
            }
        }

        public void InitStats() {
            MaxHP = (int)Math.Round(BaseStats.BaseHP * (1 + BaseStats.Toughness * .03));
            HP = MaxHP;
            DodgeChange = BaseStats.Agility * 0.05;
            MeleeDamageMod = BaseStats.Strength * 0.03;
            RoundReset();
        }

        public void RoundReset() {
            OP = BaseStats.MaxOP;
            SP = BaseStats.MaxSP;
            OldX = X;
            OldY = Y;
            HadTurn = false;
        }

        public override string ToString() {
            return $"{Team.Name}: ID {ID}: {Name} ({BaseStats.Class}) {Math.Round((float)HP / MaxHP * 100)}%  ";
        }

    }




}
