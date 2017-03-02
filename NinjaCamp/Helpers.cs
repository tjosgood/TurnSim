using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;

namespace NinjaCamp {

    public struct dblPoint {
        public double X;
        public double Y;
        public bool isValid;
    }

    public class MoveTree {

        public List<MoveTree> ChildMoves;
        public bool Finished;
        public int Score;
        public int Depth;
        public int ChildCount;
    }

    public class PossibleMove {
        public int FromX;
        public int FromY;
        public int ToX;
        public int ToY;
        public int FirstX;
        public int FirstY;
        public int ScoreValue { get; set; }
        public Card Card;
        public bool isShadow;
        public Card Destination;

        public PossibleMove() {
            FirstX = -1;
            FirstY = -1;
            isShadow = false;
        }

        public override string ToString() {
            return $"{Card.Type}{(isShadow ? " (Shadow)" : "")}: From ({FromX}, {FromY}) To ({ToX}, {ToY}) onto {Destination}, Score {ScoreValue}";
        }
        public override bool Equals(object obj) {
            var item = obj as PossibleMove;
            if (item == null) return false;
            return (FromX == item.FromX && FromY == item.FromY && ToX == item.ToX && ToY == item.ToY && FirstX == item.FirstX && FirstY == item.FirstY
              && Card.Type == item.Card.Type && isShadow == item.isShadow);
        }

        public override int GetHashCode() {
            int hash = 13;
            hash = (hash * 7) + FromX;
            hash = (hash * 7) + FromY;
            hash = (hash * 7) + FirstX;
            hash = (hash * 7) + FirstY;
            hash = (hash * 7) + ToX;
            hash = (hash * 7) + ToY;
            hash = (hash * 7) + (isShadow ? 1 : 0);
            hash = (hash * 7) + (int)Card.Type;
            return hash;
        }

    }

    public class WalkStep {
        public int X;
        public int Y;

        public override string ToString() {
            return $"X: {X}, Y: {Y}";
        }

        public bool isOpposite(WalkStep obj) {
            if ((obj.X + X) == 0 && (obj.Y + Y) == 0) return true;
            return false;
        }

    }

    static class ListExtension {
        public static T Pop<T>(this List<T> list, int index = -1) {
            if (index == -1) index = list.Count - 1;
            T r = list[index];
            list.RemoveAt(index);
            return r;
        }


    }
    public static class Helpers {

        public static List<List<T>> GetAllCombos<T>(List<T> list) {
            List<List<T>> result = new List<List<T>>();
            // head
            result.Add(new List<T>());
            result.Last().Add(list[0]);
            if (list.Count == 1)
                return result;
            // tail
            List<List<T>> tailCombos = GetAllCombos(list.Skip(1).ToList());
            tailCombos.ForEach(combo => {
                result.Add(new List<T>(combo));
                combo.Add(list[0]);
                result.Add(new List<T>(combo));
            });
            return result;
        }

        public static void Shuffle<T>(this IList<T> list, Random rnd = null) {
            if (rnd == null) rnd = new Random();
            for (var i = 0; i < list.Count - 1; i++)
                list.Swap(i, rnd.Next(i, list.Count));
        }

        public static void Swap<T>(this IList<T> list, int i, int j) {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        public static Card CardFactory(CardType T) {
            if (T == CardType.Ambush) return new AmbushCard();
            if (T == CardType.Dodge) return new DodgeCard();
            if (T == CardType.Empty) return new EmptyCard();
            if (T == CardType.Evade) return new EvadeCard();
            if (T == CardType.EvadeWood) return new EvadeWoodCard();
            if (T == CardType.Leap) return new LeapCard();
            if (T == CardType.PlaceMeeple) return new PlaceMeepleCard();
            if (T == CardType.Shadow) return new ShadowCard();
            if (T == CardType.Sprint) return new SprintCard();
            if (T == CardType.SprintWood) return new SprintWoodCard();
            if (T == CardType.Stealth) return new StealthCard();
            if (T == CardType.Trap) return new TrapCard();
            if (T == CardType.Wall) return new WallCard();
            return null;
        }

    }

    public class Logger {

        public int UILevel = 0;
        public int FileLevel = 0;
        public string LogFileName;
        public bool Enabled = true;

