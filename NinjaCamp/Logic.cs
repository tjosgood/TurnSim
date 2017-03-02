
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace NinjaCamp {

    public class NinjaGame {

        public Random RNG;
        public Visualiser Vis;
        public Logger Log;
        public List<Card> Deck;
        public BoardClass Board;
        public int CurrentPlayer;
        public bool WaitForPlayer;
        public PossibleMove LastMove;
        public int NumPlayers;

        public NinjaGame(PictureBox RenderTarget = null, TextBox LogTarget = null, string LogFile = null, int UILogLevel = 0, int FileLogLevel = 0) {
            NumPlayers = 2;

            Log = new Logger(LogTarget, LogFile, UILogLevel, FileLogLevel);
            //Log = new Logger(LogTarget,_EnableUI:false);

            if (NumPlayers > 2) {
                Board = new BoardClass(8, 7, this, Log);
            } else {
                Board = new BoardClass(8, 6, this, Log);
            }

            RNG = new Random();
            Vis = new Visualiser(this, RenderTarget);
        }

        public void SimSetup() {
            Deck = new List<Card>();
            var DeckCards = new List<Card> { new EvadeCard(), new ShadowCard(), new LeapCard(), new DodgeCard(), new StealthCard(), new AmbushCard(), new SprintCard(), new WallCard(), new TrapCard() };

            foreach (var c in DeckCards) {
                for (int i = 0; i < c.NumInDeck; i++) {
                    Deck.Add(Helpers.CardFactory(c.Type));
                }
            }

            if (Board.TilesX * Board.TilesY > Deck.Count) {
                Deck.AddRange(Deck);
            }

            Deck.Shuffle(RNG);

            for (int x = 0; x < Board.TilesX; x++) {
                for (int y = 0; y < Board.TilesY; y++) {
                    Board.Tiles[x, y] = Deck.Pop();
                }
            }

            Board.Players = new List<Player>();
            Board.Meeples = new List<Meeple>();

            if (NumPlayers > 0) Board.Players.Add(new Player() { AI = true, Color = Color.Red, ID = 0, Name = "Red", Strategy = "hybrid", LookAhead = 2 });
            if (NumPlayers > 1) Board.Players.Add(new Player() { AI = true, Color = Color.Blue, ID = 1, Name = "Blue", Strategy = "hybrid", LookAhead = 2 });
            if (NumPlayers > 2) Board.Players.Add(new Player() { AI = true, Color = Color.Purple, ID = 2, Name = "Purple", Strategy = "hybrid", LookAhead = 0 });
            if (NumPlayers > 3) Board.Players.Add(new Player() { AI = true, Color = Color.Black, ID = 3, Name = "Black", Strategy = "hybrid", LookAhead = 0 });

            foreach (var p in Board.Players) {
                for (var i = 0; i < 3; i++) {
                    Board.Meeples.Add(new Meeple() { Player = p });
                    p.Hand.Add(new PlaceMeepleCard());
                }
                p.Hand.Add(new EvadeWoodCard());
                p.Hand.Add(new SprintWoodCard());
                p.UpdateHand();
            }

            LastMove = null;
        }

        public void Sleep(int ms) {
            var SW = new Stopwatch();
            SW.Start();
            while (SW.ElapsedMilliseconds < ms) {
                Application.DoEvents();
            }
        }

        public void StartMatch() {

            var Tally = new Dictionary<string, int>();
            foreach (var p in Board.Players) {
                Tally[p.ToString()] = 0;
            }

            int odd = 1;

            while (true) {
           
                SimSetup();

                odd = 1 - odd;
                var Playorder = Board.Players.ToList();
                if (odd == 1) Playorder.Reverse();

                Log.important("Start match!", true);

                while (true) {

                    foreach (var p in Playorder) {

                        Log.important($"Turn start: {p}", true);
                        CurrentPlayer = p.ID;
                        TakeTurn(p);
                        Vis.RenderAll();

                        //  Sleep(1000);
                        Application.DoEvents();
                    }

                    if (Board.Players.Where(o => !o.OutOfMoves).Count() == 0) {
                        Log.important("All players out!", true);
                        break;
                    }
                }

                foreach (var p in Board.Players) {
                    Log.important($"{p} Score: {p.Score(Board)}");
                }
                var Rank = Board.Players.OrderByDescending(o => o.Score(Board)).ToList();
                if (Rank[0].Score(Board) == Rank[1].Score(Board)) {
                    Log.important($"We have a tie!");
                } else {
                    Tally[Rank[0].ToString()] += 1;
                    Log.important($"Tally: " + String.Join(", ",Tally.Select(o=> $"{o.Key}: {o.Value}")));
                }

            }

        }

        public void TakeHumanTurn(Player p) {
            Log.important($"Waiting for player");
            var Form = Application.OpenForms.OfType<Form1>().First();

            Form.SelectedCard = null;
            Form.SelectedMeeple = null;
            Form.lblInstruction.Text = "Select a card from your list";
            Form.label3.Text = "Shadow Card is " + Board.ShadowLastPlayedCard.ToString();
            WaitForPlayer = true;
            while (WaitForPlayer) {
                Application.DoEvents();
            }
        }


        public void TakeTurn(Player p) {

            if (p.OutOfMoves) {
                Log.important($"{p} is out, skipping turn!");
                return;
            }

            var AllMoves = AllPossibleMoves(p);

            if (AllMoves.Count == 0) {
                Log.important($"{p} is out of moves!");
                p.OutOfMoves = true;
                return;
            }

            if (!p.AI) {
                TakeHumanTurn(p);
                return;
            }

            var MoveTree = AllMoves.Select(o => new List<PossibleMove>() { o }).ToList();

            var LookSteps = p.LookAhead;
            if (p.Hand.Count(o => o.Type == CardType.PlaceMeeple) == 3) LookSteps += 1;

            var SW = new Stopwatch();
            SW.Start();
            bool MovesExhausted = false;

            for (int LookAhead = 1; LookAhead <= LookSteps; LookAhead++) {
                Log.info($"Performing lookahead step {LookAhead}");
                var NewMoveTree = new List<List<PossibleMove>>();
                var LogState = Log.Enabled;
                Log.Enabled = false;

                foreach (var MoveCombo in MoveTree) {
                    if (MoveCombo.Count != LookAhead) continue;

                    var Virtualboard = Board.MakeCopy();
                    var VirtualPlayer = Virtualboard.Players[p.ID];

                    foreach (var m in MoveCombo) {
                        Virtualboard.PlayMove(m, VirtualPlayer);
                    }

                    VirtualPlayer.Hand = VirtualPlayer.Hand.Where(o => o.Type != CardType.Shadow).ToList();

                    var FutureMoves = AllPossibleMoves(VirtualPlayer, Virtualboard);
                    if (FutureMoves.Count == 0) continue;

                    foreach (var fm in FutureMoves) {
                        var newcombo = MoveCombo.ToList();
                        newcombo.Add(fm);
                        NewMoveTree.Add(newcombo);
                    }

                }

                Log.Enabled = LogState;

                if (NewMoveTree.Count > 0) {
                    MoveTree.AddRange(NewMoveTree);
                } else {
                    Log.important($"All moves enhausted after {LookAhead} steps");
                    MovesExhausted = true;
                    break;
                }

            }

            var BestOneMoveScore = AllMoves.Max(j => j.ScoreValue);
            var BestMove = AllMoves.First(o => o.ScoreValue == BestOneMoveScore);
            Log.info($"Best greedy move is {BestMove}, considered {MoveTree.Count} moves");

            List<PossibleMove> MyMove;
            if (p.ID == 1) {
                MyMove = DecideMove(p, MoveTree);
            } else {
                MyMove = DecideMoveOLD(p, MoveTree);
            }

            //foreach (var m in MyMove) {
            //    Vis.RenderMove(m);
            //    Vis.Refresh();
            //}

            if (MyMove.Sum(o => o.ScoreValue) < 0 && MovesExhausted && p.Hand.Count(o => o.Type == CardType.Shadow) == 0) {
                Log.important($"Not a good move! I pass");
                p.OutOfMoves = true;
            } else {
                if (MyMove.First().Destination.Type == CardType.Trap) {
                    Log.debug($"Its a trap!");
                }

                Board.PlayMove(MyMove.First(), p);
            }

        }

        private List<PossibleMove> DecideMove(Player p, List<List<PossibleMove>> MoveTree) {
            var ScoreForBestScorePath = MoveTree.Max(o => o.Sum(j => j.ScoreValue));
            var GoodPaths = MoveTree.Where(o => o.Sum(j => j.ScoreValue) >= ScoreForBestScorePath);

            var GroupByFirstMove = GoodPaths.GroupBy(o => o.First()).Select(o => new { First = o.Key, Count = o.Count(), Moves = o.ToList() }).
                OrderByDescending(o => o.Count).Take(3);

            var BestGroup = new List<List<PossibleMove>>();
            foreach (var g in GroupByFirstMove) BestGroup.AddRange(g.Moves);
            var gMax = BestGroup.Max(k => k.First().Card.ScoreValue);

            BestGroup = BestGroup.Where(o => o.First().Card.ScoreValue == gMax || o.First().Card.Type == CardType.Stealth).ToList();
            BestGroup.Shuffle();

            var MyMove = BestGroup.First();

            Log.important($"Strategy is {p.Strategy} so going with {MyMove.First()}");
            return MyMove;
        }

        private List<PossibleMove> DecideMoveOLD(Player p, List<List<PossibleMove>> MoveTree) {
            var ScoreForBestScorePath = MoveTree.Max(o => o.Sum(j => j.ScoreValue));
            var BestScorePath = MoveTree.First(o => o.Sum(j => j.ScoreValue) == ScoreForBestScorePath);

            var GroupByFirstMove = MoveTree.GroupBy(o => o.First()).Select(o => new { First = o.Key, Count = o.Count(), Moves = o.ToList() }).
                OrderByDescending(o => o.Count).ThenByDescending(o => o.Moves.Average(j => j.Sum(k => k.ScoreValue)));

            var MovesForBestScorePath = GroupByFirstMove.First(o => o.First.Equals(BestScorePath.First())).Count;
            var MovesForBestMovesPath = GroupByFirstMove.First().Count;

            var ScoreForBestMovesPath = GroupByFirstMove.First().Moves.Max(o => o.Sum(j => j.ScoreValue));
            var BestMovesPath = GroupByFirstMove.First().Moves.First(o => o.Sum(j => j.ScoreValue) == ScoreForBestMovesPath);

            Log.info($"Best path for score is {ScoreForBestScorePath} with {MovesForBestScorePath} paths {BestScorePath.First()}");
            foreach (var s in BestScorePath) {
                Log.debug($"Move is {s}");
            }

            Log.info($"Best path for options is {ScoreForBestMovesPath} with {MovesForBestMovesPath} paths {BestMovesPath.First()}");
            foreach (var s in BestMovesPath) {
                Log.debug($"Move is {s}");
            }

            List<PossibleMove> MyMove;

            if (p.Strategy == "moves") {
                MyMove = BestMovesPath;
            } else if (p.Strategy == "score") {
                MyMove = BestScorePath;
            } else {
                if (ScoreForBestMovesPath == ScoreForBestScorePath) {
                    MyMove = BestMovesPath;
                } else {
                    MyMove = BestScorePath;
                }
            }

            Log.important($"Strategy is {p.Strategy} so going with {MyMove.First()}");
            return MyMove;
        }

        public List<PossibleMove> AllPossibleMoves(Player p, BoardClass VirtualBoard = null) {
            Log.debug($"Enumerating moves for {p}");

            if (VirtualBoard == null) {
                VirtualBoard = Board;
                Log.debug($"Using current board data");
            } else {
                Log.debug($"Using VIRTUAL board data");
            }


            var AllMove = new List<PossibleMove>();

            var UsableCards = p.Hand.GroupBy(o => o.Type).Select(o => o.First());
            var Meeples = VirtualBoard.Meeples.Where(o => o.Player.ID == p.ID);

            if (UsableCards.Select(o => o.Type).Contains(CardType.PlaceMeeple)) {
                UsableCards = UsableCards.Where(o => o.Type == CardType.PlaceMeeple).ToList();
                Meeples = Meeples.Where(o => o.X < 0).Take(1);
            }

            if (Log.Enabled) {
                Log.debug("useable cards:");
                foreach (var c in UsableCards) {
                    Log.debug(c.ToString());
                }
                Log.debug("useable meeples:");
                foreach (var c in Meeples) {
                    Log.debug(c.ToString());
                }
            }

            foreach (var C in UsableCards) {
                foreach (var M in Meeples) {
                    var Moves = C.PossibleMoves(M.X, M.Y, VirtualBoard, p.ID);
                    if (Log.Enabled) Log.debug($"found {Moves.Count} moves for {M} using {C}");
                    AllMove.AddRange(Moves);
                }
            }

            if (p.AI) {
                foreach (var m in AllMove) {
                    if (m.Destination.Type == CardType.Stealth) m.ScoreValue += 5;
                    // if (m.Destination.Type == CardType.Ambush) m.ScoreValue -= 1;
                }
            }

            Log.debug($"found total of {AllMove.Count} moves for {p}");

            return AllMove;
        }

    }


    public class BoardClass {

        public Logger Log;
        public NinjaGame Sim;

        public int TilesX;
        public int TilesY;

        public Card[,] Tiles;

        public Dictionary<int, List<IList<WalkStep>>> OrthWalkPatterns;
        public Dictionary<int, List<IList<WalkStep>>> DiagWalkPatterns;

        public List<Player> Players;
        public List<Meeple> Meeples;

        public Card ShadowLastPlayedCard;

        public BoardClass(int width, int height, NinjaGame _Sim, Logger _Log, bool fullInit = true) {
            TilesX = width;
            TilesY = height;
            Log = _Log;
            Sim = _Sim;
            ShadowLastPlayedCard = new EmptyCard();

            if (fullInit) {
                Tiles = new Card[width, height];
                PreCalcWalkPattern();
            }

        }

        public BoardClass MakeCopy() {
            var Copy = new BoardClass(TilesX, TilesY, Sim, Log, false);

            Copy.Tiles = Tiles.Copy();
            Copy.Players = Players.Copy();
            Copy.Meeples = Meeples.Copy();
            Copy.ShadowLastPlayedCard = ShadowLastPlayedCard.Copy();

            Copy.OrthWalkPatterns = OrthWalkPatterns;
            Copy.DiagWalkPatterns = DiagWalkPatterns;


            return Copy;
        }

        public void StartChangeTracking() {




        }


        public List<Card> GetOccupiedTiles(int TeamID) {

            var ret = new List<Card>();
            foreach (var m in Meeples.Where(o => o.Player.ID == TeamID)) {
                if (InBounds(m.X, m.Y)) {
                    ret.Add(Tiles[m.X, m.Y]);
                }
            }
            return ret;
        }

        public void PlayMove(PossibleMove Move, Player p, bool VirtualMove = false) {

            Meeple TargetMeeple;

            if (Move.Card.Type == CardType.PlaceMeeple) {
                TargetMeeple = Meeples.Where(o => o.Player.ID == p.ID && o.X < 0).First();
            } else {
                TargetMeeple = GetMeepleAt(Move.FromX, Move.FromY);
            }

            // checks
            if (TargetMeeple.Player.ID != p.ID) throw new Exception("Thats not yours!");

            PickupCard(p, Move.FromX, Move.FromY);
            if (Move.Card.Type == CardType.Stealth) {
                PickupCard(p, Move.FirstX, Move.FirstY);
            }

            if (Move.isShadow) {
                DiscardCard(p, CardType.Shadow);
            } else {
                DiscardCard(p, Move.Card.Type);
            }

            if (Move.Card.Type == CardType.Ambush) {
                var Victim = GetMeepleAt(Move.ToX, Move.ToY);
                if (Victim == null) throw new Exception("Ambush has invalid target!");
                int pushx = Math.Sign(Move.ToX - Move.FromX), pushy = Math.Sign(Move.ToY - Move.FromY);
                Victim.Place(Victim.X + pushx, Victim.Y + pushy);
                if (!IsWalkable(Victim.X, Victim.Y, false)) throw new Exception("Ambush pushed to invalid tile!");
            }

            if (!Move.isShadow) {
                ShadowLastPlayedCard = Move.Card;
            }

            TargetMeeple.Place(Move.ToX, Move.ToY);
            if (Log.Enabled) {
                Sim.LastMove = Move;
                p.UpdateHand();
            }

        }

        public void PickupCard(Player p, int x, int y) {

            if (!InBounds(x, y)) {
                Log.debug($"Cannot pick card at ({x},{y})");
                return;
            }

            var PickedCard = Tiles[x, y].Copy();
            Log.info($"{p} Picked card at ({x},{y}) {PickedCard.Type}");
            p.Hand.Add(PickedCard);
            Tiles[x, y] = new EmptyCard();

            if (!PickedCard.Walkable) {
                Log.important($"Warning {p} picked unwalkable card at ({x},{y}) {PickedCard.Type}");
                throw new Exception("Picked an unwalkable card!");
            }

            if (!PickedCard.Playable) {
                Log.info($"{p} Picked unplayable card at ({x},{y}) {PickedCard.Type} - discarding");
                DiscardCard(p, PickedCard.Type);
            }

        }

        public void DiscardCard(Player p, CardType Type) {
            Log.info($"{p} Discard Card {Type}");
            var DiscType = p.Hand.Where(o => o.Type == Type).ToList();
            if (DiscType.Count == 0) throw new Exception("You dont have that card!");
            var Disc = DiscType.First();
            p.Hand.Remove(Disc);
            p.DiscardPile.Add(Disc);
        }

        public bool InBounds(int x, int y) {
            if (x >= 0 && x < TilesX && y >= 0 && y < TilesY) return true;
            return false;
        }

        public bool IsWalkable(int x, int y, bool IncludeMeelpes = true) {
            if (!InBounds(x, y)) return false;
            if (!Tiles[x, y].Walkable) return false;

            if (IncludeMeelpes) {
                foreach (var m in Meeples) {
                    if (m.X == x && m.Y == y) return false;
                }
            }

            return true;
        }

        public Meeple GetMeepleAt(int x, int y) {
            foreach (var m in Meeples) {
                if (m.X == x && m.Y == y) return m;
            }
            return null;
        }

        public void PreCalcWalkPattern() {
            OrthWalkPatterns = new Dictionary<int, List<IList<WalkStep>>>();
            DiagWalkPatterns = new Dictionary<int, List<IList<WalkStep>>>();

            for (int i = 1; i <= 3; i++) {
                OrthWalkPatterns[i] = GenerateWalkPattern(i, false);
                DiagWalkPatterns[i] = GenerateWalkPattern(i, true);
            }
        }

        public List<IList<WalkStep>> GetWalkPattern(int Steps, bool UseDiag) {

            if (UseDiag) {
                return DiagWalkPatterns[Steps];
            } else {
                return OrthWalkPatterns[Steps];
            }
        }

        public List<IList<WalkStep>> GenerateWalkPattern(int Steps, bool useDiag) {

            var Result = new List<IList<WalkStep>>();
            var PossibleSteps = new List<WalkStep>();

            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if (!useDiag) if (!(i == 0 || j == 0)) continue;
                    if (i == 0 && j == 0) continue;
                    PossibleSteps.Add(new WalkStep() { X = i, Y = j });
                }
            }

            var Paths = new Variations<WalkStep>(PossibleSteps, Steps, GenerateOption.WithRepetition).ToList();

            Log.debug($"Steps {Steps}, Diag {useDiag}, Combs {Paths.Count}");

            foreach (var path in Paths) {
                bool noBackTrack = true;
                for (int s = 0; s < path.Count - 1; s++) {
                    if (path[s].isOpposite(path[s + 1])) {
                        noBackTrack = false;
                        break;
                    }
                }
                if (path.Sum(o => o.X) == 0 && path.Sum(o => o.Y) == 0) noBackTrack = false;
                if (noBackTrack) Result.Add(path);
            }

            Log.debug($"Steps {Steps}, Diag {useDiag}, Reduced Combs {Result.Count}");

            return Result;

        }

    }

    public class Player {
        public int ID;
        public Color Color;
        public string Name;
        public List<Card> Hand;
        public List<Card> DiscardPile;
        public bool AI;
        public bool OutOfMoves;
        public int LookAhead;
        public string Strategy;

        public Player() {
            Hand = new List<Card>();
            DiscardPile = new List<Card>();
            OutOfMoves = false;
            Strategy = "moves";
            LookAhead = 2;
        }

        public void UpdateHand() {
            ListBox T = new ListBox();

            try {
                if (ID == 0) T = Application.OpenForms.OfType<Form1>().First().listBox1;
                if (ID == 1) T = Application.OpenForms.OfType<Form1>().First().listBox2;
                if (ID == 2) T = Application.OpenForms.OfType<Form1>().First().listBox3;
                if (ID == 3) T = Application.OpenForms.OfType<Form1>().First().listBox4;
            } catch {
                return;
            }

            T.Items.Clear();

            foreach (var c in Hand.OrderBy(o => o.ScoreValue.ToString())) {
                T.Items.Add(c);
            }

            T.Items.Add("");

            foreach (var c in DiscardPile.OrderBy(o => o.ToString())) {
                T.Items.Add("(discard) " + c.ToString());
            }

        }

        public int Score(BoardClass Board) {
            int Score = 0;
            foreach (var c in Hand) {
                Score += c.ScoreValue;
            }
            foreach (var c in DiscardPile) {
                Score += c.ScoreValue;
            }
            foreach (var c in Board.GetOccupiedTiles(ID)) {
                Score += c.ScoreValue;
            }
            return Score;
        }


        public override string ToString() {
            return $"{Name} Player";
        }

    }


    public class Meeple {
        public int X;
        public int Y;
        public Player Player;

        public Meeple() {
            Place(-1, -1);
        }

        public void Place(int x, int y) {
            X = x;
            Y = y;
        }

        public override string ToString() {
            return $"{Player.Name}'s Meeple at ({X},{Y})";
        }

    }
}
