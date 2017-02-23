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

namespace TurnSim {
    public static class Helpers {

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

        public static double Dist(double X1, double Y1, double X2, double Y2) {
            return Math.Pow(Math.Pow(X1 - X2, 2) + Math.Pow(Y1 - Y2, 2), .5);
        }

        public static double Dist(UnitClass unit, UnitClass enemy) {
            return Dist(unit.X, unit.Y, enemy.X, enemy.Y);
        }

        public static double Dist(UnitClass unit, dblPoint enemy) {
            return Dist(unit.X, unit.Y, enemy.X, enemy.Y);
        }
        public static double Dist(dblPoint unit, dblPoint enemy) {
            return Dist(unit.X, unit.Y, enemy.X, enemy.Y);
        }
        public static double Dist(UnitClass unit, double X2, double Y2) {
            return Dist(unit.X, unit.Y, X2, Y2);
        }
        public static double Dist(dblPoint unit, double X2, double Y2) {
            return Dist(unit.X, unit.Y, X2, Y2);
        }

    }

    public class Logger {

        public int UILevel = 0;
        public int FileLevel = 0;
        public string LogFileName;

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

    public struct dblPoint {
        public double X;
        public double Y;
        public bool isValid;
    }

    public class Visualiser {

        PictureBox RenderTarget;

        double PxPerMeter = 0;
        double DrawRadius = .4;
        SimSession Sim;
        Graphics g;

        public bool Enabled;

        double TargetFPS = 99999;
        Stopwatch FrameTimer;
        StringFormat stringFormat;

        Dictionary<string, Image> Pics;
        bool UsePics = false;

        public Visualiser(SimSession _Sim, PictureBox _RenderTarget, double xMeters = 40) {

            Enabled = _RenderTarget != null;
            if (!Enabled) return;

            Sim = _Sim;
            RenderTarget = _RenderTarget;
            PxPerMeter = RenderTarget.ClientSize.Width / xMeters;
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
        public PointF Point(dblPoint P, bool scale = true) {
            return Point(P.X, P.Y, scale);
        }
        public PointF Point(double X, double Y, bool scale = true) {
            var sf = scale ? PxPerMeter : 1;
            return new PointF() { X = (float)(X * sf), Y = (float)(Y * sf) };
        }

        public Rectangle Rect(double x, double y, double w, double h, bool scale = true) {
            var sf = scale ? PxPerMeter : 1;
            return new Rectangle((int)(x * sf), (int)(y * sf), (int)(w * sf), (int)(h * sf));
        }

        public void RenderAttack(UnitClass unit, UnitClass def, double Damage) {
            if (!Enabled) return;

            string msg = "Miss!";
            if (Damage > 0) msg = "Ouch! " + ((int)Damage).ToString() + " damage";

            g.DrawString("Attack!", new Font("Arial", 16), Brushes.Black, Point(unit.X, unit.Y - DrawRadius * 2), stringFormat);
            g.DrawString(msg, new Font("Arial", 16), Brushes.Black, Point(def.X, def.Y - DrawRadius * 2), stringFormat);

            Refresh();
        }

        public void RenderTeamHealth() {
            foreach (var team in Sim.AIPlayers) {
                g.FillRectangle(team.Colour, Rect(0, RenderTarget.ClientSize.Height - 20 * team.TeamID - 20, RenderTarget.ClientSize.Width * team.TeamHP(), 15, false));
            }
        }

        public void RenderUnit(UnitClass unit, bool BlankOld = true) {
            if (!Enabled) return;
            if (!unit.Alive) return;

            if (BlankOld) {
                g.FillRectangle(Brushes.White, Rect(unit.OldX - Sim.SimRules.MinUnitDistance - .1, unit.OldY - Sim.SimRules.MinUnitDistance - .4,
                    Sim.SimRules.MinUnitDistance * 2 + .2, Sim.SimRules.MinUnitDistance * 2 + .8));
            }

            unit.OldX = unit.X;
            unit.OldY = unit.Y;

            var BodyRect = Rect(unit.X - DrawRadius, unit.Y - DrawRadius, DrawRadius * 2, DrawRadius * 2);

            if (UsePics) {
                g.FillRectangle(unit.Team.Colour, BodyRect);
                g.DrawImage(Pics[unit.BaseStats.Class.ToLower()], BodyRect);
            } else {
                if (unit.BaseStats.Class == "Leader") {
                    g.FillRectangle(unit.Team.Colour, BodyRect);
                } else if (unit.BaseStats.Class == "Hero") {
                    g.FillPie(unit.Team.Colour, BodyRect, 210, 120);
                    g.FillPie(unit.Team.Colour, BodyRect, 30, 120);
                } else {
                    g.FillEllipse(unit.Team.Colour, BodyRect);
                }
            }

            var ExcRec = Rect(unit.X - Sim.SimRules.MinUnitDistance, unit.Y - Sim.SimRules.MinUnitDistance, Sim.SimRules.MinUnitDistance * 2, Sim.SimRules.MinUnitDistance * 2);
            // g.DrawEllipse(Pens.Black, ExcRec);


            if (!unit.HadTurn) {
                Dot(unit.X, unit.Y - DrawRadius * 2, 0.3, Brushes.LightGreen, false);
            }

            g.DrawString(unit.ID.ToString(), new Font("Arial", 12), Brushes.Black, Point(unit.X, unit.Y - DrawRadius * 2), stringFormat);

            var HPbar = Rect(unit.X - DrawRadius * 1.5, unit.Y + DrawRadius * 1.5, DrawRadius * 3 * unit.HP / unit.MaxHP, DrawRadius / 2);
            var HPred = Rect(HPbar.X, HPbar.Y, DrawRadius * 3 * PxPerMeter, HPbar.Height, false);


            g.FillRectangle(Brushes.Red, HPred);
            g.FillRectangle(Brushes.Green, HPbar);


            if (BlankOld) {
                Refresh();
            }

        }


        public void RenderAll() {
            if (!Enabled) return;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            g.FillRectangle(Brushes.White, new Rectangle(0, 0, RenderTarget.ClientSize.Width, RenderTarget.ClientSize.Height));

            foreach (var unit in Sim.Units) {
                RenderUnit(unit, false);
            }

            RenderTeamHealth();
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
