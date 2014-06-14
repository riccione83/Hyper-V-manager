using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Globalization;
using System.Threading;

namespace VM1
{
 /*   class Constants
    {
        internal const string DefineVirtualSystem = "DefineVirtualSystem";
        internal const string ModifyVirtualSystem = "ModifyVirtualSystem";
        internal const uint ERROR_SUCCESS = 0;
        internal const uint ERROR_INV_ARGUMENTS = 87;
    }*/

    public class VM
    {
        ManagementObject virtualSystemService = null;
        ManagementScope scope = null;
        List<string> infos = new List<string>();
        public Form1 frm;
        public bool IN_UPGRADE = false;

        private delegate void SetControlCAllback(vm_control ctrl);
        private delegate void ClearControlCallback();

        public void clearControl()
        {
            if (this.frm.InvokeRequired)
            {
                ClearControlCallback stc = new ClearControlCallback(clearControl);
                frm.Invoke(stc);
            }
            else
            {
                frm.UI_VM_CONTAINER.Controls.Clear();
            }
        }

        public void setControl(vm_control ct)
        {
            if (this.frm.InvokeRequired)
            {
                SetControlCAllback stc = new SetControlCAllback(setControl);
                frm.Invoke(stc, new object[] { ct });
            }
            else
            {
                bool Fnd = false;
                vm_control pnl = null;
                for(int i=0;i< frm.UI_VM_CONTAINER.Controls.Count;i++)
                //foreach (System.Windows.Forms.Control pnl in frm.UI_VM_CONTAINER.Controls)   //search between elements in container
                {
                    if (frm.UI_VM_CONTAINER.Controls[i] is vm_control)
                    {
                        pnl = (vm_control)frm.UI_VM_CONTAINER.Controls[i];
                        if (ct.lblName.Text == ((vm_control)pnl).lblName.Text)
                        {
                            ((vm_control)pnl).lblName.Text = ct.lblName.Text;
                            ((vm_control)pnl).lblDescription.Text = ct.lblDescription.Text;
                            ((vm_control)pnl).lblOS.Text = ct.lblOS.Text;
                            ((vm_control)pnl).lblProcessorLoad.Text = ct.lblProcessorLoad.Text;
                            ((vm_control)pnl).lblState.Text = ct.lblState.Text;
                            ((vm_control)pnl).imgVM.Image = ct.imgVM.Image;
                            Fnd = true;
                        }
                    }
                }

                if (!Fnd)
                {
                    if (frm.imgWait.Visible == true)
                        frm.imgWait.Dispose();

                    frm.UI_VM_CONTAINER.Controls.Add(ct);
                }
            }
        }

      /*  private void Output(string text, params object[] args)
        {
            string msg = string.Format(text, args);
            Output(msg);
        }*/


        public VM(Form1 frm)
        {
            this.frm = frm;
        }

        public string getState(string State)
        {
            switch (State)
            {
                case "0": return "Unknown";
                case "2": return "Enabled";
                case "3": return "Disabled";
                case "32768": return "Paused";
                case "32769": return "Suspended";
                case "32770": return "Starting";
                case "32771": return "Snapshotting";
                case "32773": return "Saving";
                case "32774": return "Stopping";
                case "32776": return "Pausing";
                case "32777": return "Resuming";
                default: return "Unknown";
            }
        }

        public ManagementScope getRemoteScope()
        {
            string path = frm.app_path;
            string host = "";
            bool use_auth = false;

            if (File.Exists(path + "\\options.bin"))
            {
                ManagementScope manScope = null;
                ConnectionOptions connOpts = new ConnectionOptions();


                using (StreamReader rd = new StreamReader(path + "\\options.bin"))
                {
                    host = rd.ReadLine();
                    if (!rd.EndOfStream)
                    {
                        connOpts.Username = rd.ReadLine();
                        connOpts.Password = rd.ReadLine();
                        connOpts.Authority = "ntlmdomain:" + rd.ReadLine();
                        use_auth = true;
                    }
                }

                if (host.Contains("localhost") || host.Contains("127.0.0.1"))
                    host = ".";

                if (!use_auth) connOpts = null;

                manScope = new ManagementScope(@"\\" + host + @"\root\virtualization", connOpts);

                return manScope;
            }
            else return null;
        }
   
