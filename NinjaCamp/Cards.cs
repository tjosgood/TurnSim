using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaCamp {

    public enum CardType {
        Empty,
        Shadow,
        Leap,
        Dodge,
        Stealth,
        Ambush,
        Evade,
        Sprint,
        Wall,
        Trap,
        EvadeWood,
        SprintWood,
        PlaceMeeple
    }


    
    public class Card {

        public CardType Type { get; set; }
        public int ScoreValue { get; set; }
        public bool Walkable { get; set; }
        public bool Playable { get; set; }

        public Color Color { get; set; }
        public Color BGColor { get; set; }
        public int NumInDeck { get; set; }

        public Card() {
            Type = CardType.Empty;
            ScoreValue = 0;
            Walkable = true;
            Playable = true;
            NumInDeck = 8;
            Color = Color.White;
            BGColor = Color.White;
        }

        public override string ToString() {
            return Type.ToString().Replace("Wood","");
        }

        public virtual List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board) {
            return new List<PossibleMove>();
        }

        public virtual List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board, int PlayerID) {
            return PossibleMoves(x, y, Board);
        }

        public List<PossibleMove> Walk(int x, int y, int steps, bool allowDiag, bool StealthScore, BoardClass Board) {

            var PossiblePaths = Board.GetWalkPattern(steps, allowDiag);
            var Result = new List<PossibleMove>();
    
            foreach (var path in PossiblePaths) {
                int xcur = x, ycur = y;
                int sx = -1, sy = -1;
                var TotalScore = 0;
                var isValid = true;

                foreach (var step in path) {
                    xcur += step.X;
                    ycur += step.Y;
                    if (StealthScore && sx == -1) {
                        sx = xcur;
                        sy = ycur;
                    }
                    if (!Board.IsWalkable(xcur, ycur)) {
                        isValid = false;
                        break;
                    };
                    TotalScore += Board.Tiles[xcur, ycur].ScoreValue;
                }

                if (isValid) {
                    Result.Add(new PossibleMove() { FromX = x, FromY = y, ToX = xcur, ToY = ycur, FirstX = sx, FirstY = sy, ScoreValue = StealthScore ? TotalScore : Board.Tiles[xcur, ycur].ScoreValue, Card = this, Destination = Board.Tiles[xcur, ycur] });
                }

            }
            return Result.Distinct().ToList();
        }
    }


    
    public class PlaceMeepleCard : Card {
        public PlaceMeepleCard() : base() {
            Type = CardType.PlaceMeeple;
        }

        public override List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board, int PlayerID) {
            var res = new List<PossibleMove>();

            var Occupied = Board.GetOccupiedTiles(PlayerID).Select(o => o.Type).ToList(); 

            for (int i = 0; i < Board.TilesX; i++) {
                for (int j = 0; j < Board.TilesY; j++) {
                    if (Board.IsWalkable(i, j)) {
                        if (!Occupied.Contains(Board.Tiles[i, j].Type)) {
                            res.Add(new PossibleMove() { FromX = x, FromY = y, ToX = i, ToY = j, ScoreValue = Board.Tiles[i, j].ScoreValue, Card = this, Destination = Board.Tiles[i, j] });
                        }
                    }
                }
            }

            if (Board.Players[PlayerID].AI) {

                var newres = new List<PossibleMove>();
                var Types = res.Select(o => new { Move = o, Type = Board.Tiles[o.ToX,o.ToY].Type }).Where(o=>o.Type != CardType.Trap).GroupBy(o=>o.Type).Select(o=>o.ToList()).ToList();

                foreach (var g in Types) {
                    g.Shuffle();
                    newres.AddRange(g.Take(3).Select(o=>o.Move));                
                }
                res = newres;
            }

            return res;
        }

    }

    
    public class EmptyCard : Card {
        public EmptyCard() : base() {
            Type = CardType.Empty;
            Walkable = false;
            Playable = false;
            NumInDeck = 0;
        }
    }

    
    public class ShadowCard : Card {
        public ShadowCard() : base() {
            Type = CardType.Shadow;
            ScoreValue = 3;
            Color = Color.Black;
        }
        public override List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board, int PlayerID) {
            var m =  Board.ShadowLastPlayedCard.PossibleMoves(x, y, Board, PlayerID);       
            m.ForEach(o => o.isShadow = true);
            return m;

        }
    }

    
    public class LeapCard : Card {
        public LeapCard() : base() {
            Type = CardType.Leap;
            ScoreValue = 3;
            Color = Color.LightBlue;
        }

        private PossibleMove CheckPath(int x, int y, int xoff, int yoff, BoardClass Board) {
            int xcur = x, ycur = y;
            var gaplen = 0;

            for (int steps = 0; steps < 100; steps++) {
                xcur += xoff;
                ycur += yoff;

                if (!Board.InBounds(xcur, ycur)) return null;
                var valid = Board.IsWalkable(xcur, ycur);

                if (Board.Tiles[xcur, ycur].Type != CardType.Empty) {
                    if (gaplen > 0 && valid) return new PossibleMove() { FromX = x, FromY = y, ToX = xcur, ToY = ycur, ScoreValue = Board.Tiles[xcur, ycur].ScoreValue, Card = this, Destination = Board.Tiles[xcur, ycur] };
                    return null;
                }

                gaplen++;
            }
            return null;
        }

        public override List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board) {
            var res = new List<PossibleMove>();
            res.Add(CheckPath(x, y, 1, 0, Board));
            res.Add(CheckPath(x, y, -1, 0, Board));
            res.Add(CheckPath(x, y, 0, 1, Board));
            res.Add(CheckPath(x, y, 0, -1, Board));
            return res.Where(o => o != null).ToList(); ;
        }

    }

    
    public class DodgeCard : Card {
        public DodgeCard() : base() {
            Type = CardType.Dodge;
            ScoreValue = 1;
            Color = Color.Yellow;
        }

        public override List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board) {

            var Res = new List<PossibleMove>();

            Res.AddRange(Walk(x, y, 1, true, false, Board));
            Res.AddRange(Walk(x, y, 2, true, false, Board));

            return Res.Distinct().ToList();

        }
    }

    
    public class StealthCard : Card {
        public StealthCard() : base() {
            Type = CardType.Stealth;
            ScoreValue = 0;
            Color = Color.LightGreen;
        }
        public override List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board) {
            return Walk(x, y, 2, false, true, Board);
        }

    }

    
    public class AmbushCard : Card {
        public AmbushCard() : base() {
            Type = CardType.Ambush;
            ScoreValue = 5;
            Color = Color.DarkOrange;
        }

        private PossibleMove CheckPath(int x, int y, int xoff, int yoff, BoardClass Board) {
            int xcur = x, ycur = y;

            var Me = Board.GetMeepleAt(x, y);
            if (Me == null) return null;

            for (int steps = 0; steps < 100; steps++) {
                xcur += xoff;
                ycur += yoff;
                if (!Board.IsWalkable(xcur, ycur, false)) break;

                var Enemy = Board.GetMeepleAt(xcur, ycur);

                if (Enemy != null) {
                    if (Enemy.Player.ID == Me.Player.ID) break;
                    if (Board.IsWalkable(xcur + xoff, ycur + yoff)) {
                        return new PossibleMove() { FromX = x, FromY = y, ToX = xcur, ToY = ycur, ScoreValue = Board.Tiles[xcur, ycur].ScoreValue, Card = this, Destination = Board.Tiles[xcur, ycur] };
                    }
                }

            }
            return null;
        }

        public override List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board) {
            var res = new List<PossibleMove>();
            res.Add(CheckPath(x, y, 1, 0, Board));
            res.Add(CheckPath(x, y, -1, 0, Board));
            res.Add(CheckPath(x, y, 0, 1, Board));
            res.Add(CheckPath(x, y, 0, -1, Board));
            return res.Where(o => o != null).ToList(); ;
        }
    }

    
    public class EvadeCard : Card {
        public EvadeCard() : base() {
            Type = CardType.Evade;
            ScoreValue = 4;
            Color = Color.Purple;
        }

        public override List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board) {
            return Walk(x, y, 3, false, false, Board);
        }

    }

    
    public class EvadeWoodCard : EvadeCard {
        public EvadeWoodCard() : base() {
            Type = CardType.EvadeWood;
            ScoreValue = 0;
            Color = Color.Brown;
            NumInDeck = 4;
        }
    }

    
    public class SprintCard : Card {
        public SprintCard() : base() {
            Type = CardType.Sprint;
            ScoreValue = 2;
            Color = Color.LightSalmon;
        }

        private PossibleMove CheckPath(int x, int y, int xoff, int yoff, BoardClass Board) {
            int xcur = x, ycur = y;
            PossibleMove LastGood = null;

            for (int steps = 0; steps < 100; steps++) {
                xcur += xoff;
                ycur += yoff;
                if (!Board.IsWalkable(xcur, ycur)) break;
                LastGood = new PossibleMove() { FromX = x, FromY = y, ToX = xcur, ToY = ycur, ScoreValue = Board.Tiles[xcur, ycur].ScoreValue, Card = this, Destination = Board.Tiles[xcur, ycur] };
            }
            return LastGood;
        }

        public override List<PossibleMove> PossibleMoves(int x, int y, BoardClass Board) {
            var res = new List<PossibleMove>();
            res.Add(CheckPath(x, y, 1, 0, Board));
            res.Add(CheckPath(x, y, -1, 0, Board));
            res.Add(CheckPath(x, y, 0, 1, Board));
            res.Add(CheckPath(x, y, 0, -1, Board));
            return res.Where(o => o != null).ToList(); ;
        }

    }

    
    public class SprintWoodCard : SprintCard {
        public SprintWoodCard() : base() {
            Type = CardType.SprintWood;
            ScoreValue = 0;
            Color = Color.Brown;
            NumInDeck = 4;
        }
    }

    
    public class WallCard : Card {
        public WallCard() : base() {
            Type = CardType.Wall;
            ScoreValue = 0;
            Color = Color.Gray;
            BGColor = Color.Gray;
            Walkable = false;
            Playable = false;
            NumInDeck = 3;
        }
    }

    
    public class TrapCard : Card {
        public TrapCard() : base() {
            Type = CardType.Trap;
            ScoreValue = -1;
            Color = Color.DarkRed;
            BGColor = Color.LightPink;
            Playable = false;
            NumInDeck = 5;
        }
    }




}
