using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
//using System.IO;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Utilities;
using V2tech.Variable;
using EAutomation.Variable;
using System.Text.RegularExpressions;
//using Keyence.AutoID.SDK;

using NationalInstruments.Vision;
using NationalInstruments.Vision.Analysis;
using NationalInstruments.Vision.WindowsForms;
using Vision_Assistant.Utilities;
using Vision_Assistant;


using Instrument;
using Motion;
using Vision.Camera.CameraRecipe;
using Cognex.DataMan.SDK;
using Cognex.DataMan.SDK.Discovery;
using Cognex.DataMan.SDK.Utils;
using Automation.BDaq;
using V2Tech.Automations.Apps.Forms;

namespace V2Tech.Automations.Apps.Forms
{
    public partial class frmManualMotion : Form
    {
        /*Variables for camera*/
        public static Vision.Cameras g_Cameras;
        public static Vision.LedPro g_ledpros;
        public static int LedChannel;
        private void EnableAxisUIControl(bool x, bool y, bool z, bool u, bool Rx, bool Ry, bool restcntrlstatus)
        {
            btnY2sub.Enabled = y;
        }
        public bool MoveAxisRelative(int motionGrp, int axis, double distance_mm, double speed_mm_s_per, bool waitForDone)
        {

            try
            {
                switch (motionGrp)
                {
                    case (int)MotionGroup.MainTable:
                        switch (axis)
                        {
                            case (int)AxisID.X:
                                EAutomation.Variable.GlobalVar.Galil_Main.SetSpeed(speed_mm_s_per, axis);
                                EAutomation.Variable.GlobalVar.Galil_Main.MoveRel(distance_mm, axis);
                                break;
                            case (int)AxisID.Y:
                                EAutomation.Variable.GlobalVar.Galil_Main.SetSpeed(speed_mm_s_per, axis);
                                EAutomation.Variable.GlobalVar.Galil_Main.MoveRel(distance_mm, axis);
                                break;
                            case (int)AxisID.Z:
                                EAutomation.Variable.GlobalVar.Galil_Main.SetSpeed(speed_mm_s_per, axis);
                                EAutomation.Variable.GlobalVar.Galil_Main.MoveRel(distance_mm, axis);
                                break;
                        }
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Relative Move-Error");
                return false;
            }

        }
        private void ResetMainPanelUI(int motiongroup)
        {
            //btnZsub.Text = "-Z";
            //btnZadd.Text = "+Z";
            //btnUsub.Text = "-U";
            //btnUadd.Text = "+U";
            switch (motiongroup)
            {
                case (int)MotionGroup.MainTable:
                    EnableAxisUIControl(true, true, true, false, false, false, true);
                    //btnZsub.Text = "-U";
                    //btnZadd.Text = "+U";
                    break;
            }
        }        
        public int cbMotionGrp = 0;
        private bool islive = false;
        /*Variables for camera*/

        /*Enum for type*/
        enum JogType
        {
            Medium,
            Short,
            Fine,
            Ultra
        }
        /*Enum for type*/

        /*Enum for Camera*/
        enum CameraStatus { 
            StatusMove,
            StatusCapture
        }
        /*Enum for Camera*/

        public frmManualMotion()
        {
            InitializeComponent();
            GetCameraAlgo();
        }

        /*GetCameraAlgo Starts Here*/
        public void GetCameraAlgo() {
            var view = new window_model();
            string undefinedState = view.stateUndefined;
            foreach (EAutomation.Variable.GlobalVar.stateMachine stateMachine in Enum.GetValues(typeof(EAutomation.Variable.GlobalVar.stateMachine)))
            {
                switch(stateMachine){
                    case EAutomation.Variable.GlobalVar.stateMachine.InitCam:
                        g_Cameras = EAutomation.Variable.GlobalVar.g_Cameras[0];
                        break;
                    case EAutomation.Variable.GlobalVar.stateMachine.InitLed:
                        LedChannel = Convert.ToInt16(EAutomation.Variable.GlobalVar.Channel_2.Remove(0, 8));
                        g_ledpros = EAutomation.Variable.GlobalVar.g_ledpros;
                        break;
                    case EAutomation.Variable.GlobalVar.stateMachine.GetImage:
                        if (EAutomation.Variable.GlobalVar.isItTrue == true)
                        {
                            g_ledpros.LE_SetConstInt((byte)LedChannel, 0);//com2 channel1
                            g_ledpros.LE_SetStrobeInt((byte)LedChannel, 0);
                            g_ledpros.LE_SetStrobeWidth((byte)LedChannel, 0); //set none active chnl to 0
                            g_ledpros.LE_SetCHMode((byte)LedChannel, g_ledpros.CONSTANT);
                            g_ledpros.LE_SetConstInt((byte)LedChannel, (byte)Convert.ToInt16(EAutomation.Variable.GlobalVar.lightIntensity));
                            g_Cameras.GetImage(false);
                            g_ledpros.LE_SetConstInt((byte)LedChannel, 0);
                        }
                        else
                        {
                            g_ledpros.LE_SetConstInt((byte)LedChannel, 0);//com2 channel1
                            g_ledpros.LE_SetStrobeInt((byte)LedChannel, 0);
                            g_ledpros.LE_SetStrobeWidth((byte)LedChannel, 0); //set none active chnl to 0
                            g_ledpros.LE_SetCHMode((byte)LedChannel, g_ledpros.CONSTANT);
                            g_ledpros.LE_SetConstInt((byte)LedChannel, 2);
                            g_Cameras.GetImage(false);
                            g_ledpros.LE_SetConstInt((byte)LedChannel, 0);
                        }
                        VHSImageViewer.ZoomToFit = true;
                        VHSImageViewer.Attach(EAutomation.Variable.GlobalVar.SourceImage);
                        break;
                    default:
                        MessageBox.Show(undefinedState);
                        break;
                }
            }
        }
        /*GetCameraAlgo Ends Here*/

        /*Type Starts Here*/
        private void cbJobType_SelectedIndexChanged(object sender, EventArgs e)
        {
           switch(cbJobType.SelectedIndex){
              case (int)JogType.Medium:
                 nudJogX.Value = (decimal)1.0000;
                 nudJogY.Value = (decimal)1.0000;
                 nudJogZ.Value = (decimal)1.0000;
                 nudJogU.Value = (decimal)1.0000;
                 EAutomation.Variable.GlobalVar.defaultIndex = true;
                 EAutomation.Variable.GlobalVar.jogType = 0;
              break;
              case (int)JogType.Short:
                 nudJogX.Value = (decimal)0.1000;
                 nudJogY.Value = (decimal)0.1000;
                 nudJogZ.Value = (decimal)0.1000;
                 nudJogU.Value = (decimal)0.1000;
                 EAutomation.Variable.GlobalVar.defaultIndex = true;
                 EAutomation.Variable.GlobalVar.jogType = 1;
              break;
              case (int)JogType.Fine:
                 nudJogX.Value = (decimal)0.0100;
                 nudJogY.Value = (decimal)0.0100;
                 nudJogZ.Value = (decimal)0.0100;
                 nudJogU.Value = (decimal)0.0100;
                 EAutomation.Variable.GlobalVar.defaultIndex = true;
                 EAutomation.Variable.GlobalVar.jogType = 2;
              break;
              case (int)JogType.Ultra:
                 nudJogX.Value = (decimal)0.0010;
                 nudJogY.Value = (decimal)0.0010;
                 nudJogZ.Value = (decimal)0.0010;
                 nudJogU.Value = (decimal)0.0010;
                 EAutomation.Variable.GlobalVar.defaultIndex = true;
                 EAutomation.Variable.GlobalVar.jogType = 3;
              break;                    
              }
        }
        /*Type Ends Here*/

        /*Y Sub Starts Here*/
        private void btnY2sub_Click(object sender, EventArgs e)
        {
            foreach(int i in Enum.GetValues(typeof(CameraStatus))){
                switch(i){
                    case (int)CameraStatus.StatusMove:
                            try
                            {
                                EnableAxisUIControl(false, false, false, false, false, false, false);
                                int motionGrp = cbMotionGrp;
                                double speedper = (double)nudSpeed.Value;
                                double distance = -1 * (double)nudJogY.Value;
                                MoveAxisRelative(motionGrp, (int)AxisID.Y, distance, speedper, true);
                                ResetMainPanelUI(motionGrp);

                            }catch { EnableAxisUIControl(true, true, true, true, true, true, true); }
                        break;
                    case (int)CameraStatus.StatusCapture:
                            GetCameraAlgo();
                        break;
                    default:
                        break;
                }
            }
        }
        /*Y Sub Ends Here*/

        /*Y Add Starts Here*/
        private void btnY2add_Click(object sender, EventArgs e)
        {
            foreach(int i in Enum.GetValues(typeof(CameraStatus))){
                switch(i){
                    case (int)CameraStatus.StatusMove:
                        try
                        {
                            EnableAxisUIControl(false, false, false, false, false, false, false);
                            int motionGrp = cbMotionGrp;
                            double speedper = (double)nudSpeed.Value;
                            double distance = (double)nudJogY.Value;
                            MoveAxisRelative(motionGrp, (int)AxisID.Y, distance, speedper, true);
                            ResetMainPanelUI(motionGrp);
                        }
                        catch { }
                        break;
                    case (int)CameraStatus.StatusCapture:
                        GetCameraAlgo();
                        break;
                    default:
                        break;
                }
            }
        }
        /*Y Add Ends Here*/

        /*X Add Starts Here*/
        private void btnX2add_Click(object sender, EventArgs e)
        {
            foreach (int i in Enum.GetValues(typeof(CameraStatus)))
            {
                switch (i)
                {
                    case (int)CameraStatus.StatusMove:
                        try
                        {
                            EnableAxisUIControl(false, false, false, false, false, false, false);
                            int motionGrp = cbMotionGrp;
                            double speedper = (double)nudSpeed.Value;
                            double distance = (double)nudJogX.Value;
                            MoveAxisRelative(motionGrp, (int)AxisID.X, distance, speedper, true);
                            ResetMainPanelUI(motionGrp);
                        }
                        catch { EnableAxisUIControl(true, true, true, true, true, true, true); }
                        break;
                    case (int)CameraStatus.StatusCapture:
                        GetCameraAlgo();
                        break;
                    default:
                        break;
                }
            }
        }
        /*X Add Ends Here*/

        /*X Sub Starts Here*/
        private void btnX2sub_Click(object sender, EventArgs e)
        {
            foreach (int i in Enum.GetValues(typeof(CameraStatus)))
            {
                switch (i)
                {
                    case (int)CameraStatus.StatusMove:
                        try
                        {
                            EnableAxisUIControl(false, false, false, false, false, false, false);
                            int motionGrp = cbMotionGrp;
                            double speedper = (double)nudSpeed.Value;
                            double distance = -1 * (double)nudJogX.Value;
                            MoveAxisRelative(motionGrp, (int)AxisID.X, distance, speedper, true);
                            ResetMainPanelUI(motionGrp);
                        }
                        catch { EnableAxisUIControl(true, true, true, true, true, true, true); }
                        break;
                    case (int)CameraStatus.StatusCapture:
                        GetCameraAlgo();
                        break;
                    default:
                        break;
                }
            }
        }
        /*X Sub Ends Here*/

        /*Z Sub Starts Here*/
        private void btnZ2sub_Click(object sender, EventArgs e)
        {
            foreach (int i in Enum.GetValues(typeof(CameraStatus)))
            {
                switch (i)
                {
                    case (int)CameraStatus.StatusMove:
                        try
                        {
                            EnableAxisUIControl(false, false, false, false, false, false, false);
                            int motionGrp = cbMotionGrp;
                            double speedper = (double)nudSpeed.Value;
                            double distance = -1 * (double)nudJogZ.Value;
                            MoveAxisRelative(motionGrp, (int)AxisID.Z, distance, speedper, true);
                            ResetMainPanelUI(motionGrp);
                        }
                        catch { EnableAxisUIControl(true, true, true, true, true, true, true); }
                        break;
                    case (int)CameraStatus.StatusCapture:
                        GetCameraAlgo();
                        break;
                    default:
                        break;
                }
            }
        }
        /*Z Sub Ends Here*/

        /*Z Add Starts Here*/
        private void btnZ2add_Click(object sender, EventArgs e)
        {
            foreach (int i in Enum.GetValues(typeof(CameraStatus)))
            {
                switch (i)
                {
                    case (int)CameraStatus.StatusMove:
                        try
                        {
                            EnableAxisUIControl(false, false, false, false, false, false, false);
                            int motionGrp = cbMotionGrp;
                            double speedper = (double)nudSpeed.Value;
                            double distance = (double)nudJogZ.Value;
                            MoveAxisRelative(motionGrp, (int)AxisID.Z, distance, speedper, true);
                            ResetMainPanelUI(motionGrp);
                        }
                        catch { EnableAxisUIControl(true, true, true, true, true, true, true); }
                        break;
                    case (int)CameraStatus.StatusCapture:
                        GetCameraAlgo();
                        break;
                    default:
                        break;
                }
            }
        }
        /*Z Add Ends Here*/

        /*Check pic folder is exist or not*/
        public bool checkFolderPic(string newPathRoot) {
            if (!System.IO.File.Exists(newPathRoot)) {
                EAutomation.Variable.GlobalVar.isThisPicExist = false;
            } else {
                EAutomation.Variable.GlobalVar.isThisPicExist = true;
            }
            return EAutomation.Variable.GlobalVar.isThisPicExist;
        }
        /*Check pic folder is exist or not*/

        /*(Event Handler) FormClosing Starts Here*/
        private void frmManualMotion_FormClosing(object sender, FormClosingEventArgs e)
        {
            var model = new window_model();
            string myDateOnly = DateTime.Now.ToString("d");
            string myDateAfter = myDateOnly.Replace("/", " ");
            string myRootFolder = @"D:\ImageCOC[RECAPTURE]";
            string newPathRoot = System.IO.Path.Combine(myRootFolder, myDateAfter);
            string ld_unit = model.ld_unit;
            string underscore = model.underscore;
            string slasher = model.slasher;
            string exe = model.png;
            string unitNumber = "Unit";
            //string a;
            string SPACE = model.space;
            checkFolderPic(newPathRoot);
            if (!(checkFolderPic(newPathRoot))) {
                System.IO.Directory.CreateDirectory(newPathRoot);
                EAutomation.Variable.GlobalVar.SourceImage.WritePngFile(@"D:\ImageCOC[RECAPTURE]\" + slasher + myDateAfter + slasher + ld_unit + EAutomation.Variable.GlobalVar.myLD + SPACE + myDateAfter + SPACE + unitNumber + EAutomation.Variable.GlobalVar.myUnit + underscore + EAutomation.Variable.GlobalVar.PrcsRslt.Barcode_SN + exe);
            } else {
                System.IO.Directory.CreateDirectory(newPathRoot);
                EAutomation.Variable.GlobalVar.SourceImage.WritePngFile(@"D:\ImageCOC[RECAPTURE]\" + slasher + myDateAfter + slasher + ld_unit + EAutomation.Variable.GlobalVar.myLD + SPACE + myDateAfter + SPACE + unitNumber + EAutomation.Variable.GlobalVar.myUnit + underscore + EAutomation.Variable.GlobalVar.PrcsRslt.Barcode_SN + exe);
            }
            /*Attach image in frmMain starts here*/
            VHSImageViewer.Attach(EAutomation.Variable.GlobalVar.SourceImage);
            EAutomation.Variable.GlobalVar.attachImage = true;
            /*Attach image in frmMain ends here*/
        }
        /*(Event Handler) FormClosing Ends Here*/

        private bool mySystemhAlt() { 
            EAutomation.Variable.GlobalVar.isSystemHalt = true;
            return EAutomation.Variable.GlobalVar.isSystemHalt;
        }

        /*Reset Starts Here*/
        private void Reset_Click(object sender, EventArgs e)
        {
            mySystemhAlt();
        }
        /*Reset Starts Here*/

        /*LED Confirm button*/
        private void Confirm_LED_Click(object sender, EventArgs e)
        {
            string myPattern = @"[a-zA-Z0-9]";
            Regex myRegex = new Regex(myPattern);
            var model = new window_model();
            if (!(myRegex.IsMatch(textBox1.Text))){
                LDerr.SetError(textBox1, "Need Real LD Number");
                EAutomation.Variable.GlobalVar.isErr1 = true;
            }
            else{
                EAutomation.Variable.GlobalVar.isErr1 = true;
                if (textBox1.Text.Length < 5){
                    LDerr.SetError(textBox1, "LD Number should be more than 6 Characters");
                    EAutomation.Variable.GlobalVar.isErr2 = true;
                }
                else{
                    EAutomation.Variable.GlobalVar.isErr1 = false;
                    EAutomation.Variable.GlobalVar.isErr2 = false;
                }
            }
            //true trigger false behaviour
            if ((EAutomation.Variable.GlobalVar.isErr1 && EAutomation.Variable.GlobalVar.isErr1)){
                MessageBox.Show("Please Check LD Number");
            }
            else {
            //false trigger good behaviour
                EAutomation.Variable.GlobalVar.lDCustomOverwrite = textBox1.Text;
                this.Close();
            }
        }

        private void frmManualMotion_Load(object sender, EventArgs e) {
            var modl = new window_model();
            string medium = modl.medium;
            string shorty = modl.shorty;
            string fine = modl.fine;
            string ultra = modl.ultra;
            string bugMsg = modl.bugMsg;
            string space = modl.space;
            try {
                if (EAutomation.Variable.GlobalVar.defaultIndex == true) {
                    switch (EAutomation.Variable.GlobalVar.jogType) {
                        case 0:
                            cbJobType.Text = medium;
                            nudJogX.Value = (decimal)1.0000;
                            nudJogY.Value = (decimal)1.0000;
                            nudJogZ.Value = (decimal)1.0000;
                            nudJogU.Value = (decimal)1.0000;
                            EAutomation.Variable.GlobalVar.defaultIndex = true;
                            EAutomation.Variable.GlobalVar.jogType = 0;
                            break;
                        case 1:
                            cbJobType.Text = shorty;
                            nudJogX.Value = (decimal)0.1000;
                            nudJogY.Value = (decimal)0.1000;
                            nudJogZ.Value = (decimal)0.1000;
                            nudJogU.Value = (decimal)0.1000;
                            EAutomation.Variable.GlobalVar.defaultIndex = true;
                            EAutomation.Variable.GlobalVar.jogType = 1;
                            break;
                        case 2:
                            cbJobType.Text = fine;
                            nudJogX.Value = (decimal)0.0100;
                            nudJogY.Value = (decimal)0.0100;
                            nudJogZ.Value = (decimal)0.0100;
                            nudJogU.Value = (decimal)0.0100;
                            EAutomation.Variable.GlobalVar.defaultIndex = true;
                            EAutomation.Variable.GlobalVar.jogType = 2;
                            break;
                        case 3:
                            cbJobType.Text = ultra;
                            nudJogX.Value = (decimal)0.0010;
                            nudJogY.Value = (decimal)0.0010;
                            nudJogZ.Value = (decimal)0.0010;
                            nudJogU.Value = (decimal)0.0010;
                            EAutomation.Variable.GlobalVar.defaultIndex = true;
                            EAutomation.Variable.GlobalVar.jogType = 3;
                            break;

                    }
                }            
            }catch(Exception ex){
                MessageBox.Show(bugMsg+space+ex.Message);
            }
        }
        /*LED Confirm button*/
    }
}