        TextBox txtUI;
        System.IO.StreamWriter LogFile;
        string[] LevelNames = new string[] { "Debug", "Info", "Important" };

        public Logger(TextBox _txtUI = null, string _LogFileName = null, int _UILevel = 0, int _FileLevel = 0) {
            txtUI = _txtUI;
            LogFileName = _LogFileName;
            UILevel = _UILevel;
            FileLevel = _FileLevel;

            if (txtUI == null) UILevel = 10;
            if (!String.IsNullOrWhiteSpace(LogFileName)) {
                LogFile = new System.IO.StreamWriter(LogFileName);
            } else {
                FileLevel = 10;
            }

        }

        private void Log(string s, bool el = false, int level = 0) {
            if (!Enabled) return;
            s = string.Format("{0}{1}: {2} - {3}", el ? Environment.NewLine : "", DateTime.Now.ToLongTimeString(), GetLvlName(level), s);
            Debug.WriteLine(s);

            if (level >= UILevel) {
                txtUI.AppendText(s + Environment.NewLine);
            }
            if (level >= FileLevel) {
                LogFile.WriteLine(s);
            }
        }

        public void debug(string s, bool nl = false) {
            Log(s, nl, 0);
        }

        public void info(string s, bool nl = false) {
            Log(s, nl, 1);
        }

        public void important(string s, bool nl = false) {
            Log(s, nl, 2);
        }


        public string GetLvlName(int level) {
            try {
                return LevelNames[level];
            } catch {
                return "INVALID";
            }

        }

    }



    public class Visualiser {

        PictureBox RenderTarget;

        public double CWid = 0;
        public double CHit = 0;
        double Spacing = 5;
        NinjaGame Sim;
        Graphics g;

        public bool Enabled;

        double TargetFPS = 99999;
        Stopwatch FrameTimer;
        StringFormat stringFormat;

        Dictionary<string, Image> Pics;
        bool UsePics = true;

        public Visualiser(NinjaGame _Sim, PictureBox _RenderTarget) {

            Enabled = _RenderTarget != null;
            if (!Enabled) return;

            Sim = _Sim;
            RenderTarget = _RenderTarget;

            CHit = RenderTarget.ClientSize.Height / Sim.Board.TilesY;
            CWid = CHit * .75;

            if (RenderTarget.Image == null) {
                RenderTarget.Image = new Bitmap(RenderTarget.ClientSize.Width, RenderTarget.ClientSize.Height);
            }

            g = Graphics.FromImage(RenderTarget.Image);

            stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            Pics = new Dictionary<string, Image>();

            try {
                foreach (var f in Directory.GetFiles("img")) {
                    Pics[Path.GetFileNameWithoutExtension(f).ToLower()] = Image.FromFile(f);
                }
            } catch { }

            if (Pics.Count == 0) {
                UsePics = false;
            }

            FrameTimer = new Stopwatch();
            FrameTimer.Start();
        }

        public void Dot(double X, double Y, double r = .1, Brush col = null, bool redraw = true) {
            col = col ?? Brushes.Green;
            g.FillEllipse(col, Rect(X - r, Y - r, r * 2, r * 2));
            if (redraw) {
                Refresh(false);
            }

        }

        public void MarkCard(int X, int Y, double r = .1, Brush col = null, bool redraw = true) {
            Dot(X + .5, Y + .5, r, col, redraw);
        }

        public PointF Point(dblPoint P, bool scale = true) {
            return Point(P.X, P.Y, scale);
        }
        public PointF Point(double X, double Y, bool scale = true) {
            var sfx = scale ? CWid : 1;
            var sfy = scale ? CHit : 1;
            return new PointF() { X = (float)(X * sfx), Y = (float)(Y * sfy) };
        }

        public Rectangle Rect(double x, double y, double w, double h, bool scale = true) {
            var sfx = scale ? CWid : 1;
            var sfy = scale ? CHit : 1;
            return new Rectangle((int)(x * sfx), (int)(y * sfy), (int)(w * sfx), (int)(h * sfy));
        }

        public Rectangle CardOutline(int x, int y) {
            return Rect(CWid * x + Spacing, CHit * y + Spacing, CWid - Spacing * 2, CHit - Spacing * 2, false);
        }


