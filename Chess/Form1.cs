using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets; //Chess alkalmazás én vagyok a kliens program!
namespace Chess
{
    public partial class Form1 : Form
    {
        List<Panel> Panel_mezők = new List<Panel>(); //a sakktábla 64 mezője
        List<Color> szinek_miattseged = new List<Color>(); //a sakktábla 64 mezőjének színkódjai
        private string[,] Array_ofPieces = new string[8, 8];  //a bábuk helye egy mátrixban
        private ArrayList Összes_lépés = new ArrayList();     // a meglépett lépéseket tartalmazó lista
        string abc = "abcdefgh";
        int Mező_firstClicked = 1;
        Panel Innen_lepEL = new Panel();       //panel változó amely az első kattintás helyén található figurai tulajdonságaival rendkezik
        private char Whosturn = 'w';           // éppen ki van lépésen
        private string Special_moveOccured = ""; // anpasszent megtörténhet
        private string white_kiraly = "e1";     // világos király poziciója a táblán
        private string black_kiraly = "e8";     //sötét király -||-
        private bool IsSakkot = false;            // sakkot kapott a király és ki kell lépni belőle
        private string sanc_rovid = "";
        private string sanc_hosszu = "";
        Dictionary<char, string> dict = new Dictionary<char, string> { { 'g', "gyalog" }, { 'b', "bastya" }, { 'h', "huszar" }, { 'f', "futo" }, { 'v', "vezer" }, { 'k', "kiraly" } };
        private int white_time = 900;
        private int black_time = 900;
        Thread szal; //egy uj thread létrwehozása mert az kezeli az órát!
        private bool isload_enabled = true;
        private bool stop_clock= false;

