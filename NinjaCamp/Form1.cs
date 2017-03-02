
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace NinjaCamp {
    public partial class Form1 : Form {

        public NinjaGame Sim;
        public Card SelectedCard;
        public Meeple SelectedMeeple;

        public int StealthX = -1;
        public int StealthY = -1;

        public Form1() {
            InitializeComponent();
            Sim = new NinjaGame(RenderBox, txtLog, "ouput.txt", 1);

            //foreach (var i in Enum.GetValues(typeof(CardType))) {
            //    comboBox1.Items.Add(((int)i).ToString() + ". " + i.ToString());
            //}

        }

        private void button1_Click(object sender, EventArgs e) {
            Sim.SimSetup();
            Sim.Vis.RenderAll();
            Sim.StartMatch();
        }

        private void ToggleVis_Click(object sender, EventArgs e) {
            Sim.Vis.Enabled = !Sim.Vis.Enabled;
        }

        private void RenderBox_MouseDown(object sender, MouseEventArgs e) {

            int TileX = (int)Math.Floor(e.X / Sim.Vis.CWid);
            int TileY = (int)Math.Floor(e.Y / Sim.Vis.CHit);

            if (e.Button == MouseButtons.Right) {
                StealthX = TileX;
                StealthY = TileY;
                lblInstruction.Text = "OK! Now Click destination again";
            }

            if (e.Button == MouseButtons.Left) {

                if (SelectedMeeple == null) {
                    SelectedMeeple = Sim.Board.GetMeepleAt(TileX, TileY);
                    if (SelectedMeeple == null) return;

                    if (SelectedMeeple.Player.ID != Sim.CurrentPlayer) {
                        SelectedMeeple = null;
                        lblInstruction.Text = "Thats not yours!";
                        return;
                    }
                    StealthX = -1;
                    StealthY = -1;
                    lblInstruction.Text = "OK! Now pick a destination!";

                    if (SelectedCard != null) {
                        var Moves = SelectedCard.PossibleMoves(TileX, TileY, Sim.Board, Sim.CurrentPlayer);
                        foreach (var m in Moves) {
                            Sim.Vis.MarkCard(m.ToX, m.ToY, .1, null, false);
                        }
                        Sim.Vis.Refresh();
                    }

                } else {

                    if (SelectedCard == null) {
                        lblInstruction.Text = "No card selected!";
                        return;
                    }
                    if (SelectedCard.Type == CardType.Stealth && StealthY < 0) {
                        lblInstruction.Text = "Right click a tile to Stealth over! Then Click destination again";
                        return;
                    }

                    var PossibleMoves = Sim.AllPossibleMoves(Sim.Board.Players[Sim.CurrentPlayer], Sim.Board);

                    var MoveType = SelectedCard;
                    var Score = Sim.Board.Tiles[TileX, TileY].ScoreValue;

                    if (SelectedCard.Type == CardType.Shadow) {
                        MoveType = Sim.Board.ShadowLastPlayedCard;
                    }
                    if (SelectedCard.Type == CardType.Stealth) {
                        Score += Sim.Board.Tiles[StealthX, StealthY].ScoreValue;
                    }


                    var ProposedMove = new PossibleMove() {
                        FromX = SelectedMeeple.X,
                        FromY = SelectedMeeple.Y,
                        ToX = TileX,
                        ToY = TileY,
                        Card = MoveType,
                        Destination = Sim.Board.Tiles[TileX, TileY],
                        isShadow = SelectedCard.Type == CardType.Shadow,
                        FirstX = StealthX,
                        FirstY = StealthY,
                        ScoreValue = Score
                    };

                    if (PossibleMoves.Contains(ProposedMove)) {
                        Sim.Board.PlayMove(ProposedMove, Sim.Board.Players[Sim.CurrentPlayer]);
                        Sim.WaitForPlayer = false;
                    } else {
                        lblInstruction.Text = "Invalid move!";
                        return;
                    }

                }

            }

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            ChooseCard(listBox1, 0);
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e) {
            ChooseCard(listBox2, 1);
        }


        private void listBox3_SelectedIndexChanged(object sender, EventArgs e) {
            ChooseCard(listBox3, 2);
        }

        private void listBox4_SelectedIndexChanged(object sender, EventArgs e) {
            ChooseCard(listBox4, 3);
        }

        public void ChooseCard(ListBox LB, int index) {

            if (Sim.CurrentPlayer != index) {
                lblInstruction.Text = "Not your cards! use other box!";
                return;
            }

            Card ChosenCard;
            try { ChosenCard = (Card)LB.SelectedItem; } catch { return; }
            if (ChosenCard == null) return;

            if (Sim.Board.Players[Sim.CurrentPlayer].Hand.Count(o => o.Type == CardType.PlaceMeeple) > 0) {
                if (ChosenCard.Type != CardType.PlaceMeeple) {
                    lblInstruction.Text = "You Must choose PlaceMeeple at the start!";
                    return;
                }
            }

            SelectedCard = ChosenCard;
            StealthX = -1;
            StealthY = -1;
            if (ChosenCard.Type == CardType.PlaceMeeple) {
                lblInstruction.Text = "Card selected: " + ChosenCard.ToString() + ", now pick a destination!";
                SelectedMeeple = Sim.Board.Meeples.First(o => o.X < 0 && o.Player.ID == Sim.CurrentPlayer);
            } else {
                lblInstruction.Text = "Card selected: " + ChosenCard.ToString() + ", now pick a meeple!";
            }


        }

        private void button2_Click(object sender, EventArgs e) {
            if (Sim.WaitForPlayer) {
                lblInstruction.Text = "You are out!";
                Sim.Board.Players[Sim.CurrentPlayer].OutOfMoves = true;
                Sim.WaitForPlayer = false;
            }
        }

    }
}