        public void RenderCard(int x, int y, bool BlankOld = true) {
            if (!Enabled) return;

            if (BlankOld) {
                g.FillRectangle(Brushes.White, Rect(CWid * x, CHit * y, CWid, CHit, false));
            }

            if (Sim.Board.Tiles[x, y].Type == CardType.Empty) {
                if (BlankOld) Refresh();
                return;
            }

            var Outline = CardOutline(x, y);
            var Header = new Rectangle(Outline.X, Outline.Y, Outline.Width, (int)(CHit / 5));

            if (UsePics) {
                g.DrawImage(Pics[Sim.Board.Tiles[x, y].ToString().ToLower()], Outline);
                g.DrawRectangle(Pens.Black, Outline);
            } else {
                if (Sim.Board.Tiles[x, y].BGColor != Color.White) {
                    g.FillRectangle(new SolidBrush(Sim.Board.Tiles[x, y].BGColor), Outline);
                }

                g.DrawRectangle(Pens.Black, Outline);
                g.FillRectangle(new SolidBrush(Sim.Board.Tiles[x, y].Color), Header);
                g.DrawRectangle(Pens.Black, Header);

                var textCol = Brushes.Black;
                if (Sim.Board.Tiles[x, y].Color == Color.Black) textCol = Brushes.White;

                g.DrawString(Sim.Board.Tiles[x, y].ToString(), new Font("Arial", 12), textCol, Point(Header.X + Header.Width / 2, Header.Y + Header.Height / 2, false), stringFormat);
            }
            

            g.DrawString($"({x},{y})", new Font("Arial", 8), UsePics ? Brushes.White:  Brushes.Black, Point(Header.X + Header.Width / 2, Outline.Y + Outline.Height - 10, false), stringFormat);

            if (BlankOld) Refresh();

        }

        public void RenderMeeple(Meeple m, bool BlankOld = true) {
            if (!Enabled) return;
            if (BlankOld) {
                MarkCard(m.X, m.Y, .3, Brushes.White, false);
            }

            MarkCard(m.X, m.Y, .2, new SolidBrush(m.Player.Color), false);

            if (BlankOld) Refresh();
        }

        public void RenderMove(PossibleMove M) {
            var mep = Sim.Board.GetMeepleAt(M.ToX, M.ToY);
            var col = mep == null ? Pens.Green : new Pen(mep.Player.Color);

            g.DrawEllipse(col, Rect(M.FromX + .5 - .1, M.FromY + .5 - .1, .2, .2));
            g.DrawLine(col, Point(M.FromX + .5 - .1, M.FromY + .5), Point(M.ToX + .5, M.ToY + .5));
            g.DrawLine(col, Point(M.FromX + .5 + .1, M.FromY + .5), Point(M.ToX + .5, M.ToY + .5));
            g.DrawLine(col, Point(M.FromX + .5, M.FromY + .5 + .1), Point(M.ToX + .5, M.ToY + .5));
            g.DrawLine(col, Point(M.FromX + .5, M.FromY + .5 - .1), Point(M.ToX + .5, M.ToY + .5));
        }

        public void RenderAll() {
            if (!Enabled) return;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            g.FillRectangle(Brushes.White, new Rectangle(0, 0, RenderTarget.ClientSize.Width, RenderTarget.ClientSize.Height));

            for (int x = 0; x < Sim.Board.TilesX; x++) {
                for (int y = 0; y < Sim.Board.TilesY; y++) {
                    RenderCard(x, y, false);
                }
            }

            foreach (var m in Sim.Board.Meeples) {
                RenderMeeple(m, false);
            }

            if (Sim.LastMove != null) {
                RenderMove(Sim.LastMove);
            }

            Refresh();
            Application.DoEvents();
        }

        public void Refresh(bool wait = true) {
            RenderTarget.Refresh();
            var Passed = FrameTimer.ElapsedMilliseconds;
            var waitms = (1000.0 / TargetFPS) - Passed;
            if (waitms > 0 && wait) {
                Thread.Sleep((int)waitms);
            }

            FrameTimer.Restart();
        }

    }

}