        List<string> send_tolan = new List<string>();
        private char Typeofconnection = 'o'; //o=offline l=lan
        private bool Isit_myturn = true;
        public Form1()
        {
            InitializeComponent();
        }
        public void Perfrom_endgame()
        {
            int checker = Játék_vége(Whosturn);
            if (checker != 0)
            {
                stop_clock = true;
                if (checker == 2)
                {
                    string winner = "Világos";
                    if (Whosturn == 'w') { winner = "Sötét"; }
                    MessageBox.Show("Játék vége, " + winner + " nyert");
                }
                else if (checker == 1) { MessageBox.Show("Játék vége, Döntetlen lett"); }
                Mező_firstClicked = -2; //-2 azt jelenti hogy játék vége                                     
            }
        }
        public int Játék_vége(char szin)//vmelyik fél mattot kapott vagy pattot
        {                     
            foreach (var item in Array_ofPieces)
            {
                if (item.Length > 2 && item[2] == szin)
                {
                    ArrayList ar = Final_validation(item, true);

                    Kilép_sakkbol(ar, item);
                    if (ar.Count != 0) { return 0; }
                }
            }
            Sakkot_kapott(szin, "asd");
            if (IsSakkot == true) { return 2; }
            else return 1;                                 
        }
        private void Kilép_sakkbol(ArrayList Színezni_ki, string position)
        {
            for (int i = Színezni_ki.Count - 1; i >= 0; i--)
            {
                string tmp = Real_Mező_tulajdonság(Színezni_ki[i].ToString()); //ahova lépni kíván mező tul.mentése
                Change_arrayOf_Peaces(Színezni_ki[i] + position.Substring(2, 2));  //ahova lépni kiván felülírása
                Change_arrayOf_Peaces(position.Substring(0, 2));                   //ahonnan ellép felülírása
                if (position[3] == 'k') { Sakkot_kapott(Whosturn, Színezni_ki[i] + position.Substring(2, 2), true); }
                else { Sakkot_kapott(Whosturn, "asd"); }
                if (IsSakkot == true) { Színezni_ki.Remove(Színezni_ki[i]); }
                Change_arrayOf_Peaces(tmp);        //ahova lépni kíván vissza állítása
                Change_arrayOf_Peaces(position);   //ahonnan ellép   vissza állítása             
            }
        }
        private void Sakkot_kapott(char szin, string new_position, bool ISchange_Enabled = false)//megvizsgálja hogy a király sakkban áll e
        {
            string item;
            if (szin == 'w')
            {
                item = white_kiraly;
            }
            else { item = black_kiraly; }
            if (ISchange_Enabled == true)
            { item = new_position.Substring(0, 2); }
            IsSakkot = Do_theBigLoop(item, szin);
        }
        private void Change_turn()
        {
            if (Whosturn == 'w') { Whosturn = 'b'; }
            else { Whosturn = 'w'; }
        }
        private void Button_mező_clicked(object sender, EventArgs e)
        {
            if (Isit_myturn == true)
            {
                Panel p = (Panel)sender;
                string position = p.Tag.ToString();
                if (Mező_firstClicked == 1 && position.Length == 4 && Whosturn == position[2])
                {
                    ArrayList Színezni_ki = Final_validation(position, true);
                    Kilép_sakkbol(Színezni_ki, position);
                    if (Színezni_ki.Count != 0)
                    {
                        foreach (string item in Színezni_ki)
                        {
                            foreach (var panels in Panel_mezők)
                            {
                                if (panels.Tag.ToString().Substring(0, 2) == item)
                                {
                                    panels.BackColor = Color.Khaki;
                                }
                            }
                        }

                        Mező_firstClicked = 0;
                        Innen_lepEL = p;
                        IsSakkot = false;
                        send_tolan.Add(p.Name);
                    }
                }
                else if (Mező_firstClicked == 0)
                {
                    if (p.BackColor == Color.Khaki)
                    {
                        p.BackgroundImage = Innen_lepEL.BackgroundImage;
                        Innen_lepEL.BackgroundImage = null;

                        if (Special_moveOccured != "" && p.Tag.ToString() == Special_moveOccured.Substring(0, 2))
                        {
                            Összes_lépés.Add(Innen_lepEL.Tag.ToString() + p.Tag.ToString() + "/" + Special_moveOccured.Substring(2, 2));
                            Change_arrayOf_Peaces(Special_moveOccured.Substring(2, 2));
                            Panel tmp_panel = Change_panel(Special_moveOccured.Substring(2, 2), false);
                            tmp_panel.BackgroundImage = null;
                            Special_moveOccured = "";
                        }
                        else if (sanc_rovid == position)
                        {
                            Sáncoláshoz_bábucsere(position, "h", "f");
                        }
                        else if (sanc_hosszu == position)
                        {
                            Sáncoláshoz_bábucsere(position, "a", "d");
                        }
                        else
                        { Összes_lépés.Add(Innen_lepEL.Tag.ToString() + p.Tag.ToString()); }
                        p.Tag = p.Tag.ToString().Substring(0, 2) + Innen_lepEL.Tag.ToString().Substring(2, 2);
                        Innen_lepEL.Tag = Innen_lepEL.Tag.ToString().Substring(0, 2);
                        Change_arrayOf_Peaces(Innen_lepEL.Tag.ToString());
                        Change_arrayOf_Peaces(p.Tag.ToString());
                        Mező_firstClicked = 1;
                        for (int i = 0; i < 64; i++)
                        {
                            Panel_mezők[i].BackColor = szinek_miattseged[i];
                        }
                        string new_place = p.Tag.ToString();
                        if ((new_place[1] == '8' && new_place[2] == 'w' && new_place[3] == 'g') || (new_place[1] == '1' && new_place[2] == 'b' && new_place[3] == 'g'))
                        {
                            Change_page(panel_gyalogátváltozás, Panel_mezők, true);
                            Special_moveOccured = new_place[2].ToString();
                            Innen_lepEL = p;
                            Mező_firstClicked = -1;
                            Összes_lépés[Összes_lépés.Count - 1] = Összes_lépés[Összes_lépés.Count - 1] + "!";
                        }
                        if (new_place.Substring(2, 2) == "wk") { white_kiraly = new_place.Substring(0, 2); }
                        else if (new_place.Substring(2, 2) == "bk") { black_kiraly = new_place.Substring(0, 2); }
                        Change_turn();
                        Perfrom_endgame();
                        sanc_rovid = "";
                        sanc_hosszu = "";
                        send_tolan.Add(p.Name);
                        System.Diagnostics.Debug.WriteLine("called_main");                   
                        if (send_tolan.Count == 2 && Typeofconnection == 'l' )
                        {
                            if (e.GetType().ToString() == "System.EventArgs")
                            { send_tolan.Clear(); }
                            else { Isit_myturn = false; Send_lepes(); }                         
                        }
                    }
                }
            }
        }
        public void Sáncoláshoz_bábucsere(string position, string bastya_innen, string bastya_ide) //bastya_innen=h bastya ide=f
        {
            Panel tmp_panel = new Panel();
            Összes_lépés.Add(Innen_lepEL.Tag.ToString() + position + "0" + bastya_ide + position[1] + bastya_innen + position[1]);//Sáncolás_átalános jele: 0
            Change_arrayOf_Peaces(bastya_innen + position[1]);
            Change_arrayOf_Peaces(bastya_ide + Innen_lepEL.Tag.ToString().Substring(1, 2) + "b");
            tmp_panel = Change_panel(bastya_ide + Innen_lepEL.Tag.ToString().Substring(1, 2) + "b");
            tmp_panel.BackgroundImage = Change_panel("h" + position[1], false).BackgroundImage;
            tmp_panel = Change_panel(bastya_innen + position[1]);
            tmp_panel.BackgroundImage = null;
        }
        public Panel Change_panel(string position, bool Change_tag = true)
        {
            foreach (var item in Panel_mezők)
            {
                if (item.Tag.ToString().Substring(0, 2) == position.Substring(0, 2))
                {
                    if (Change_tag == true)
                    { item.Tag = position; }
                    return item;
                }
            }
            Panel p = new Panel();
            return p;
        }
        public void Change_page(Panel x, List<Panel> y, bool xx)
        {
            if (xx == true)
            {
                foreach (var item in y)
                {
                    item.SendToBack();
                }
                x.BringToFront();
            }
            else
            {
                foreach (var item2 in y)
                {
                    item2.BringToFront();                   
                }
                x.SendToBack();
            }
        }
        public void Change_arrayOf_Peaces(string position)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (Array_ofPieces[i, j].Substring(0, 2) == position.Substring(0, 2))
                    {
                        Array_ofPieces[i, j] = position;
                    }
                }
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //alapértelmezés és lista feltöltése           
            string special_pieces = "bhfvkfhb";  //special pieces et. except pawn
            for (int i = 0; i < abc.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (i == 0)
                    {
                        { Array_ofPieces[i, j] = abc[j] + (i + 1).ToString() + "w" + special_pieces[j]; }
                    }
                    else if (i == 1)
                    { Array_ofPieces[i, j] = abc[j] + (i + 1).ToString() + "wg"; }
                    else if (i == 6)
                    { Array_ofPieces[i, j] = abc[j] + (i + 1).ToString() + "bg"; }
                    else if (i == 7)
                    { Array_ofPieces[i, j] = abc[j] + (i + 1).ToString() + "b" + special_pieces[j]; }
                    else
                    { Array_ofPieces[i, j] = abc[j] + (i + 1).ToString(); }
                }
            }
            List<Panel> Panel_mezőktmp = this.Controls.OfType<Panel>().ToList();
            foreach (var item in Panel_mezőktmp)
            {
                if (item.Tag != null) { Panel_mezők.Add(item); }
            }
            for (int i = 0; i < 64; i++)
            {
                szinek_miattseged.Add(Panel_mezők[i].BackColor);
            }
            this.Size = new Size(1100, 770);           
        }
        public ArrayList Validation_forGyalog(string position, bool All_partIsneeded = true)
        {          
            char betu = position[0];
            int szam = Convert.ToInt16(position[1].ToString());
            char szin = position[2];
            ArrayList Ide_léphet = new ArrayList();
            int utes = 0;
            int fel_vagyle;
            int fel_vagyle_utes;
            if (szin == 'w')
            {
                fel_vagyle = 5;
                fel_vagyle_utes = 1;              
            }
            else { fel_vagyle = 6; fel_vagyle_utes = 3; }
            if (All_partIsneeded == true)
            {
                if ((szam == 2 && szin == 'w') || (szam == 7 && szin == 'b'))
                {
                    string x = Egyet_lép(position, fel_vagyle, ref utes);
                    if(Real_Mező_tulajdonság(x).Length==2)
                    {
                        System.Diagnostics.Debug.WriteLine("calledgsfs");
                        Ide_léphet.Add(x);
                        x = Egyet_lép(x + szin, fel_vagyle, ref utes);
                        if (Real_Mező_tulajdonság(x).Length == 2) { Ide_léphet.Add(x); }
                    }
                }
                else
                {
                    string x = Egyet_lép(position, fel_vagyle, ref utes);
                    if (utes == 0) { Ide_léphet.Add(x); }
                }
            }
            if ((szam == 5 && szin == 'w') || (szam == 4 && szin == 'b'))
            {
                string last_lepes = Összes_lépés[Összes_lépés.Count - 1].ToString();
                for (int i = 7; i < 9; i++)
                {
                    string sv = Egyet_lép(position, i, ref utes);
                    if (sv == last_lepes.Substring(4) && last_lepes[3] == 'g')
                    {

                        Ide_léphet.Add(Egyet_lép(sv + "w", fel_vagyle, ref utes));
                        Special_moveOccured = (Egyet_lép(sv + "w", fel_vagyle, ref utes) + last_lepes.Substring(4));
                    }
                }

            }
            utes = 0; //az elágazásokba belépve változhat az utes,ezért mielőtt teszteljuk a gyalog esetében a ferde lépés(utes) lehetőségét muszáj lenullázni!
            for (int i = fel_vagyle_utes; i < fel_vagyle_utes + 2; i++)
            {
                string tmp = Egyet_lép(position, i, ref utes);
                if (utes == 1 && All_partIsneeded == true) { Ide_léphet.Add(tmp); utes = 0; }
                else if (tmp != "-" && All_partIsneeded == false) { Ide_léphet.Add(tmp); }
            }
            return Ide_léphet;
        }
        public string Egyet_lép(string position, int x, ref int utes)
        {
            char betu = position[0];
            int betu_positionInabc = abc.IndexOf(betu);
            int szam = Convert.ToInt16(position[1].ToString());
            char szin = position[2];
            string tmp = "";
            if (betu_positionInabc + 1 <= 7 && szam + 1 <= 8 && x == 1)
            {
                tmp = abc[betu_positionInabc + 1] + (szam + 1).ToString();//x==1 balra_fel
            }
            else if (betu_positionInabc - 1 >= 0 && szam + 1 <= 8 && x == 2)
            {
                tmp = abc[betu_positionInabc - 1] + (szam + 1).ToString();//x==2 jobbra fel
            }
            else if (betu_positionInabc + 1 <= 7 && szam - 1 >= 1 && x == 3)
            {
                tmp = abc[betu_positionInabc + 1] + (szam - 1).ToString();  //x==3 balra le
            }
            else if (betu_positionInabc - 1 >= 0 && szam - 1 >= 1 && x == 4)
            {
                tmp = abc[betu_positionInabc - 1] + (szam - 1).ToString();//x==4 jobbra le
            }
            else if (szam + 1 <= 8 && x == 5)
            {
                tmp = abc[betu_positionInabc] + (szam + 1).ToString();//x==5 fel egyet
            }
            else if (szam - 1 >= 1 && x == 6)
            {
                tmp = abc[betu_positionInabc] + (szam - 1).ToString();//x==6 le egyet
            }
            else if (betu_positionInabc + 1 <= 7 && x == 7)
            {
                tmp = abc[betu_positionInabc + 1] + (szam).ToString();//x==7 balra egyet
            }
            else if (betu_positionInabc - 1 >= 0 && x == 8)
            {
                tmp = abc[betu_positionInabc - 1] + (szam).ToString();//x==8 jobbra egyet
            }
            if (tmp != "")
            {
                string g = Mező_tulajdonság(tmp);
                if ((g == "0"))
                {
                    return tmp;
                }
                else if (g != szin.ToString())
                {
                    utes = 1;
                    return tmp;
                }
            }
            return "-";
        }
        public ArrayList Validation_bábok(string position, int startindex, int endindex)
        {
            ArrayList Ide_léphet = new ArrayList();
            string c_position;
            int utes = 0;
            for (int i = startindex; i < endindex; i++)
            {
                c_position = position;
                while (true)
                {
                    if (utes == 1) { utes = 0; break; }
                    c_position = Egyet_lép(c_position, i, ref utes);
                    if (c_position != "-") { Ide_léphet.Add(c_position); c_position += position[2]; }

                    else { break; }
                }
            }
            return Ide_léphet;
        }
        public ArrayList Validation_futo(string position)
        {
            return Validation_bábok(position, 1, 5);
        }
        public ArrayList Validation_vezer(string position)
        {
            return Validation_bábok(position, 1, 9);
        }
        public ArrayList Validation_bastya(string position)
        {
            return Validation_bábok(position, 5, 9);
        }
        public ArrayList Validation_huszar(string position)
        {
            ArrayList Ide_léphet = new ArrayList();
            char betu = position[0];
            int betu_positionInabc = abc.IndexOf(betu);
            int szam = Convert.ToInt16(position[1].ToString());
            string szin = position[2].ToString();
            if (betu_positionInabc + 1 <= 7 && szam + 2 <= 8 && Mező_tulajdonság(abc[betu_positionInabc + 1] + (szam + 2).ToString()) != szin)
            {
                Ide_léphet.Add(abc[betu_positionInabc + 1] + (szam + 2).ToString());
            }
            if (betu_positionInabc - 1 >= 0 && szam + 2 <= 8 && Mező_tulajdonság(abc[betu_positionInabc - 1] + (szam + 2).ToString()) != szin)
            {
                Ide_léphet.Add(abc[betu_positionInabc - 1] + (szam + 2).ToString());
            }
            if (betu_positionInabc + 1 <= 7 && szam - 2 >= 1 && Mező_tulajdonság(abc[betu_positionInabc + 1] + (szam - 2).ToString()) != szin)
            {
                Ide_léphet.Add(abc[betu_positionInabc + 1] + (szam - 2).ToString());
            }
            if (betu_positionInabc - 1 >= 0 && szam - 2 >= 1 && Mező_tulajdonság(abc[betu_positionInabc - 1] + (szam - 2).ToString()) != szin)
            {
                Ide_léphet.Add(abc[betu_positionInabc - 1] + (szam - 2).ToString());
            }
            if (betu_positionInabc - 2 >= 0 && szam + 1 <= 8 && Mező_tulajdonság(abc[betu_positionInabc - 2] + (szam + 1).ToString()) != szin)
            {
                Ide_léphet.Add(abc[betu_positionInabc - 2] + (szam + 1).ToString());
            }
            if (betu_positionInabc - 2 >= 0 && szam - 1 >= 1 && Mező_tulajdonság(abc[betu_positionInabc - 2] + (szam - 1).ToString()) != szin)
            {
                Ide_léphet.Add(abc[betu_positionInabc - 2] + (szam - 1).ToString());
            }
            if (betu_positionInabc + 2 <= 7 && szam + 1 <= 8 && Mező_tulajdonság(abc[betu_positionInabc + 2] + (szam + 1).ToString()) != szin)
            {
                Ide_léphet.Add(abc[betu_positionInabc + 2] + (szam + 1).ToString());
            }
            if (betu_positionInabc + 2 <= 7 && szam - 1 >= 1 && Mező_tulajdonság(abc[betu_positionInabc + 2] + (szam - 1).ToString()) != szin)
            {
                Ide_léphet.Add(abc[betu_positionInabc + 2] + (szam - 1).ToString());
            }
            return Ide_léphet;
        }
        public bool Lépette_már(string position)//megvizsgálja hogy egy bizonyos mezőn lévó bábú lépett e már a játék során
        {
            for (int i = 0; i < Összes_lépés.Count; i++)
            {
                string tmp = Összes_lépés[i].ToString();
                if (tmp.Substring(0, 2) == position) { return true; }
            }
            return false;
        }
        public ArrayList Validation_király(string position, bool part = true)
        {
            ArrayList Ide_léphet = new ArrayList();
            string c_position;
            int utes = 0;
            for (int i = 1; i < 9; i++)
            {
                c_position = position;
                c_position = Egyet_lép(c_position, i, ref utes);
                if (c_position != "-") { Ide_léphet.Add(c_position); c_position += position[2]; }
            }
            if (part == true)
            {
                string szn;
                if (position[2] == 'w') //position[2]=w vagy b
                {
                    szn = "1";
                }
                else { szn = "8"; }
                Sakkot_kapott(position[2], "asd");
                if (IsSakkot == false)
                {
                    string r = Sáncolás_átalános(position[2], "h" + szn, "f" + szn + "g" + szn);
                    if (r != "0") { Ide_léphet.Add(r); sanc_rovid = r; }
                    r = Sáncolás_átalános(position[2], "a" + szn, "d" + szn + "c" + szn);
                    if (r != "0") { Ide_léphet.Add(r); sanc_hosszu = r; }
                }
                for (int i = Ide_léphet.Count - 1; i >= 0; i--)
                {
                    string item = Ide_léphet[i].ToString();
                    string tmp = Real_Mező_tulajdonság(item);
                    Change_arrayOf_Peaces(item + position[2] + "k");
                    bool b = Do_theBigLoop(item, position[2]);
                    if (b == true) { Ide_léphet.Remove(item); }
                    Change_arrayOf_Peaces(tmp);
                }
            }
            return Ide_léphet;
        }
        public string Sáncolás_átalános(char szin, string bástya, string check_list)//"h1","f1g1"
        {
            if ((szin == 'w' && white_kiraly == "e1" && Lépette_már("e1") == false) || (szin == 'b' && black_kiraly == "e8" && Lépette_már("e8") == false))
            {
                if (Real_Mező_tulajdonság(bástya) == bástya + szin + "b" && Lépette_már(bástya) == false)
                {
                    int x = 0;
                    string ret = "";
                    for (int i = 0; i < check_list.Length; i += 2)
                    {
                        string tmp = check_list.Substring(i, 2);
                        Sakkot_kapott(szin, tmp, true);
                        if (Mező_tulajdonság(tmp) == "0" && IsSakkot == false) { x += 1; ret = tmp; }
                    }
                    if (x == check_list.Length / 2) { return ret; }
                }
            }
            return "0";
        }
        public bool Do_theBigLoop(string item, char szin)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    string tmp = Array_ofPieces[i, j];

                    if (tmp.Length > 2 && tmp[2] != szin)
                    {
                        ArrayList ar = Final_validation(tmp);
                        if (ar.Contains(item)) { return true; }
                    }
                }
            }
            return false;
        }
        public ArrayList Final_validation(string position, bool forkirály = false)
        {           
            ArrayList tmp_ar = new ArrayList();
            if (position[3] == 'b') { tmp_ar = Validation_bastya(position); }
            else if (position[3] == 'h') { tmp_ar = Validation_huszar(position); }
            else if (position[3] == 'f') { tmp_ar = Validation_futo(position); }
            else if (position[3] == 'v') { tmp_ar = Validation_vezer(position); }
            else if (position[3] == 'g') { tmp_ar = Validation_forGyalog(position, forkirály); }
            else { tmp_ar = Validation_király(position, forkirály); }
            while (tmp_ar.Contains("-")) { tmp_ar.Remove("-"); }//szükséges hogy csak valós lépéseket tartalmazzon                  
            return tmp_ar;                                      //a gyűjtemény
        }
        public string Real_Mező_tulajdonság(string mezo)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (Array_ofPieces[i, j].Substring(0, 2) == mezo)
                    {
                        string tmp = Array_ofPieces[i, j];
                        return tmp;
                    }
                }
            }
            return "-";
        }
        public string Mező_tulajdonság(string mezo) //fekete fehér és milyen báb vagy nem áll babú azon a mezőn 
        {
            string tmp = Real_Mező_tulajdonság(mezo);
            if (tmp.Length == 2) { return "0"; }
            else return tmp[2].ToString();
        }
        private void Button_Újfigura_clicked(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            string babu = b.Tag.ToString();
            string szin = Special_moveOccured;
            Special_moveOccured = "";
            Bitmap bp = new Bitmap(szin + babu + ".png");
            Innen_lepEL.BackgroundImage = bp;
            Innen_lepEL.Tag = Innen_lepEL.Tag.ToString().Substring(0, 2) + szin + babu[0];
            Change_arrayOf_Peaces(Innen_lepEL.Tag.ToString());
            Change_page(panel_gyalogátváltozás, Panel_mezők, false);
            Mező_firstClicked = 1;
        }
        private void Button_undo_last(object sender, EventArgs e)
        {
            if (Összes_lépés.Count > 0 && Mező_firstClicked == 1)
            {
                string last_lepes = Összes_lépés[Összes_lépés.Count - 1].ToString();
                Összes_lépés[Összes_lépés.Count - 1] = "";
                Összes_lépés.Remove("");
                string innen = last_lepes.Substring(0, 4);
                string ide = last_lepes.Substring(4, 2);
                if (innen.Substring(2, 2) == "wk") { white_kiraly = innen.Substring(0, 2); }
                else if (innen.Substring(2, 2) == "bk") { black_kiraly = innen.Substring(0, 2); }

                Change_arrayOf_Peaces(innen);
                Change_arrayOf_Peaces(ide);
                Change_panel(innen).BackgroundImage = Change_panel(ide).BackgroundImage;
                Change_panel(ide).BackgroundImage = null;

                if (last_lepes.Length > 6 && last_lepes[6] == '/')
                {
                    string leutott_gyalog = last_lepes.Substring(7, 2) + Whosturn + "g";
                    Change_arrayOf_Peaces(leutott_gyalog);
                    Bitmap bm = new Bitmap(Whosturn + "gyalog.png");
                    Change_panel(leutott_gyalog).BackgroundImage = bm;
                }
                else if (last_lepes.Length > 6 && last_lepes[6] == '0')
                {
                    string bastya_ide = last_lepes.Substring(7, 2);
                    string bastya_innen = last_lepes.Substring(9, 2) + last_lepes[2] + "b"; //last_lepes[2]==szin 'w' vagy 'b' ami megegyezik a figura színével amit vissza teszek
                    Change_arrayOf_Peaces(bastya_ide);
                    Change_arrayOf_Peaces(bastya_innen);
                    Change_panel(bastya_innen).BackgroundImage = Change_panel(bastya_ide).BackgroundImage;
                    Change_panel(bastya_ide).BackgroundImage = null;
                    if (last_lepes[2] == 'w') { white_kiraly = "e1"; }
                    else { black_kiraly = "e8"; }

                }               
                else if (last_lepes.Length >=8)
                {                   
                    string engem_utottle = last_lepes.Substring(4, 4);
                    Change_arrayOf_Peaces(engem_utottle);

                    string value = dict[engem_utottle[3]];
                    Bitmap bm = new Bitmap(Whosturn + value + ".png");
                    Change_panel(engem_utottle).BackgroundImage = bm;
                    if (last_lepes.Length == 9)
                    {
                        Change_arrayOf_Peaces(innen.Substring(0, 3) + "g");
                        Bitmap nm = new Bitmap(innen[2] + "gyalog.png");
                        Change_panel(innen).BackgroundImage = nm;
                    }
                }
                Change_turn();
            }
        }
        private void Save_current_game(object sender, EventArgs e)
        {
            if (Mező_firstClicked == 1)
            {
                string filename = toolStripTextBox2.Text;
                System.Diagnostics.Debug.WriteLine(filename + "called");
                try
                {
                    StreamWriter sr = new StreamWriter(filename);
                    foreach (var item in Array_ofPieces)
                    {
                        sr.WriteLine(item);
                    }
                    foreach (var item2 in Összes_lépés)
                    {
                        sr.WriteLine(item2);
                    }
                    sr.WriteLine(";");
                    sr.WriteLine(white_kiraly);
                    sr.WriteLine(black_kiraly);
                    sr.Close();
                    MessageBox.Show("Sikeresen mentve!");
                }
                catch { MessageBox.Show("Hibas fájl név!"); }
            }
        }
        private void Load_game(object sender, EventArgs e)
        {
            if (isload_enabled == true)
            {
                string filename = toolStripTextBox1.Text;
                try
                {
                    string[] full_content = File.ReadAllLines(filename);
                    int counter = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            Array_ofPieces[i, j] = full_content[counter];
                            Change_panel(Array_ofPieces[i, j]);
                            counter++;
                        }
                    }
                    Összes_lépés.Clear();
                    while (full_content[counter] != ";")
                    {
                        Összes_lépés.Add(full_content[counter]);
                        counter++;
                    }
                    white_kiraly = full_content[counter + 1];
                    black_kiraly = full_content[counter + 2];
                    Set_pictures();
                    if (Összes_lépés.Count % 2 == 0) { Whosturn = 'w'; }
                    else { Whosturn = 'b'; }
                    Mező_firstClicked = 1;
                    button5.PerformClick();
                }
                catch { MessageBox.Show("Hibás fájl név!"); }
            }
        }
        public void Set_pictures()
        {
            //képeket beállítani
            string position;
            foreach (var item in Panel_mezők)
            {
                position = item.Tag.ToString();
                if (position.Length > 2)
                {
                    string name_peace = dict[position[3]];
                    Bitmap bm = new Bitmap(position[2] + name_peace + ".png");
                    item.BackgroundImage = bm;
                }
                else
                {
                    item.BackgroundImage = null;
                }
            }
        }
        private void New_game(object sender, EventArgs e)
        {
            isload_enabled = false;            
            panel_loader.Visible = false;
            string new_time;
            if (numericUpDown2.Value.ToString().Length == 1)
            { new_time = numericUpDown1.Value.ToString() + " : " + "0" + numericUpDown2.Value.ToString(); }
            else { new_time = numericUpDown1.Value.ToString() + " : " + numericUpDown2.Value.ToString(); }
            label_órafeher.Text = new_time; label_óra_black.Text = new_time;
            int get_inMp = Convert.ToInt16(numericUpDown1.Value) * 60 + Convert.ToInt16(numericUpDown2.Value);
            white_time = get_inMp; black_time = get_inMp;
            stop_clock = false;
            szal = new Thread(new ThreadStart(doit));
            szal.Start();
        }
        delegate void Delg(string what);
        Delg clock1;
        Delg clock2;
        public void doit()
        {
            string time;
            clock1 = new Delg(Change_label_feher);
            clock2 = new Delg(Change_label_fekete);
            try
            {
                while (stop_clock == false)
                {
                    Thread.Sleep(1000);
                    if (Whosturn == 'w' && white_time != 0)
                    {
                        time = Timer(ref white_time);
                        label_órafeher.Invoke(clock1, time);
                    }
                    else if (Whosturn == 'b' && black_time != 0)
                    {
                        time = Timer(ref black_time);
                        label_óra_black.Invoke(clock2, time);
                    }
                    else if (white_time == 0 || black_time == 0)
                    {
                        string winner = "Világos";
                        if (Whosturn == 'w') { winner = "Sötét"; }
                        MessageBox.Show("A győztes: " + winner);
                        Mező_firstClicked = -2;
                    }
                }
            }
            catch { szal.Abort(); }
        }
        public void Change_label_feher(string what)
        {
            label_órafeher.Text = what;
        }
        public void Change_label_fekete(string what)
        {
            label_óra_black.Text = what;
        }
        public string Timer(ref int clock)
        {
            clock--;
            int min = clock / 60;
            int sec = clock - min * 60;
            string final;
            if (sec.ToString().Length == 1)
            { final = min + " : " + "0" + sec; }
            else
            {
                final = min + " : " + sec;
            }
            return final;
        }
        private void Go_back(object sender, EventArgs e)
        {
            if (Mező_firstClicked == -2)
            {
                Mező_firstClicked = 1;
                while (Összes_lépés.Count != 0) { undoToolStripMenuItem.PerformClick(); }
                panel_loader.Visible = true;
                panel_loader.BringToFront();
                Whosturn = 'w';
                isload_enabled = true;
                try
                {
                    label6.Visible = false;
                    kliens.Close();
                    stream.Close();
                }
                catch { }
            }        
        }

        Thread szal_halozat;
        Thread szal_received_communication;       
        //tcp/ip hálozati protocol kliens elkészítése
        TcpClient kliens = new TcpClient();
        private void Button_Start_LAN(object sender, EventArgs e)
        {                                          
                szal_halozat = new Thread(new ThreadStart(Start_LAN));//uj szal létrehozása amely a hálozazi kommunikációért lesz felelős
                szal_halozat.Start();  //a szál indul is
                Typeofconnection = 'l';       
        }
        public delegate void For_commincation(string szöveg);
        public delegate void Perfrom_it();
        For_commincation fc;
        Perfrom_it p;
        public Stream stream;       
        public void Start_LAN()
        {
            try
            {
                kliens.Connect("127.0.0.1", 8001);//connect to szerver! 
                isload_enabled = false;
                p = new Perfrom_it(Button7_Perform_click);
                button7.Invoke(p);
                stream = kliens.GetStream();
                string fogadott = Bytearray_tostring();
                white_time = Convert.ToInt16(fogadott.Substring(2)); black_time = Convert.ToInt16(fogadott.Substring(2));
                if (fogadott[1] == 'b') { Isit_myturn = false; }
                szal_received_communication = new Thread(new ThreadStart(Communication_received));
                szal_received_communication.Start();
            }
            catch
            {
                p = new Perfrom_it(Set_label);
                label6.Invoke(p);
                szal_halozat.Abort();
            }                     
        }
        public void Set_label()
        {
            label6.Visible = true;
            label6.Text = "Nem lehet kapcsolatot részesíteni a célszerverrel!Probálja meg később.";
        }
        public void Button7_Perform_click()
        {
            button7.PerformClick(); // Uj_jaték button leutes elkezdődik a játék          
        }
        public void Commincation(string szöveg)
        {
            listBox1.Items.Add(szöveg);
        }
        private void Button_Send_Message(object sender, EventArgs e)
        {
            if (Typeofconnection == 'l')
            {
                string adat = textBox1.Text;
                listBox1.Items.Add("you : " + adat);
                byte[] adatot_küld = new byte[adat.Length + 1];
                adatot_küld[0] = Convert.ToByte('/');
                for (int i = 1; i < adatot_küld.Length; i++)
                {
                    adatot_küld[i] = Convert.ToByte(adat[i - 1]);
                }
                stream.Write(adatot_küld, 0, adatot_küld.Length);
            }
            else { listBox1.Items.Add(textBox1.Text); }
        }
        public void Communication_received()
        {
            try
            {
                while (true)
                {
                    string fogadott = Bytearray_tostring();
                    fc = new For_commincation(Commincation);
                    if (fogadott[0] == '/')
                    { listBox1.Invoke(fc, "opponent : " + fogadott.Substring(1)); }
                    else if (fogadott[0] == '!')
                    {
                        Isit_myturn = true;
                        foreach (var item in Panel_mezők)
                        {
                            if (item.Name.ToString() == fogadott.Substring(1)) {  Button_mező_clicked(item, EventArgs.Empty); }
                        }                       
                    }
                    else if (fogadott[0] == '-')
                    {
                        stop_clock = true;
                        Typeofconnection = 'o';
                        MessageBox.Show("A kapcsolat a szerver és a kliens között megszakadt Te nyertél!");
                        Mező_firstClicked = -2;
                    }
                }
            }
            catch { szal_received_communication.Abort(); szal_halozat.Abort(); }           
        }
        public string Bytearray_tostring()
        {
            string fogadott = "";
            byte[] buffer = new byte[210];
            stream.Read(buffer, 0, 210);
            foreach (var item in buffer)
            {
                if (item != 0)
                { fogadott += Convert.ToChar(item); }
            }
            return fogadott;
        }
        public byte[] Fill_byte(string szoveg)
        {
            byte[] buffer = new byte[szoveg.Length];
            for (int i = 0; i < szoveg.Length; i++)
            {
                buffer[i] = Convert.ToByte(szoveg[i]);
            }
            return buffer;
        }
        public void Send_lepes()
        {
            if (kliens.Connected==true)
            {
                stream.Write(Fill_byte("!" + send_tolan[0]),0,send_tolan[0].Length+1);
                stream.Write(Fill_byte("!" + send_tolan[1]),0, send_tolan[1].Length + 1);
                send_tolan.Clear();                                            
            }
        }
        private void Kill_thread(object sender, FormClosingEventArgs e)
        {
            try
            {
                if(Typeofconnection=='l')
                { stream.WriteByte(Convert.ToByte('-')); } //megszakítás kezdeményezése           
                kliens.Close();
            }
            catch { }
        }
    }  
}

    