        public void doAction(string vmName, string action,vm_control vmctrl)
        {
            const int Enabled = 2;
            const int Disabled = 3;
            const int Reboot = 10;
            const int Reset = 11;
            const int Paused = 32768;
            const int Suspended = 32769;

            if (scope == null) scope = getRemoteScope();

            ManagementObject vm = Utility.GetTargetComputer(vmName, scope);

            if (null == vm)
            {
                throw new ArgumentException(
                    string.Format(
                    "The virtual machine '{0}' could not be found.", 
                    vmName));
            }

            ManagementBaseObject inParams = vm.GetMethodParameters("RequestStateChange");

            if (action.ToLower() == "start")
            {
                inParams["RequestedState"] = Enabled;
            }
            else if (action.ToLower() == "stop")
            {
                inParams["RequestedState"] = Disabled;
            }
            else if (action.ToLower() == "reboot")
            {
                inParams["RequestedState"] = Reboot;
            }
            else if (action.ToLower() == "reset")
            {
                inParams["RequestedState"] = Reset;
            }
            else if (action.ToLower() == "paused")
            {
                inParams["RequestedState"] = Paused;
            }
            else if (action.ToLower() == "suspend")
            {
                inParams["RequestedState"] = Suspended;
            }
            else
            {
                throw new Exception("Wrong action is specified");
            }

            ManagementBaseObject outParams = vm.InvokeMethod("RequestStateChange", inParams, null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope,vmctrl))
                {
                    Console.WriteLine("{0} state was changed successfully.", vmName);
                }
                else
                {
                    Console.WriteLine("Failed to change virtual system state");
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                Console.WriteLine("{0} state was changed successfully.",vmName);
            }
            else
            {
                Console.WriteLine("Change virtual system state failed with error {0}",outParams["ReturnValue"]);
            }
        }

        static Image SaveImageData(byte[] imageData, string vmName)
        {
            int x = 100, y = 100;

            Bitmap VMThumbnail = new Bitmap(x, y, PixelFormat.Format16bppRgb565);
            Rectangle rectangle = new Rectangle(0, 0, x, y);
            BitmapData VMThumbnailBiltmapData = VMThumbnail.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);

            System.Runtime.InteropServices.Marshal.Copy(imageData, 0, VMThumbnailBiltmapData.Scan0, x * y * 2);
            VMThumbnail.UnlockBits(VMThumbnailBiltmapData);

