using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using NationalInstruments.Vision.Analysis;
using NationalInstruments.Vision;
using NationalInstruments.Vision.WindowsForms;

using Automation.BDaq;
using Instrument;
using V2Tech.AutomationApps;

namespace EAutomation.Variable
{
    //Apps
    public static class GlobalVar
    {
        /*value to set default index*/
        public static bool defaultIndex = false;
        public static int jogType = -1;
        /*value to set default index*/
        
        /*Bool to Attach image in frmMain*/
        public static bool attachImage;
        /*Bool to Attach image in frmMain*/
        
        /*Get Barcode starts here for green box*/
        public static string myOwnBarcode;
        /*Get Barcode ends here for green box*/

        /*Get LD Number for SaveImage(machineState) in frmManualMotion*/
        public static string myLD;
        /*Get LD Number for SaveImage(machineState) in frmManualMotion*/

        /*Get Barcode Unit for SaveImage(machineState) in frmManualMotion*/
        public static string myUnit;
        /*Get Barcode Unit for SaveImage(machineState) in frmManualMotion*/

        /*Get Custom LD Number*/
        public static string lDCustomOverwrite = "";
        /*Get Custom LD Number*/

        /*Collection of bool form validate*/
        public static bool isErr1;
        public static bool isErr2;
        /*Collection of bool form validate*/

        /*is AbortButton Click*/
        public static bool isAbortClicked;
        /*is AbortButton Click*/

        /*is System Halt*/
        public static bool isSystemHalt = false;
        /*is System Halt*/
        
        /*Bool for check pic sheet*/
        public static bool isThisPicExist;
        /*Bool for check pic sheet*/

        /*Bool for check folder sheet*/
        public static bool isThisFileExist;
        /*Bool for check folder sheet*/

        /*Bool to check isSNDetect*/
        public static bool isSNDetect;
        /*Bool to check isSNDetect*/

        /*frmManualKeyIn*/
        public static bool isThiswdClosed;
        public static bool isThiswdOpened;
        /*frmManualkeyIn*/

        /*canOCRRead*/
        public static bool canOCRRead = false;
        /*canOCRRead*/
        /*Machine State for Overiding Camera*/
        public enum stateMachine
        {
            InitCam,
            InitLed,
            GetImage
        }
        public static string cam1 = "0";
        public static string Channel_2 = "Channel_2";
        /*Machine State for Overiding Camera*/

        /*LightIntensity*/
        public static byte lightIntensity;
        public static bool isItTrue;
        /*LightIntensity*/

        /*struct for Unique LD Number*/
        public static myModel model = new myModel();
        public struct myModel {
            public string uniqueLDNum1;
        }
        /*struct for Unique LD Number*/

        /*struct for nkedEye Option*/
        public static myController control = new myController();
        public struct myController {
            //1 denoted solid eye while 2 denoted liquid eye but 3 denoted home position
            public int optionSelected;
        }
        /*struct for nkedEye Option*/

        /*struct for waferID for dgvResult table & datagridview1 table*/
        public static myView view = new myView();
        public struct myView {
            public string myWaferId;
        }
        /*struct for waferID for dgvResult table & datagridview1 table*/
        //login 
        public static IntPtr handle;
        public static string UserLevel;
        public static string UserName;
        public static string UserEmpID;
        public static string UserMachineName;
        public static bool LoginSuccess;
        public static string ServerConnectString = "Data Source= D:\\Finisar\\Setting\\System\\MyDatabase.db;Version=3;";
        //App Use
        //process flow
        public enum ProcessFlow
        {

            Safety_PreCheck,
            Scan_BarCode,
            Move_CaptureReference,
            Move_CaptureSN,
            PushDatatoDb,
            Writing_History,
            FinishTask,
 
        }

        public static string AppName = "";
        public static string AppVersion = "";
        public static string MachineName = "";
        public static string CurrentProduct = "";
        public static string rootpath = "";
        public static string SystemSettingPath = "D:\\Finisar\\Setting\\System\\";
        public static string ProductSettingPath = "D:\\Finisar\\Setting\\Product\\";
        public static string SystemConfig_location = "D:\\Finisar\\Setting\\System\\SystemConfig.xml";
        public static string LogPath = "D:\\Finisar\\Log\\";
        public static SystemVariable system_var = new SystemVariable();
        public static ProductSettings product_var = new ProductSettings();
        public static AlignmentFlow Alignment_profile = new AlignmentFlow();
        public static int tickcount = 0;
        public static int processStep = -1;
        public static bool isabort = false;
        public static bool isabortalignment = false;
        public static Utilities.Logger Resultlog = new Utilities.Logger();
        public static ProcessResult Result = new ProcessResult();
        //public static ProcessResult Verdict = new ProcessResult();
        public static myProcessResult myProcessResultr = new myProcessResult();
        public static ProcessResult PrcsRslt = new ProcessResult();
        public static bool isPause = false;
        public static bool isAbort = false;

