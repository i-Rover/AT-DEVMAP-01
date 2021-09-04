using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace V2Tech.Automations.Apps.Forms
{
    class window_model : Exception
    {
    //class window_model : controller{
        public string bugMsg = "Bug starts here!";
        public string txt = ".txt";
        public string[] header = { "myTimeStamp", "Operator", "Unit", "SerialNum", "PartNum", "Barcode_SN", "LD_SN", "Child_ID", "Wafer_ID", "Result" };
        public string slash = "/";
        public string empty = "";
        public string yymmdd = "yyyyMMdd";
        public string tt = "tt";
        public string machineName = "AT-DEVMAP-01";
        public string testPath = @"C:\Users\tocan\Desktop\";
        public string rootPath = @"D:\Finisar\Log\ResultLog\";
        public string slasher = "\\";
        public string ld_unit = "LD_Unit_";
        public string underscore = "_";
        public string png = ".png";
        public string testMsg = "Testing";
        public string pathName = "C:\\Users\\tocan\\Desktop\\nkedEye.txt";
        public string dirName = "C:\\Users\\tocan\\Desktop\\solidEye.txt";
        public string clickMsg = "Click pass or fail Now";
        public string fileExe = "Image Files(*.jpg; *.jpeg; *.png; *.gif; *.bmp)|*.jpg; *.jpeg; *.png; *.gif; *.bmp";
        public string badAlert = "You should select image then only you can zoom";
        public string rootImage = "C:\\Users\\Public\\Pictures\\Sample Pictures\\";
        public string exe = ".jpg";
        public string space = " ";
        public string LD_Compare = "LD Compare";
        public string passText = "PASS";
        public string failText = "FAIL";
        public string inspecMsg = "Please inspect the LD";
        public string noBar = "No Barcode detected! Proceed to next DUT";
        public string tab = "\t";
        public string sccMsg = "Data has been created!";
        public string errMsg = "Data failed to push!";
        public string dataPush = "Amount of data pushed: ";
        public string NOdataPush = "No data is detected!";
        public string stateUndefined = "Undefined State!";
        public string startEngineMSG = "startEngine Run Now!";
        public string doStuffMSG = "doStuff Run Now!";
        public string endEngineMSG = "endEngine Run Now!";
        public string statusMove = "Status : Camera is moving";
        public string statusCapture = "Status : Camera is capturing";
        public string statusDone = "Status : Process Done!";
        public string statusDefault = "Status :";
        public string save_image_ExistedFile = "Save Image in Existed Folder";
        public string save_image_NewFile = "Save Image in New Created Folder";
        public string noLdNum = "";
        public string medium = "Medium";
        public string shorty = "Short";
        public string fine = "Fine";
        public string ultra = "Ultra";
        public void Initialize(string a, string b, string c, string d, string e, string f, string g, string h,string i,string j,string k,string l,string m,string n,string o,string p,string q,string r,string s,string t,string u,string v,string w,string x,string y,string z,string aa,string bb,string cc,string dd,string ee,string ff,string gg,string hh,string ii,string jj,string kk,string ll,string mm,string nn,string oo, string pp,string qq,string rr,string[] ss,string tt,string uu,string vv)
        {
            this.testMsg = a;
            this.fileExe = b;
            this.badAlert = c;
            this.rootImage = d;
            this.exe = e;
            this.space = f;
            this.clickMsg = g;
            this.LD_Compare = h;
            this.passText = i;
            this.failText = j;
            this.inspecMsg = k;
            this.noBar = l;
            this.pathName = m;
            this.tab = n;
            this.sccMsg = o;
            this.errMsg = p;
            this.dirName = q;
            this.dataPush = r;
            this.NOdataPush = s;
            this.stateUndefined = t;
            this.startEngineMSG = u;
            this.doStuffMSG = v;
            this.endEngineMSG = w;
            this.statusMove = x;
            this.statusCapture = y;
            this.statusDone = z;
            this.statusDefault = aa;
            this.save_image_ExistedFile = bb;
            this.save_image_NewFile = cc;
            this.slasher = dd;
            this.ld_unit = ee;
            this.underscore = ff;
            this.png = gg;
            this.noLdNum = hh;
            this.medium = ii;
            this.shorty = jj;
            this.fine = kk;
            this.ultra = ll;
            this.machineName = mm;
            this.rootPath = nn;
            this.yymmdd = oo;
            this.tt = pp;
            this.slash = qq;
            this.empty = rr;
            this.header = ss;
            this.testPath = tt;
            this.txt = uu;
            this.bugMsg = tt;
        }
    }
}