            return VMThumbnail;
        }


        public Image GetVirtualSystemThumbnailImage(string vmName)
        {
            if (scope == null) scope = getRemoteScope();

            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemManagementService");

            ManagementObject vm = Utility.GetTargetComputer(vmName, scope);

            ManagementObjectCollection vmsettingDatas = vm.GetRelated(
                "Msvm_VirtualSystemsettingData",
                "Msvm_SettingsDefineState",
                null,
                null,
                "SettingData",
                "ManagedElement",
                false,
                null);

            ManagementObject settingData = null;

            foreach (ManagementObject data in vmsettingDatas)
            {
                settingData = data;
                break;
            }

            if (settingData != null)
            {
                Image ImgTmp;

                ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("GetVirtualSystemThumbnailImage");
                inParams["HeightPixels"] = 100;
                inParams["WidthPixels"] = 100;
                inParams["TargetSystem"] = settingData.Path.Path;


                ManagementBaseObject outParams = virtualSystemService.InvokeMethod("GetVirtualSystemThumbnailImage", inParams, null);

                if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
                {
                    if (Utility.JobCompleted(outParams, scope,null))
                    {
                        ImgTmp = SaveImageData((byte[])outParams["ImageData"], vmName);
                    }
                    else
                    {
                        ImgTmp = null;
                    }
                }
                else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
                {
                    ImgTmp =  SaveImageData((byte[])outParams["ImageData"], vmName);
                }
                else
                {
                    ImgTmp = null;
                }

                inParams.Dispose();
                outParams.Dispose();
                settingData.Dispose();
                vm.Dispose();
                virtualSystemService.Dispose();

                return ImgTmp;
            }
            else
                return null;
        }

        ManagementObject GetVirtualSystemSetting(string vmName)
        {
            if (scope == null) scope = getRemoteScope();
            virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemManagementService");

            ManagementObject virtualSystem = Utility.GetTargetComputer(vmName, scope);

            ManagementObjectCollection virtualSystemSettings = virtualSystem.GetRelated
             (
                 "Msvm_VirtualSystemSettingData",
                 "Msvm_SettingsDefineState",
                 null,
                 null,
                 "SettingData",
                 "ManagedElement",
                 false,
                 null
             );

            ManagementObject virtualSystemSetting = null;

            foreach (ManagementObject instance in virtualSystemSettings)
            {
                virtualSystemSetting = instance;
                break;
            }

            return virtualSystemSetting;

        }

        Dictionary<string,string> GetSummaryInformation(ManagementObject[] virtualSystemSettings, UInt32[] requestedInformation)
        {
            Dictionary<string,string> infos = new Dictionary<string,string>();

            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("GetSummaryInformation");
            string[] settingPaths = new string[virtualSystemSettings.Length];
            for (int i = 0; i < settingPaths.Length; ++i)
            {
                settingPaths[i] = virtualSystemSettings[i].Path.Path;
            }

            inParams["SettingData"] = settingPaths;
            inParams["RequestedInformation"] = requestedInformation;

            ManagementBaseObject outParams = virtualSystemService.InvokeMethod("GetSummaryInformation", inParams, null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                Console.WriteLine("Summary information was retrieved successfully.");

                ManagementBaseObject[] summaryInformationArray = (ManagementBaseObject[])outParams["SummaryInformation"];
                
                infos.Clear();
                foreach (ManagementBaseObject summaryInformation in summaryInformationArray)
                {
                    Console.WriteLine("\nVirtual System Summary Information:");
                    foreach (UInt32 requested in requestedInformation)
                    {
                        switch (requested)
                        {
                            case 4:
                                infos.Add("4", summaryInformation["NumberofProcessors"].ToString());
                                break;
                            case 101:
                                if(summaryInformation["ProcessorLoad"] != null)
                                    infos.Add("101", summaryInformation["ProcessorLoad"].ToString());
                                break;
                            case 103:
                                if (summaryInformation["MemoryUsage"] != null)
                                    infos.Add("103", summaryInformation["MemoryUsage"].ToString());
                                break;
                            case 105:
                                if (summaryInformation["Uptime"] != null)
                                    infos.Add("105", summaryInformation["Uptime"].ToString());
                                break;
                            case 106:
                                if (summaryInformation["GuestOperatingSystem"] != null)
                                     infos.Add("106", summaryInformation["GuestOperatingSystem"].ToString());
                                break;
                            case 109:
                                if (summaryInformation["HealthState"] != null)
                                    infos.Add("109", summaryInformation["HealthState"].ToString());
                                break;
                            case 112:
                                if (summaryInformation["MemoryAvailable"] != null)
                                    infos.Add("112", summaryInformation["MemoryAvailable"].ToString());
                                break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Failed to retrieve virtual system summary information");
            }

            inParams.Dispose();
            outParams.Dispose();

            return infos;
        }

        public Dictionary<string,string> getInfo(string vmName)
        {
            Dictionary<string, string> infos = new Dictionary<string, string>();

            string[] InfoNode =  {"0", "4", "101", "103", "105", "106", "109", "112"};

            //VM getSummaryInfo = new VM();
            ManagementObject virtualSystemSetting = GetVirtualSystemSetting(vmName);
            ManagementObject[] settings = new ManagementObject[1];
            settings[0] = virtualSystemSetting;

            UInt32[] requestedInfo = new UInt32[InfoNode.Length - 1];

            for (int i = 1; i < InfoNode.Length; ++i)
            {
                requestedInfo[i - 1] = UInt32.Parse(InfoNode[i].ToString());
            }
            if(settings[0]!=null)
                infos = GetSummaryInformation(settings, requestedInfo);

            if (virtualSystemSetting !=null) virtualSystemSetting.Dispose();

            return infos;
        }


        public string CreateVirtualSystemSnapshot(string vmName,vm_control vmctrl)
        {
            ManagementBaseObject result=null;
            string ret="";
            const string Job = "Job";
            const string JobState = "JobState";


            if (scope == null) scope = getRemoteScope();

            ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemManagementService");

            ManagementObject vm = Utility.GetTargetComputer(vmName, scope);

            ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("CreateVirtualSystemSnapshot");
            inParams["SourceSystem"] = vm.Path.Path;

            ManagementBaseObject outParams = virtualSystemService.InvokeMethod("CreateVirtualSystemSnapshot", inParams, null);

            if (Utility.JobCompleted(outParams, scope, vmctrl))
            {


                if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
                {
                    if (Utility.JobCompleted(outParams, scope,null))
                    {
                        ret = ("Snapshot was created successfully.");

                    }
                    else
                    {
                        ret = ("Failed to create snapshot VM");
                    }
                }
                else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
                {
                    ret = ("Snapshot was created successfully.");
                }
                else
                {
                    ret = String.Format("Create virtual system snapshot failed with error {0}", outParams["ReturnValue"].ToString());
                }
            }
            inParams.Dispose();
            outParams.Dispose();
            vm.Dispose();
            virtualSystemService.Dispose();

            return ret;
        }

        private ManagementObject GetMsVM_VirtualSystemManagementService()
        {
            return GetWmiObject("MsVM_VirtualSystemManagementService", null);
        }

        private ManagementObject GetMsvm_VirtualSystemSettingData(string vmName)
        {
            return GetWmiObject("Msvm_VirtualSystemSettingData", string.Format("systemname='{0}'", vmName));
        }

        #region Wmi Helpers
        private ManagementObject GetWmiObject(string classname, string where)
        {
            ManagementObjectCollection resultset = GetWmiObjects(classname, where);
            if (resultset.Count != 1)
                throw new InvalidOperationException(string.Format("Cannot locate {0} where {1}", classname, where));
            ManagementObjectCollection.ManagementObjectEnumerator en = resultset.GetEnumerator();
            en.MoveNext();
            ManagementObject result = en.Current as ManagementObject;
            if (result == null)
                throw new InvalidOperationException("Failure retrieving " + classname + " where " + where);
            return result;
        }

        private ManagementObjectCollection GetWmiObjects(string classname, string where)
        {
            string query;

            ConnectionOptions connOpts = new ConnectionOptions();
            connOpts.Username = "RiccardoRizzo";   //user
            connOpts.Authority = "ntlmdomain:" + "2858security";
            connOpts.Password = "laura007?";   //password

            ManagementScope scope = new ManagementScope(@"\\192.168.2.85\root\virtualization", connOpts);

            //  ManagementScope scope = new ManagementScope(@"root\virtualization", null);
            if (where != null)
            {
                query = string.Format(
                   "select * from {0} where {1}",
                   classname,
                   where);
            }
            else
            {
                query = string.Format(
                    CultureInfo.InvariantCulture,
                    "select * from {0}",
                    classname);
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
            ManagementObjectCollection resultset = searcher.Get();
            return resultset;
        }

        #endregion Wmi helpers

        public void getRemoteVM()
        {

            IN_UPGRADE = true;

            ObjectQuery queryObj = new ObjectQuery("SELECT * FROM Msvm_ComputerSystem");

            if (scope == null) scope = getRemoteScope();

            if (scope != null)
            {
                ManagementObjectSearcher vmSearcher = new ManagementObjectSearcher(scope, queryObj);
                ManagementObjectCollection vmCollection = vmSearcher.Get();
                try
                {
                    foreach (ManagementObject vm in vmCollection)
                    {
                        vm_control ctrl = new vm_control();
                        ctrl.frm = frm;
                        ctrl.lblName.Text = vm["ElementName"].ToString();
                        ctrl.lblState.Text = "[" + getState(vm["EnabledState"].ToString()) + "]";
                        ctrl.lblDescription.Text = vm["Description"].ToString();
                        ctrl.imgVM.Image = GetVirtualSystemThumbnailImage(vm["ElementName"].ToString());
                        Dictionary<string, string> infos = getInfo(vm["ElementName"].ToString());
                        if (infos.Count > 0)
                        {
                            if (infos.ContainsKey("106"))
                                ctrl.lblOS.Text = "Sistema: " + infos["106"].ToString();
                            else ctrl.lblOS.Text = "Sistema: na";
                            if (infos.ContainsKey("101"))
                                ctrl.lblProcessorLoad.Text = "CPU: " + infos["101"].ToString() + "%";
                            else ctrl.lblProcessorLoad.Text = "CPU: na";
                        }
                        else
                        {
                            ctrl.lblOS.Text = "Sistema: na";
                            ctrl.lblProcessorLoad.Text = "CPU: na";
                        }

                        this.setControl(ctrl);

                        IN_UPGRADE = false;
                        //  frm.UI_VM_CONTAINER.Controls.Add(ctrl);
                    }
                }
                catch
                {
                    frmMachine opt = new frmMachine();
                    opt.ShowDialog();
                }
            }
            else
            {
                frmMachine opt = new frmMachine();
                opt.ShowDialog();
            }
        }
    }
}