        public static V2Tech.AutomationApps.KeyenceBarcodeSR750 g_barcodeRead = new KeyenceBarcodeSR750();
        public static MESFunction.MesWriteData MESResult = new MESFunction.MesWriteData();
        public static MESFunction mes = new MESFunction();
        public static MESFunction.MesContainerInfo MesContainerInfo = new MESFunction.MesContainerInfo();
        public static MESFunction.MesWriteNonDCData NonDCData = new MESFunction.MesWriteNonDCData();
       

        //motion pos use
        public static bool isHomeDone = false;
        public static string GantryPos_Path = "GantryPos.xml";
        public static string FiberAlignerPos_Path = "FiberAlignerPos.xml";
        public static string NestPos_Path = "NestPos.xml";
        public static GantryPos Gantry_Pos = new GantryPos();
      
        //vision variable
        public static Dictionary<string, VisionImage> VisionTemplateBank = new Dictionary<string, VisionImage>();
        public static VisionVariable[] vision_vars = new VisionVariable[0];
        public static VisionImage[] ResultImages = new VisionImage[20];
        public static VisionImage SourceImage = new VisionImage();
        public static VisionImage GlobalImage = new VisionImage();

        public static Vision.LedPro g_ledpro;
        public static Vision.Cameras g_Camera;
        public static int LedChannel;

        public struct myProcessResult { 
            public string machineName;
            public int machineNumber;
        }
        //Epoxy Timer
        public static bool EpoxyExpired = false;
        //Alignment Profile
        public enum Select_profile
        {
            FineFiberAlign,
            FiberAlign,
        }
        public static Select_profile selected_profile;
        public static AlignmentFlow FiberAlign_profile = new AlignmentFlow();
        public static string FiberAlign_Profile_Path = "";
        public static AlignmentFlow FineFiberAlign_profile = new AlignmentFlow();
        public static string FineFiberAlign_Profile_Path = "";
        //Tray info
        public struct LensTrayunits
        {
            public double posx;
            public double posy;
            public double posz;
            public double posu;
            public double posx_align;
            public double posy_align;
            public double posz_align;
            public double posu_align;

            public bool visionOk;
        }
        public static LensTrayunits[,] gLensUnitInfo = new LensTrayunits[4, 8];
        public static int current_port_row = 1;
        public static int current_port_col = 1;
        public static int current_fiberHolder_row = 1;
        public static int current_fiberHolder_col = 1;
        public static int row_direction = -1;
        public static int col_direction = 1;
        public static int current_epoxytest_row = 1;
        public static int current_epoxytest_col = 1;
        public static int current_cleanNeedle_idx = 0;

        //Global Hardware object
        //advantech
        public static Hardware.Motion_Advantech[] g_Advantech = new Hardware.Motion_Advantech[2]; //Card numnber, each card can have 8 or 4 chnls
        public static Hardware.Motion_Advantech_Parameters[] g_AdvSet = new Hardware.Motion_Advantech_Parameters[2];
        //Hiwin
        public static Hardware.Motion_Hiwin[] g_Hiwin = new Hardware.Motion_Hiwin[3]; //X,Y,Z
        public static Hardware.Motion_Hiwin_Parameters[] g_HiwinSet = new Hardware.Motion_Hiwin_Parameters[3];
        //Vision
        public static Vision.Camera.CameraRecipe.CameraRecipe g_CameraTest;
        public static Vision.Cameras[] g_Cameras;
        public static Vision.LedPro g_ledpros;
        public static V2Tech.AutomationApps.VisionCameraStandard g_CameraStandard;
        public static KeyenceBarcodeSR750 keyencebarcode;

        //IO
        public static AdvantechIoFunction[] g_IOAdvantech = new AdvantechIoFunction[1];
        public static IOControl g_IOControl;
        //SMU
        public static KeithleyInstrument.Keithley2601 smu;
        //ui log
        public static ToolStripLabel ref_lblMsg;
        public static RichTextBox ref_txtStatus;
        public static ToolStripButton ref_btnTowerLite;
        public static Utilities.Logger Machinelog;

