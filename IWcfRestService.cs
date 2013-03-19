using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.IO;
using System.ServiceModel.Channels;
using fastJSON;
using System.Configuration;
using System.Web.Configuration;
using System.ComponentModel;
using System.Net.Mail;
using System.Net;
using System.Collections.Specialized;
using System.Net.Sockets;

namespace NatureWorksFridgeMonitoring
{
    /// <summary>
    /// A WCF REST Service that is the nexus between the OW-Server, the data store, and the graphical data display.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">   
    /// <item>Deploy this service and its implementation to IIS on a CPU.</item>
    /// <item>Open port TCP 80 in the CPU and in relevant routers.</item>
    /// <item>Setup port forwarding in relevant routers.</item>
    /// <item>Put the CPU into the DMZ if appropriate.</item>
    /// </list>
    /// <list type="bullet">   
    /// <item>Configure OWServer_v2-Enet.</item>
    /// <item>Use the GUI found at http://bowlands.homeserver.com:8081.</item>
    /// <item>Go to System Configuration > POST Client.</item>
    /// <item>Check "Enable Client"</item>
    /// <item>Add http://xx.xx.xx.xxx/postclient.svc/receive-xml-post/v1 to "URL" textbox.</item>
    /// <item>Set a "Period" in seconds.</item>    
    /// </list>
    /// </remarks>
    [ServiceContract]
    public interface IWcfRestService
    {
        /// <summary>
        /// Receives, saves, and timestamps POST data from the OWServer_v2-Enet.
        /// </summary>
        /// <param name="postData">The POST that contains details.xml file from the OWServer_v2-Enet.</param>
        [OperationContract(Name = "ReceiveXmlPost")]
        [WebInvoke(Method = "POST", UriTemplate = "ReceiveXmlPost/v1", RequestFormat = WebMessageFormat.Xml)]
        void ReceiveOneWireXmlPost(Stream postData);
        /// <summary>
        /// Send the temperature persisted temperature data.
        /// </summary>
        /// <returns></returns>
        [OperationContract(Name = "SendFlotDataSet")]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "SendFlotDataSet")]
        FlotDataSet[] SendFlotDataSet();
        /// <summary>
        /// Receive the temperature monitoring system settings and save them as JSON in a text file.
        /// </summary>
        /// <param name="postData">Form POST data encoded as application/x-www-form-urlencoded</param> 
        [OperationContract(Name = "ReceiveSystemSettingsFromUserInterface")]
        [WebInvoke(Method = "POST", UriTemplate = "ReceiveSystemSettingsFromUserInterface", ResponseFormat = WebMessageFormat.Json)]
        SimpleHttpResponseMessage ReceiveSystemSettingsFromUserInterface(Stream postData);
        /// <summary>
        /// SendSystemSettings summary
        /// </summary>        
        [OperationContract(Name = "SendSystemSettingsToUserInterface")]
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        SystemSettings SendSystemSettingsToUserInterface();
        /// <summary>
        /// Send a test email to the specified recipients.
        /// </summary>
        /// <param name="postData">
        /// Comma separated list of email addresses to whom to send the test email.
        /// </param>
        [OperationContract(Name = "SendTestEmail")]
        [WebInvoke(Method = "POST", UriTemplate = "SendTestEmail", ResponseFormat = WebMessageFormat.Json)]
        SimpleHttpResponseMessage SendTestEmail(Stream postData);
    }
    /// <summary>
    /// 
    /// </summary>
    public static class UtcJavascriptTimestampHelper
    {
        /// <summary>
        /// Useful for converting a DateTime into a javascript timestamp,
        /// because Flot works in Javascript timestamps.
        /// </summary>
        private const string UNIX_EPOCH = "1/1/1970";
        private const int ASP_NET_TICKS_PER_JAVASCRIPT_TICK = 10000;
        /// <summary>
        /// 
        /// </summary>
        public static long Now
        {
            get
            {
                long result;
                result = Convert_UtcAspNetDateTime_To_UtcJavascriptTimestamp(DateTime.UtcNow);
                return result;
            }
        }
        /// <summary>
        /// Convert a DateTime into a Javascript Timestamp
        /// </summary>
        /// <param name="input">Any DateTime.</param>
        /// <returns>A JavascriptTimestamp equivalent of the specified DateTime.</returns>
        /// <remarks>
        /// <para>The Flot javascript plugin requires a Javascript Timestamp.</para>
        /// <para>A Javascript timestamp is the number of milliseconds since January 1, 1970 00:00:00 UTC.
        /// This is almost the same as Unix timestamps, except it's in milliseconds, so remember to multiply by 1000!</para>
        /// </remarks>
        public static long Convert_UtcAspNetDateTime_To_UtcJavascriptTimestamp(System.DateTime input)
        {
            // There are 10,000 ticks in a millisecond. 
            long inputInTicks;
            long unixEpochInTicks;
            long inputInTicksMinusUnixEpochInTicks;
            long inputAsJavascriptTimestamp;

            inputInTicks = input.Ticks;
            unixEpochInTicks = System.DateTime.Parse(UNIX_EPOCH).Ticks;
            inputInTicksMinusUnixEpochInTicks = inputInTicks - unixEpochInTicks;
            inputAsJavascriptTimestamp = inputInTicksMinusUnixEpochInTicks / ASP_NET_TICKS_PER_JAVASCRIPT_TICK;

            return inputAsJavascriptTimestamp;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="utcJavascriptTimestamp"></param>
        /// <returns></returns>
        public static DateTime Convert_UtcJavascriptTimestamp_To_UtcAspNetDateTime(long utcJavascriptTimestamp)
        {
            // There are 10,000 ticks in a millisecond. 
            long inputInTicks;
            TimeSpan inputAsTimespan;
            DateTime unixEpochAsDateTime;
            DateTime inputAsDateTime;

            inputInTicks = utcJavascriptTimestamp * ASP_NET_TICKS_PER_JAVASCRIPT_TICK;
            inputAsTimespan = new TimeSpan(inputInTicks);
            unixEpochAsDateTime = DateTime.Parse(UNIX_EPOCH);
            inputAsDateTime = unixEpochAsDateTime.Add(inputAsTimespan);

            return inputAsDateTime;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="javascriptTicks">Equivalent to millseconds.</param>
        /// <returns></returns>
        public static TimeSpan ConvertJavascriptTicks_To_AspNetTimeSpan(long javascriptTicks)
        {
            TimeSpan timespan;
            int aspNetTickjavascriptTicksPerJavascriptTicks = ASP_NET_TICKS_PER_JAVASCRIPT_TICK;
            timespan = new TimeSpan(javascriptTicks * aspNetTickjavascriptTicksPerJavascriptTicks);
            return timespan;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="utcJavascriptTimestamp"></param>
        /// <returns></returns>
        public static DateTime Convert_UtcJavascriptTimestampTo_LocalAspNetDateTime(long utcJavascriptTimestamp)
        {
            DateTime utcDateTime, localAspNetDateTime;
            int timeZoneOffset;
            timeZoneOffset = Convert.ToInt16(WebConfigurationManager.AppSettings["timezone_offset"]);
            utcDateTime = Convert_UtcJavascriptTimestamp_To_UtcAspNetDateTime(utcJavascriptTimestamp);
            localAspNetDateTime = utcDateTime.AddHours(timeZoneOffset);
            return localAspNetDateTime;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static DateTime Get_LocalAspNetDateTime()
        {
            DateTime utcAspNetDateTime, localAspNetDateTime;
            int timeZoneOffset;
            timeZoneOffset = Convert.ToInt16(WebConfigurationManager.AppSettings["timezone_offset"]);
            utcAspNetDateTime = DateTime.UtcNow;
            localAspNetDateTime = utcAspNetDateTime.AddHours(timeZoneOffset);
            return localAspNetDateTime;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="utcJavascriptTimestamp"></param>
        /// <returns></returns>
        public static double HowManyDaysOldIsThisUtcJavascriptTimestamp(long utcJavascriptTimestamp)
        {
            long javascriptTicksOld;
            double daysOld;
            javascriptTicksOld = UtcJavascriptTimestampHelper.Now - utcJavascriptTimestamp;
            daysOld = UtcJavascriptTimestampHelper.ConvertJavascriptTicks_To_AspNetTimeSpan(javascriptTicksOld).TotalDays;
            return daysOld;
        }
    }
    #region Device and System Settings
    /// <summary>
    /// Settings that apply to devices.
    /// </summary>
    [DataContract]
    public class DeviceSettings
    {
        /// <summary>
        /// 
        /// </summary>        
        public const int SYSTEM_SETTINGS_DEVICE_DATA_COUNT = 2;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string FriendlyName;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int TemperatureThreshold;
        /// <summary>
        /// 
        /// </summary>
        public DeviceSettings()
        {

        }
    }
    /// <summary>
    /// Settings that apply to the Fridge Monitoring system, which includes a dictionary of device specific settings.
    /// </summary>
    [DataContract]
    public class SystemSettings
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "DataStoreDurationInDays")]
        public int DataStoreDurationInDays;
        /// <summary>
        /// Comma separated list of email addresses that will receive warning emails.
        /// </summary>
        [DataMember(Name = "WarningEmailRecipientsInCsv")]
        public string WarningEmailRecipientsInCsv;
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "HoursThatMustPassBetweenSendingDeviceSpecificWarningEmails")]
        public double HoursThatMustPassBetweenSendingDeviceSpecificWarningEmails;
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "DeviceSettingsDictionary")]
        public Dictionary<string, DeviceSettings> DeviceSettingsDictionary;
        /// <summary>
        /// 
        /// </summary>
        public string SystemSettingsPhysicalPath
        {
            get
            {
                string appPhysicalPath;
                string dataStoreDirectory;
                string systemSettingsFileName;
                string systemSettingsPhysicalPath;

                appPhysicalPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
                dataStoreDirectory = WebConfigurationManager.AppSettings["data_store_directory"];
                systemSettingsFileName = WebConfigurationManager.AppSettings["system_settings_file_name"];
                systemSettingsPhysicalPath = Path.Combine(appPhysicalPath, dataStoreDirectory, systemSettingsFileName);

                return systemSettingsPhysicalPath;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public SystemSettings()
        {
            DeviceSettingsDictionary = new Dictionary<string, DeviceSettings>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fillFromDataStore"></param>
        public SystemSettings(bool fillFromDataStore)
        {
            if (fillFromDataStore)
            {
                FillFromDataStore();
            }
            else
            {
                DeviceSettingsDictionary = new Dictionary<string, DeviceSettings>();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            string json;
            fastJSON.JSONParameters fastJSONParameters = new fastJSON.JSONParameters();
            fastJSONParameters.UsingGlobalTypes = true;
            json = fastJSON.JSON.Instance.ToJSON(this, fastJSONParameters);
            json = fastJSON.JSON.Instance.Beautify(json);
            using (StreamWriter outfile = new StreamWriter(SystemSettingsPhysicalPath, false))
            {
                outfile.Write(json);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ConvertDeviceRomIDToFriendlyName(string deviceRomID)
        {
            string friendlyName;
            friendlyName = this.DeviceSettingsDictionary[deviceRomID].FriendlyName;
            return friendlyName;
        }
        /// <summary>
        /// 
        /// </summary>
        private void FillFromDataStore()
        {
            SystemSettings systemSettings;

            // retrieve the system settings from the data store
            string json;
            json = String.Empty;
            try
            {
                using (StreamReader sr = new StreamReader(SystemSettingsPhysicalPath))
                {
                    json = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
            }

            // parse the json string into a systemSettings obj
            if (json.Length > 0)
            {
                systemSettings = (SystemSettings)JSON.Instance.ToObject(json, typeof(SystemSettings));

                this.DataStoreDurationInDays = systemSettings.DataStoreDurationInDays;
                this.DeviceSettingsDictionary = systemSettings.DeviceSettingsDictionary;
                this.HoursThatMustPassBetweenSendingDeviceSpecificWarningEmails = systemSettings.HoursThatMustPassBetweenSendingDeviceSpecificWarningEmails;
                this.WarningEmailRecipientsInCsv = systemSettings.WarningEmailRecipientsInCsv;
            }
            else
            {
                this.DataStoreDurationInDays = int.MaxValue;
                this.DeviceSettingsDictionary = new Dictionary<string, DeviceSettings>();
                this.HoursThatMustPassBetweenSendingDeviceSpecificWarningEmails = int.MinValue;
                this.WarningEmailRecipientsInCsv = string.Empty;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="romID"></param>
        /// <returns></returns>
        internal DeviceSettings GetDeviceSettingsByRomID(string romID)
        {
            DeviceSettings result;
            if (this.DeviceSettingsDictionary.ContainsKey(romID))
            {
                result = this.DeviceSettingsDictionary[romID];
            }
            else
            {
                result = new DeviceSettings();
                result.FriendlyName = GenerateDeviceFriendlyName();
                this.DeviceSettingsDictionary.Add(romID, result);
                this.Save();
            }
            return result;
        }
        private string GenerateDeviceFriendlyName()
        {
            StringBuilder builder;
            builder = new StringBuilder("FriendlyName");
            int i = 0;
            while (this.DeviceSettingsDictionary.Values.Any<DeviceSettings>(d => d.FriendlyName.Equals(builder.ToString())))
            {
                builder.Replace(i.ToString(), string.Empty);
                ++i;
                builder.Append(i.ToString());
            }
            return builder.ToString();
        }
    }
    #endregion
    #region Flot DataSet and DataTable
    /// <summary>
    /// A set of data that Flot can plot without further manipulation.
    /// </summary>
    [CollectionDataContract]
    public class FlotDataSet : List<FlotDataTable>
    {
        public string PhysicalSavePath
        {
            get
            {
                string appPhysicalPath;
                string dataStoreDirectory;
                string fileName;
                string physicalPath;

                appPhysicalPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
                dataStoreDirectory = WebConfigurationManager.AppSettings["data_store_directory"];
                fileName = WebConfigurationManager.AppSettings["flot_data_set_file_name"];
                physicalPath = Path.Combine(appPhysicalPath, dataStoreDirectory, fileName);

                return physicalPath;
            }
        }
        /// <summary>
        /// CollectionDataContractAttribute attribute requires a default constructor
        /// </summary>
        public FlotDataSet()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fillFromDataStore">If true, then fill the FlotDataSet from saved data. This is the recommended value. If false, then instantiate an empty FlotDataSet</param>
        /// <param name="doHighResolution">If true, then return more data points per FlotDataTable. If false, return less data points per FlotDataTable.</param>        
        public FlotDataSet(bool fillFromDataStore, bool doHighResolution)
        {
            List<FlotDataTable> savedData;
            double chartResolutionInMinutes_lowResolution;

            if (fillFromDataStore)
            {
                savedData = GetSavedData();
                chartResolutionInMinutes_lowResolution = Convert.ToDouble(WebConfigurationManager.AppSettings["chart_resolution_in_minutes_low_resolution"]);  
                foreach (FlotDataTable t in savedData)
                { 
                    if (!doHighResolution)
                    {
                        t.DownSampleData(chartResolutionInMinutes_lowResolution);
                    }
                    t.SetMaxAndMinTemperatures();
                }
                this.AddRange(savedData);
            }            
        }
        public void RecompleTheIndividualDataTableFilesIntoOneLargeDataSetFileIfEnoughTimeHasPassed(bool doHighResolution)
        {
            double chartResolutionInMinutes_highResolution; // for faster browsers
            double chartResolutionInMinutes_lowResolution; // for slow browsers

            string[] flotDataTablePhysicalPaths;
            string oneWireDeviceRomID;
            FlotDataTable flotDataTable;

            if (EnoughtTimeHasPassedSinceLastWeRefreshedTheFlotDataSet())
            {
                // get the max chart resolution in minutes from web.config 
                chartResolutionInMinutes_highResolution = Convert.ToDouble(WebConfigurationManager.AppSettings["chart_resolution_in_minutes_high_resolution"]);
                chartResolutionInMinutes_lowResolution = Convert.ToDouble(WebConfigurationManager.AppSettings["chart_resolution_in_minutes_low_resolution"]);

                flotDataTablePhysicalPaths = GetThePhysicalPathsOfAllTheFlotDataTableFiles();

                // for each One Wire device                
                foreach (string physicalPath in flotDataTablePhysicalPaths)
                {
                    // 1 get its romID 
                    oneWireDeviceRomID = FlotDataTable.GetRomIDFromPhysicalSavePath(physicalPath);

                    // 2 create a new FlotDataTable...
                    flotDataTable = new FlotDataTable(oneWireDeviceRomID);

                    // 3 downsample the data...
                    if (doHighResolution)
                    {
                        // ...to high resolution
                        flotDataTable.DownSampleData(chartResolutionInMinutes_highResolution);
                    }
                    else
                    {
                        // ...or to low resolution
                        flotDataTable.DownSampleData(chartResolutionInMinutes_lowResolution);
                    }

                    // 4 add the data table to the data set
                    this.Add(flotDataTable);

                    // save the data set!!!
                    Save();
                }
            }
        }
        public double GetMinutesSinceLastWeRefreshedTheFlotDataSet()
        {
            FileInfo fileInfo;
            TimeSpan timespan;
            double minutes;
            if (File.Exists(PhysicalSavePath))
            {
                fileInfo = new FileInfo(PhysicalSavePath);
                // use UTC to be consistent
                timespan = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;
                minutes = timespan.TotalMinutes;                                                
            }
            else
            {
                // use max value, because if the file doesn't exist, then the duration since last save is infinite, kinda.
                minutes = double.MaxValue;
            }

            return minutes;
        }
        private bool EnoughtTimeHasPassedSinceLastWeRefreshedTheFlotDataSet()
        {
            double flotDataSetTargetRefreshInterval;
            double minutesSinceLastRefresh = GetMinutesSinceLastWeRefreshedTheFlotDataSet();
            bool enoughTimeHasPassed;
            double.TryParse(WebConfigurationManager.AppSettings["flot_data_set_refresh_interval_in_minutes"], out flotDataSetTargetRefreshInterval);
            enoughTimeHasPassed = false;
            try
            {
                enoughTimeHasPassed = minutesSinceLastRefresh > flotDataSetTargetRefreshInterval;
            }
            catch (Exception ex)
            {

            }
            return enoughTimeHasPassed;
        }
        public FlotDataSet GetSavedData()
        {
            string json;
            FlotDataTable flotDataTable;
            FlotDataSet flotDataSet;
            List<object> jsonParseResults;

            // read the json data from the flat file
            json = String.Empty;
            try
            {
                using (StreamReader sr = new StreamReader(PhysicalSavePath))
                {
                    json = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
            }

            // instantiate an empty flotDataSet that we will fill if there is saved data
            flotDataSet = new FlotDataSet(false, false);

            // if the json string has data, then fill the emtpy entry log 
            if (json.Length > 0)
            {
                jsonParseResults = JSON.Instance.Parse(json) as List<object>;
                foreach (object o in jsonParseResults)
                {
                    int i = 0;
                    Dictionary<string, object> dictionary;
                    object dataAsObject;
                    string dataAsRawJson;
                    List<List<decimal>> dataAsList;
                    object labelAsObject;
                    string labelAsString;

                    dictionary = o as Dictionary<string, object>;
                    dictionary.TryGetValue("Data", out dataAsObject);
                    dataAsRawJson = JSON.Instance.ToJSON(dataAsObject);
                    dataAsList = FlotDataTable.ConvertRawFastJsonStringIntoFlotData(dataAsRawJson);

                    dictionary.TryGetValue("Label", out labelAsObject);
                    labelAsString = labelAsObject as string;

                    flotDataTable = new FlotDataTable(labelAsString, dataAsList);                    

                    flotDataSet.Add(flotDataTable);

                    ++i;
                }
            }

            return flotDataSet;
        }
        public void Save()
        {
            string json;

            // save the file to that path
            json = JSON.Instance.ToJSON(this);
            using (StreamWriter outfile = new StreamWriter(PhysicalSavePath, false))
            {
                outfile.Write(json);
            }
        }
        public string[] GetThePhysicalPathsOfAllTheFlotDataTableFiles()
        {
            string appPhysicalPath;
            string dataStoreDirectory;
            string dataStorePhysicalPath;
            string[] physicalPaths;

            // get the data store directory physical path from web.config
            appPhysicalPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            dataStoreDirectory = WebConfigurationManager.AppSettings["data_store_directory"];
            dataStorePhysicalPath = Path.Combine(appPhysicalPath, dataStoreDirectory);

            // get all the files within the data store directory that begin with the "romID"                                
            // each of theses files represents data for a single "One Wire" device (such as a thermometer)
            physicalPaths = Directory.GetFiles(dataStorePhysicalPath, "romID_*");

            return physicalPaths;
        }
    }
    /// <summary>
    /// A table of data that Flot can plot.
    /// </summary>
    [DataContract]
    public class FlotDataTable
    {
        /// <summary>
        /// The physical save path for this FlotDataTable. 
        /// </summary>
        public string PhysicalSavePath;
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "label")]
        public string Label;
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "data")]
        public List<List<decimal>> Data;
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "maxTemperature")]
        public decimal MaxTemperature;
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "minTemperature")]
        public decimal MinTemperature;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="label"></param>
        /// <param name="data"></param>
        public FlotDataTable(string label, List<List<decimal>> data)
        {
            this.Label = label;
            this.Data = data;
        }
        /// <summary>
        /// Looks for a data file at the specified physicalSavePath.
        /// If the data file exists, 
        /// then instantiates a FlotDataTable from the data,
        /// else instantiates an brand new FlotDataTable.
        /// </summary>
        /// <param name="physicalSavePath">The full path to the saved data.</param>
        public FlotDataTable(string romID)
        {
            string appPhysicalPath;
            string dataStoreDirectory;
            string physicalSavePath;
            string rawJson;

            // instantiate the PhysicalSavePath
            appPhysicalPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            dataStoreDirectory = WebConfigurationManager.AppSettings["data_store_directory"];
            physicalSavePath = Path.Combine(appPhysicalPath, dataStoreDirectory, ("romID_" + romID + ".txt"));
            this.PhysicalSavePath = physicalSavePath;

            // populate the flot data table data
            this.Data = new List<List<decimal>>();
            try
            {
                // do we have saved data for this One Wire device
                if (File.Exists(physicalSavePath))
                {
                    // yup, the we have saved data, so populate the Data field with the saved data
                    using (StreamReader reader = new StreamReader(physicalSavePath))
                    {
                        // read the data file
                        rawJson = reader.ReadToEnd();

                        // assign to Data field
                        this.Data = ConvertRawFastJsonStringIntoFlotData(rawJson);
                    }
                }
                else
                {
                    // nope, file does not exist, so populate the Data field with an empty list
                    this.Data = new List<List<decimal>>();
                }

                // populate the flot data table label
                romID = GetRomIDFromPhysicalSavePath();
                SystemSettings systemSettings = new SystemSettings(true);
                this.Label = systemSettings.ConvertDeviceRomIDToFriendlyName(romID);
            }
            catch (Exception ex)
            {

            }

        }
        /// <summary>
        /// Append a new data point to the FlotDataTable.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void AppendDataPoint(decimal x, decimal y)
        {
            List<decimal> dataPoint = new List<decimal>() { x, y };
            this.Data.Add(dataPoint);
        }
        /// <summary>
        /// Down sample the data to the specified maximum resolution
        /// </summary>
        /// <param name="maximumResolutionMinutes"></param>
        public void DownSampleData(double maximumResolutionMinutes)
        {
            long javascriptTimestamp;
            DateTime datetime1, datetime2;
            double differenceInMinutes;

            // for now, store the previous time at which we looked as an aribtrarily small DateTime
            datetime2 = DateTime.MinValue;

            // reverse iterate through all the data points
            // stackoverflow.com/questions/1582285/how-to-remove-elements-from-a-generic-list-while-iterating-over-it
            for (int i = this.Data.Count - 1; i >= 0; --i)
            {
                // get the javascriptTimestamp for the data point,
                // convert it to a DateTime object, and
                // find the absolute difference between it and the previous data point
                javascriptTimestamp = (long)this.Data[i][0];
                datetime1 = UtcJavascriptTimestampHelper.Convert_UtcJavascriptTimestamp_To_UtcAspNetDateTime(javascriptTimestamp);
                differenceInMinutes = Math.Abs(datetime1.Subtract(datetime2).TotalMinutes);
                // compare the difference to the max resolution
                if (differenceInMinutes <= maximumResolutionMinutes)
                {
                    // if it's less than the max resolution in minutes, then just remove it                    
                    this.Data.RemoveAt(i);
                }
                else
                {
                    // if not, then keep it and update datetime2
                    datetime2 = datetime1;
                }
            }
        }
        /// <summary>
        /// Deletes stale data from the FlotDataTable.
        /// Remember to call Save() afterward if you want to persist the data.
        /// </summary>
        public void DeleteStaleData()
        {
            long javaScriptTimestamp_item;
            double daysOld, daysToKeepData;

            SystemSettings systemSettings = new SystemSettings(true);
            daysToKeepData = systemSettings.DataStoreDurationInDays;

            for (int i = this.Data.Count - 1; i >= 0; --i)
            {
                javaScriptTimestamp_item = (long)this.Data[i][0]; // decimal to long conversion
                daysOld = UtcJavascriptTimestampHelper.HowManyDaysOldIsThisUtcJavascriptTimestamp(javaScriptTimestamp_item);

                if (daysOld > daysToKeepData)
                {
                    // data is stale so remove it
                    this.Data.RemoveAt(i);
                }
            }
        }
        /// <summary>
        /// Parse the physical save path of the current FlotDataTable and return its RomID.
        /// </summary>
        /// <returns>The RomID of the device which this FlotDataTable represents.</returns>
        private string GetRomIDFromPhysicalSavePath()
        {
            string romID;
            romID = GetRomIDFromPhysicalSavePath(this.PhysicalSavePath);
            return romID;
        }
        /// <summary>
        /// Parse the physical save path of the FlotDataTable and return its RomID.
        /// </summary>
        /// <returns>The RomID of the device which this FlotDataTable represents.</returns>
        public static string GetRomIDFromPhysicalSavePath(string physicalSavePath)
        {
            string fileName;
            string romID;

            // eg. romID_4500000325F85B28
            fileName = Path.GetFileNameWithoutExtension(physicalSavePath);
            // eg. 4500000325F85B28
            romID = fileName.Replace("romID_", string.Empty);

            return romID;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static List<List<decimal>> ConvertRawFastJsonStringIntoFlotData(string rawJson)
        {
            List<object> list_ofObjects;
            List<List<object>> jaggedList_ofObjects;
            List<decimal> list_ofDecimal;
            List<List<decimal>> jaggedList_ofDecimal;

            // get the parse results as a list of type object          
            list_ofObjects = JSON.Instance.Parse(rawJson) as List<object>;

            // convert it into a jagged list of objects
            jaggedList_ofObjects = list_ofObjects.ConvertAll(listItem => (List<object>)listItem);

            // convert further still into a jagged list of decimals
            jaggedList_ofDecimal = new List<List<decimal>>();
            foreach (List<object> list in jaggedList_ofObjects)
            {
                list_ofDecimal = list.ConvertAll(listItem => Convert.ToDecimal(listItem));
                jaggedList_ofDecimal.Add(list_ofDecimal);
            }
            return jaggedList_ofDecimal;
        }
        /// <summary>
        /// Save the FlotDataTable
        /// </summary>
        public void Save()
        {
            string json;
            json = JSON.Instance.ToJSON(this.Data);
            using (StreamWriter outfile = new StreamWriter(PhysicalSavePath, false))
            {
                outfile.Write(json);
            }
        }        
        public void SetMaxAndMinTemperatures()
        {
            decimal temperature;
            this.MinTemperature = decimal.MaxValue;
            this.MaxTemperature = decimal.MinValue;
            foreach (List<decimal> c in this.Data)
            {
                temperature = c[1];
                if (temperature > this.MaxTemperature)
                {
                    this.MaxTemperature = temperature;
                }
                if (temperature < this.MinTemperature)
                {
                    this.MinTemperature = temperature;
                }
            }   
        }
    }
    #endregion
    /// <summary>
    /// 
    /// </summary>
    public class SimpleHttpResponseMessage
    {
        public string Message;
    }
    /// <summary>
    /// 
    /// </summary>
    public class EventLog : List<EventLogEntry>
    {
        /// <summary>
        /// 
        /// </summary>
        public virtual string PhysicalSavePath { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            string json;
            json = JSON.Instance.ToJSON(this);
            json = JSON.Instance.Beautify(json);
            using (StreamWriter outfile = new StreamWriter(PhysicalSavePath, false))
            {
                outfile.Write(json);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void DeleteStaleData()
        {
            long javaScriptTimestamp_item;
            double daysOld, daysToKeepData;

            daysToKeepData = Convert.ToDouble(WebConfigurationManager.AppSettings["days_to_keep_log_file_records"]);

            for (int i = this.Count - 1; i >= 0; --i)
            {
                javaScriptTimestamp_item = (long)this[i].UtcJavascriptTimestamp; // decimal to long conversion
                daysOld = UtcJavascriptTimestampHelper.HowManyDaysOldIsThisUtcJavascriptTimestamp(javaScriptTimestamp_item);
                if (daysOld > daysToKeepData)
                {
                    // data is stale so remove it
                    this.RemoveAt(i);
                }
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class EventLogEntry
    {
        /// <summary>
        /// 
        /// </summary>
        public long UtcJavascriptTimestamp;
        public string FriendlyLocalTimestamp;
        public EventLogEntry()
        {
            this.UtcJavascriptTimestamp = UtcJavascriptTimestampHelper.Now;
            this.FriendlyLocalTimestamp = UtcJavascriptTimestampHelper.Get_LocalAspNetDateTime().ToString("g");
        }
    }
    #region Emailer
    /// <summary>
    /// 
    /// </summary>
    public class Emailer
    {
        /// <summary>
        /// 
        /// </summary>
        private MailAddress FromMailAddress
        {
            get
            {
                string emailFromAddress;
                string emailFromName;
                MailAddress from;

                emailFromAddress = WebConfigurationManager.AppSettings["email_from_address"]; ;
                emailFromName = WebConfigurationManager.AppSettings["email_from_name"]; ;

                from = new MailAddress(emailFromAddress, emailFromName);
                return from;
            }
        }
        /// <summary>
        /// Make a record that we have sent a warning email.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The e.UserToken is a deviceFriendlyName for the device associated with the email.</param>
        private void EmailerSendCompleteEventHandler(object sender, AsyncCompletedEventArgs e)
        {
            string userState;
            EmailLog emailLog;

            // get the data that we want to log
            userState = e.UserState.ToString();

            try
            {
                // get the email log
                emailLog = new EmailLog();

                // delete stale data
                emailLog.DeleteStaleData();

                // add
                emailLog.Add(userState);

                // save
                emailLog.Save();
            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// Send an email to the administrators. 
        /// </summary>
        /// <param name="romID">The device name.</param>        
        /// <param name="temperatureCelsius">The device temperature in celsius.</param>
        public void SendWarningEmail(string romID, decimal temperatureCelsius, decimal javascriptUtcTimestamp)
        {
            StringBuilder body;
            StringBuilder subject;
            SmtpClient client;
            MailMessage emailMessage;
            string deviceFriendlyName;
            string friendlyTimestamp;
            SystemSettings systemSettings;
            string serviceHostUrl;
            string smpt_host;
            string smpt_username;
            string smtp_password;

            // check whether we've already sent too many emails or not.
            if (HasSystemReachedWarningEmailFrequencyLimit(romID))
            {
                return;
            }

            systemSettings = new SystemSettings(true);
            deviceFriendlyName = systemSettings.ConvertDeviceRomIDToFriendlyName(romID);
            friendlyTimestamp = UtcJavascriptTimestampHelper.Convert_UtcJavascriptTimestampTo_LocalAspNetDateTime(Convert.ToInt64(javascriptUtcTimestamp)).ToString("g");
            serviceHostUrl = GetHostUrl();

            // build body       
            body = new StringBuilder();
            body.Append("<dl>");
            body.AppendFormat("<o><strong>Device Name</strong></o><dd>{0}</dd>", deviceFriendlyName);
            body.AppendFormat("<o><strong>Timestamp</strong></o><dd>{0}</dd>", friendlyTimestamp);
            body.AppendFormat("<o><strong>Temperature Celsius</strong></o><dd>{0}</dd>", temperatureCelsius);
            body.Append("</dl>");

            body.Append("<p>");
            body.Append("One of the Nature Works fridges has exceeded its temperature threshold.");
            body.Append("</p>");

            body.AppendFormat("<p><a href='{0}/index.htm'>Nature Works Fridge Monitoring System v.2012.12.26-1</a></p>", serviceHostUrl);

            // build subject
            subject = new StringBuilder();
            subject.Append(deviceFriendlyName);
            subject.Append(": ");
            subject.Append(temperatureCelsius);
            subject.Append(" C");

            // create message
            emailMessage = new MailMessage();
            emailMessage.To.Add(systemSettings.WarningEmailRecipientsInCsv);
            emailMessage.Bcc.Add(WebConfigurationManager.AppSettings["bcc_address_csv"]); // send to admin also
            emailMessage.From = FromMailAddress;
            emailMessage.Subject = subject.ToString();
            emailMessage.Body = body.ToString();
            emailMessage.IsBodyHtml = true;

            // send message            
            smpt_host = WebConfigurationManager.AppSettings["email_smtp_host"];
            smpt_username = WebConfigurationManager.AppSettings["email_smpt_username"];
            smtp_password = WebConfigurationManager.AppSettings["email_smpt_password"];
            client = new SmtpClient(smpt_host);

            client.Credentials = new NetworkCredential(smpt_username, smtp_password);
            client.SendCompleted += new SendCompletedEventHandler(EmailerSendCompleteEventHandler);
            client.SendAsync(emailMessage, string.Format(romID));
        }
        /// <summary>
        /// Limit the number of warning emails that we send to prevent inbox flooding.
        /// </summary>
        /// <remarks>
        /// The system is allowed to send no more than one email per RomID per duration.   
        /// </remarks>
        /// <returns>true if system has reached limit</returns>
        private bool HasSystemReachedWarningEmailFrequencyLimit(string romID)
        {
            DateTime theTimeThatWeSentTheLastEmailForThisRomID;
            double hoursThatHavePassedSinceSendingTheLastEmail;
            bool itIsTooSoonToSendAnotherEmailForThisDevice;
            EmailLog warningEmailLog;

            // we assume it is not too soon
            itIsTooSoonToSendAnotherEmailForThisDevice = false;

            // iterature through all the emails that we've sent
            warningEmailLog = new EmailLog();

            foreach (EmailLogEntry deviceEmailEntry in warningEmailLog)
            {
                // if the romID matches
                if (deviceEmailEntry.RomID != null && deviceEmailEntry.RomID.Contains(romID))
                {
                    // then get the time that we sent the email
                    theTimeThatWeSentTheLastEmailForThisRomID = UtcJavascriptTimestampHelper.Convert_UtcJavascriptTimestamp_To_UtcAspNetDateTime(deviceEmailEntry.UtcJavascriptTimestamp);

                    hoursThatHavePassedSinceSendingTheLastEmail = DateTime.UtcNow.Subtract(theTimeThatWeSentTheLastEmailForThisRomID).TotalHours;

                    // if it was less than a constant number of hours ago
                    SystemSettings systemSettings = new SystemSettings(true);

                    if (hoursThatHavePassedSinceSendingTheLastEmail < systemSettings.HoursThatMustPassBetweenSendingDeviceSpecificWarningEmails)
                    {
                        // then it's too soon.
                        itIsTooSoonToSendAnotherEmailForThisDevice = true;
                    }
                }
            }

            // inform the calling code
            return itIsTooSoonToSendAnotherEmailForThisDevice;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="toAddressesCsv"></param>
        public void SendTestEmail(string toAddressesCsv)
        {
            StringBuilder body;
            StringBuilder subject;
            SmtpClient client;
            MailMessage message;
            string serviceHostUrl;
            string smpt_host;
            string smpt_username;
            string smtp_password;

            serviceHostUrl = GetHostUrl();

            // build body       
            body = new StringBuilder();
            body.Append("<p>Good. You can receive emails from NW Fridge Monitoring.</p>");
            body.Append("<p>Warning emails are automated; as a result, they may land in your Spam.</p>");
            body.Append("<p>If this happens, please take these steps:</p>");
            body.Append("<ul>");
            body.Append("<li>Add shaun@bigfont.ca to your Contacts list.</li>");
            body.Append("<li>Create a filter so messages from shaun@bigfont.ca are never sent to Spam.</li>");
            body.Append("</ul>");
            body.Append("<p>That will ensure you continue to receive warning emails.</p>");
            body.AppendFormat("<p><a href='{0}/index.htm'>Nature Works Fridge Monitoring System v.2012.12.26-1</a></p>", serviceHostUrl);

            // build subject
            subject = new StringBuilder();
            subject.Append("NW Test Email");

            // create message
            message = new MailMessage();
            message.To.Add(toAddressesCsv);
            message.Bcc.Add(WebConfigurationManager.AppSettings["bcc_address_csv"]); // send to admin also
            message.From = FromMailAddress;
            message.Subject = subject.ToString();
            message.Body = body.ToString();
            message.IsBodyHtml = true;

            foreach (MailAddress a in message.To)
            {
                // WriteToSimpleErrorLog(new Exception(a.Address.ToString()), true);
            }

            // send message                       
            // do nothing on complete                            
            smpt_host = WebConfigurationManager.AppSettings["email_smtp_host"];
            smpt_username = WebConfigurationManager.AppSettings["email_smpt_username"];
            smtp_password = WebConfigurationManager.AppSettings["email_smpt_password"];
            client = new SmtpClient(smpt_host);
            client.Credentials = new NetworkCredential(smpt_username, smtp_password);
            client.SendCompleted += new SendCompletedEventHandler(EmailerSendCompleteEventHandler);
            client.SendAsync(message, string.Format("SendTestEmail to {0}", toAddressesCsv));

        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GetHostUrl()
        {
            string host;
            string serviceHostUrl;
            host = WebOperationContext.Current.IncomingRequest.Headers[HttpRequestHeader.Host];
            serviceHostUrl = "http://" + host;
            return serviceHostUrl;
        }      
    }
    /// <summary>
    /// 
    /// </summary>
    public class EmailLog : EventLog
    {
        /// <summary>
        /// 
        /// </summary>
        public override string PhysicalSavePath
        {
            get
            {
                string appPhysicalPath;
                string dataStoreDirectory;
                string emailLogFileName;
                string emailLogPhysicalPath;

                appPhysicalPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
                dataStoreDirectory = WebConfigurationManager.AppSettings["data_store_directory"];
                emailLogFileName = WebConfigurationManager.AppSettings["email_log_file_name"];
                emailLogPhysicalPath = Path.Combine(appPhysicalPath, dataStoreDirectory, emailLogFileName);

                return emailLogPhysicalPath;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fillFromDataStore"></param>
        public EmailLog()
        {

            // fill the object that we are constructing with the entryLog data
            List<EmailLogEntry> dataRange = GetSavedData();
            this.AddRange(dataRange);
        }
        public List<EmailLogEntry> GetSavedData()
        {
            string json;
            List<EmailLogEntry> saveDataRange;

            // read the json data from the flat file
            json = String.Empty;
            try
            {
                using (StreamReader sr = new StreamReader(PhysicalSavePath))
                {
                    json = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
            }

            // instantiate an empty List<EmailLogEntry> object
            saveDataRange = new List<EmailLogEntry>();

            // if the json string has data, then fill the emtpy entry log 
            if (json.Length > 0)
            {
                saveDataRange = (List<EmailLogEntry>)JSON.Instance.ToObject(json, typeof(List<EmailLogEntry>));
            }
            return saveDataRange;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="romID"></param>
        /// <param name="utcJavascriptTimestamp"></param>
        public void Add(string romID)
        {
            EmailLogEntry entry = new EmailLogEntry();
            entry.RomID = romID;
            this.Add(entry);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class EmailLogEntry : EventLogEntry
    {
        /// <summary>
        /// 
        /// </summary>
        public string RomID;
    }
    #endregion
    #region Error Log
    /// <summary>
    /// 
    /// </summary>
    public class ErrorLog : EventLog
    {
        /// <summary>
        /// 
        /// </summary>
        public override string PhysicalSavePath
        {
            get
            {
                string appPhysicalPath;
                string dataStoreDirectory;
                string fileName;
                string physicalPath;

                appPhysicalPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
                dataStoreDirectory = WebConfigurationManager.AppSettings["data_store_directory"];
                fileName = WebConfigurationManager.AppSettings["error_log_file_name"];
                physicalPath = Path.Combine(appPhysicalPath, dataStoreDirectory, fileName);

                return physicalPath;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fillFromDataStore"></param>
        public ErrorLog()
        {
            List<ErrorLogEntry> savedData = GetSavedData();
            this.AddRange(savedData);
        }
        public List<ErrorLogEntry> GetSavedData()
        {
            string json;
            List<ErrorLogEntry> saveDataRange;

            // read the json data from the flat file
            json = String.Empty;
            try
            {
                using (StreamReader sr = new StreamReader(PhysicalSavePath))
                {
                    json = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
            }

            // instantiate an empty List<EmailLogEntry> object
            saveDataRange = new List<ErrorLogEntry>();

            // if the json string has data, then fill the emtpy entry log 
            if (json.Length > 0)
            {
                saveDataRange = (List<ErrorLogEntry>)JSON.Instance.ToObject(json, typeof(List<ErrorLogEntry>));
            }
            return saveDataRange;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Add(string message)
        {
            ErrorLogEntry entry = new ErrorLogEntry();
            entry.ErrorMessage = message;
            this.Add(entry);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class ErrorLogEntry : EventLogEntry
    {
        /// <summary>
        /// 
        /// </summary>
        public string ErrorMessage;
    }
    #endregion
}