        public static Hardware.Motion_Galil Galil_Main;
        //public static Hardware.Motion_Galil Galil_Nest;
        //public static Hardware.Motion_Galil Galil_NestU;
        //public static Hardware.Motion_Galil Galil_Common;



        internal static void AbortAll()
        {
            throw new NotImplementedException();
            MessageBox.Show("Iam activated");
        }
    }
    //system
    public class SystemVariable
    {
        //Vision & LED
        public string[] camerasSerial = new string[0];
        public string[] camerasVendor = new string[0];
        public int[] CamtoLED = new int[0];
        public int[] LEDCtrlPorts = new int[0];
        public string[] LEDCtrlVendor = new string[0];

        //Motion 
        public List<int> portNum = new List<int>();
        public List<string> AxisName = new List<string>();
        public List<string> AxisDesc = new List<string>(); //description
        public List<string> MotionCardVendor = new List<string>();
        public List<string> configPath = new List<string>();

        //IO
        public string[] IOBoardID = new string[0];
        public string[] IOVendor = new string[0];
        public int[] IOInputCount = new int[0];
        public int[] IOOutputCount = new int[0];

        //motion
        public string Advprm = "";
        public string Hiwinprm = "";

        //UV
        public int UVAdrr;
        public string UVVendor;

        //Result
        public bool isLogResult;
        public bool isSaveImage;
        public bool isOverlayResult;
        public string ImageFormat;
        public string ResultPath;
        public bool isSaveGraph;
        public bool isCSEnable;
        public bool isCSAutomove;
        public bool isCSCheckStep;
        public string CS_StepNames;
        public bool isCSUploadData;
        public bool isAutoSelectTab;
        //simulation mode enable
        public bool isSimulation;
        public int SimLoop;
        //epoxy timer
        public DateTime EpoxyStartDate;
        public double EpoxyLife;
        //Barcode Addr
        public int BarcodeAdrr;
        //Auto RUN
        public bool AutoRun;
        

    }
    //product
    public class ProductList
    {
        public List<string> productlist = new List<string>();
    }
    public struct ProcessResult
    {
        public string TimeStamp;
        public string Operator;
        public int Unit;
        public string SerialNum;
        public string PartNum;
        public string Barcode_SN;
        public string LD_SN;
        public string Child_ID;
        //public string LD_Unit2;
        //public string LD_Unit3;
        //public string LD_Unit4;
        //public string LD_Unit5;
        //public string LD_Unit6;
        //public string LD_Unit7;
        //public string LD_Unit8;
        public string Wafer_ID;
        public string Result;
        public void ResetData()
        {
            TimeStamp = "";
            Operator = "";
            Unit = -999;
            SerialNum = "";
            PartNum = "";
            Barcode_SN = "";
            LD_SN = "";
            Child_ID = "";
            //LD_Unit1 = "";
            //LD_Unit2 = "";
            //LD_Unit3 = "";
            //LD_Unit4 = "";
            //LD_Unit5 = "";
            //LD_Unit6 = "";
            //LD_Unit7 = "";
            //LD_Unit8 = "";
            Wafer_ID = "";
            Result = "";
        }
    }
    public class ProductSettings
    {
        //tray
        public int SiPh_ColCount;
        public int SiPh_RowCount;
        public double SiPh_Colpitch;
        public double SiPh_Rowpitch;


        public double AngleLimit;
        public double XYZOffsetLimit;


    }
    public class AlignmentFlow
    {
        public enum AlignmentStep
        {
            SearchLight,
            YZ_Axis, //optical XY
            RxRu_Axis,//optical RxRy
            X_Axis, // optical Z
            Y_Axis, // optical X
            Z_Axis, // optical Y
            Rx_Axis, // optical RX
            Ry_Axis, //optical U (PM fiber)
            Rr_Axis, //optical RY
            SyncAlign_2D
        }
        public enum SearchOption
        {
            Square
        }
        public enum AlignmentOption
        {
            Centroid,
            Peak,
            Centroid_Peak,
            Peak_Centroid,
            Centroid_Centroid,
            Peak_Peak,
        }
        public enum AlignmentOption1
        {
            Centroid,
            Peak,
        }
        public enum AlignmentOption2D
        {
            Y_Z,
            Rx_Ru,
        }
        public enum AlignmentOption3D
        {
            X_Y_Z,
        }
        public enum AlignmentOption2
        {
            Centroid_Peak,
            Peak_Centroid,
            Centroid_Centroid,
            Peak_Peak,
        }
        public List<string> Selected_Step = new List<string>();
        public List<string> Selected_Option = new List<string>();
        public List<double> Centroid_Percentage = new List<double>();
        public List<double> ScanSpeed = new List<double>();
        public List<double> ScanWidth = new List<double>();
        public List<double> ScanStepSize = new List<double>();
        public List<double> PowerTH = new List<double>();
        public List<double> PowerLSL = new List<double>();
        public List<double> PowerUSL = new List<double>();
        public List<double> VoltageTH = new List<double>();
        public List<double> MaxIteration = new List<double>();
        public List<double> StdDev = new List<double>();
        public List<int> FitOrder = new List<int>();
        public List<int> spiralCount = new List<int>();
        public List<bool> continous = new List<bool>();
    }
    //vision
    public enum VisionFunctions
    {
        Pattern_Matching,
        Blob,
        Blob_Width,
        Circle,
        Barcode,
        AbsentPresent,
        Straight_Line,
        CustomVMI,
        None,
    };
    public enum ActiveFunctions
    {
        Pattern_Matching,
        Blob,
        Blob_Width,
        Circle,
        Barcode,
        AbsentPresent,
        Straight_Line,
        None,
    };
    public class reference_position
    {
        public double X = 0.0;
        public double Y = 0.0;
        public double Angle = 0.0;
        public bool isRef = false;
    }
    public class VisionVariable
    {
        public double camera_scale;
        public List<string> rois = new List<string>();
        public List<string> VisionName = new List<string>();
        public List<string> VisionType = new List<string>();
        public List<string> VisionTempPath = new List<string>();
        public List<double> Brightness = new List<double>();
        public List<double> Contrast = new List<double>();
        public List<double> Gamma = new List<double>();
        public List<bool> InvertImage = new List<bool>();
        public List<byte> LEDChnl = new List<byte>(); //1,2,4,8 max 16 = 4 chnl
        public List<int> LEDInt = new List<int>();
        public List<int> LEDSWidth = new List<int>();
        public List<string> LedMode = new List<string>();
        public List<int> ThresholdMin = new List<int>();
        public List<int> ThresholdMax = new List<int>();
        public List<double> SpecMin = new List<double>();
        public List<double> SpecMax = new List<double>();
        public List<reference_position> ref_P = new List<reference_position>();
        public List<string> Direction = new List<string>();
        public List<string> SearchMode = new List<string>();
        public List<double> CamGain = new List<double>();
        public List<double> CamShutter = new List<double>();

    }
    public static class VisionPosition
    {
        public enum CamPos
        {
            cam1_Reference,          
        }

        public enum Cam2Pos
        {
            cam2_CaptureSN,
        }

    }

    public enum BarcodePos
    { 
        barcodecapture
    }

    public enum CamSelect
    { 
        cam1,
        cam2,
    }
    public class LedProVariables
    {

        public byte[] strobeIntensity = new byte[4];
        public UInt16[] strobeDelay = new UInt16[4];
        public UInt16[] strobeWidth = new UInt16[4];
        public UInt16[] strobeOutputDelay = new UInt16[4];

        public byte currentMultiplier = new byte();
    }
    //Motion
    public enum MotionGroup
    {
        MainTable,
    }
    public enum AxisID
    {
        X = 0, //card 0 - 8 axis
        Y, //1
        Z, //2
        U, //3
        Rx,
        Ry,
        Rz,
        Ru,
    }
    public enum GalilAxis
    {
        X,
        Y,
        Z,
    }
    public class GantryPos
    {
        public enum locnames
        {
            Homing,
            locTopCam1,
            locTopCam2_LD1_Unit1,//locTopCam2_Barcode_Unit[1]
            locTopCam2_LD1_Unit2,//2
            locTopCam2_LD1_Unit3,//3
            locTopCam2_LD1_Unit4,//4
            locTopCam2_Barcode_Unit1,
            locTopCam2_Barcode_Unit2,
            locTopCam2_Barcode_Unit3,
            locTopCam2_Barcode_Unit4,
            locSafety
        }


        public static void LocalToPublic()
        {
 
        }

        public static int[] locTopCam2_Barcode_Unit = new int[4];

        public List<string> points_label = new List<string>();
        public List<double> points_X = new List<double>();
        public List<double> points_Y = new List<double>();
        public List<double> points_Z = new List<double>();
        public List<double> Speed = new List<double>();
    }

    public class barcode750
    { 
    
    }
}
