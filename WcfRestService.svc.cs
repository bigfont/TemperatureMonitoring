using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.IO;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Net.Mail;
using System.Net;
using System.ComponentModel;
using System.Threading;
using System.Collections.Specialized;
using fastJSON;
using System.Collections;
using System.ServiceModel.Channels;
using System.Web.Configuration;
using System.ServiceModel.Web;

namespace NatureWorksFridgeMonitoring
{
    /// <inheritdoc cref="IWcfRestService" />
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class WcfRestService : IWcfRestService
    {
        #region Public Methods
        /// <inheritdoc />
        public void ReceiveOneWireXmlPost(Stream postData)
        {
            XDocument xDoc;
            IEnumerable<XElement> xElements;
            long javascriptUtcTimestamp;
            decimal temperatureCelsius;
            string romID;
            string thermometerXmlNodeName;

            // flot plots time data using javascript timestamps in UTC
            // so we timestamp on the server using javascript timestamps
            javascriptUtcTimestamp = UtcJavascriptTimestampHelper.Now;

            try
            {
                // convert POST data into an xDocument               
                xDoc = ConvertXmlPostDataToXDocument(postData);

                // get the descendent thermometers
                thermometerXmlNodeName = WebConfigurationManager.AppSettings["thermometer_xml_node_name"];
                xElements = xDoc.Descendants(thermometerXmlNodeName);

                // for each child device
                foreach (XElement xElement in xElements)
                {

                    // get its romID
                    romID = null;
                    if (xElement.Elements("ROMId").FirstOrDefault() != null)
                    {
                        romID = xElement.Elements("ROMId").FirstOrDefault().Value.ToString();
                    }

                    // get its temperature
                    temperatureCelsius = Decimal.MinValue;
                    if (xElement.Elements("Temperature").FirstOrDefault() != null)
                    {
                        temperatureCelsius = Convert.ToDecimal(xElement.Elements("Temperature").FirstOrDefault().Value.ToString());
                    }

                    if (TemperatureHasExceededThreshold(temperatureCelsius, romID))
                    {
                        Emailer emailer = new Emailer();
                        emailer.SendWarningEmail(romID, temperatureCelsius, javascriptUtcTimestamp);
                    }

                    // get the data store
                    FlotDataTable flotDataTable = new FlotDataTable(romID);

                    // delete old data from the data store                    
                    flotDataTable.DeleteStaleData();

                    // append a new data point to the data store
                    flotDataTable.AppendDataPoint(javascriptUtcTimestamp, temperatureCelsius);

                    // save changes to the FlotDataTable
                    flotDataTable.Save();

                    FlotDataSet flotDataSet;
                    flotDataSet = new FlotDataSet(false, false);
                    flotDataSet.RecompleTheIndividualDataTableFilesIntoOneLargeDataSetFileIfEnoughTimeHasPassed(true);
                }
            }
            catch (Exception ex)
            {

            }
        }
        /// <inheritdoc />
        public FlotDataSet[] SendFlotDataSet()
        {
            FlotDataSet[] flotDataSets = new FlotDataSet[]
                {
                    new FlotDataSet(true, true), 
                    new FlotDataSet(true, false)
                };
            // do high resolution iff browser supports canvas.
            return flotDataSets;
        }
        /// <inheritdoc />              
        public SimpleHttpResponseMessage ReceiveSystemSettingsFromUserInterface(Stream postData)
        {
            string romId, friendlyName, username, password;
            int temperatureThreshold, counter;
            NameValueCollection postKeyValuePairs;
            SimpleHttpResponseMessage result;

            // assume that the save will be succesful, for now
            result = new SimpleHttpResponseMessage();
            result.Message = "success";

            // parse the application/x-www-form-urlencoded POST data into a collection
            postKeyValuePairs = ConvertPostStreamIntoNameValueCollection(postData);

            username = GetValueFromNameValueCollectionAndThenRemoveItToo(postKeyValuePairs, "username");
            password = GetValueFromNameValueCollectionAndThenRemoveItToo(postKeyValuePairs, "password");
            if (!UsernameAndPasswordAreCorrect(username, password))
            {
                result.Message = "unauthorized";
                return result;
            }

            // save the system wide settings, of which there are two
            SystemSettings systemSettings = new SystemSettings();
            systemSettings.DataStoreDurationInDays = Convert.ToInt32(GetValueFromNameValueCollectionAndThenRemoveItToo(postKeyValuePairs, "data-store-duration-in-days"));
            systemSettings.HoursThatMustPassBetweenSendingDeviceSpecificWarningEmails = Convert.ToInt32(GetValueFromNameValueCollectionAndThenRemoveItToo(postKeyValuePairs, "max-email-freq-in-hours"));
            systemSettings.WarningEmailRecipientsInCsv = GetValueFromNameValueCollectionAndThenRemoveItToo(postKeyValuePairs, "warning-email-recipients-csv");

            // instantiate empty variables in preparation for saving device specific settings
            romId = String.Empty;
            friendlyName = String.Empty;
            temperatureThreshold = Int32.MaxValue;

            // save the device specific settings, of which there are two for each device
            // a romId is the key for each device
            counter = 0;
            foreach (string key in postKeyValuePairs.AllKeys)
            {
                if (key.ToLowerInvariant().Contains("rom-id"))
                {
                    counter = 0;
                    romId = postKeyValuePairs[key];
                }
                else if (key.ToLowerInvariant().Contains("friendly-name"))
                {
                    ++counter;
                    friendlyName = postKeyValuePairs[key];
                }
                else if (key.ToLowerInvariant().Contains("temp-threshold"))
                {
                    ++counter;
                    temperatureThreshold = Convert.ToInt32(postKeyValuePairs[key]);
                }
                if (counter > 0 && counter % DeviceSettings.SYSTEM_SETTINGS_DEVICE_DATA_COUNT == 0)
                {
                    DeviceSettings deviceSettings = new DeviceSettings() { FriendlyName = friendlyName, TemperatureThreshold = temperatureThreshold };
                    if (systemSettings.DeviceSettingsDictionary.ContainsKey(romId))
                    {
                        systemSettings.DeviceSettingsDictionary[romId] = deviceSettings;
                    }
                    else
                    {
                        systemSettings.DeviceSettingsDictionary.Add(romId, new DeviceSettings() { FriendlyName = friendlyName, TemperatureThreshold = temperatureThreshold });
                    }
                }
            }

            systemSettings.Save();
            return result;
        }
        /// <inheritdoc />      
        public SystemSettings SendSystemSettingsToUserInterface()
        {
            SystemSettings systemSettings = new SystemSettings(true);

            return systemSettings;
        }
        /// <inheritdoc />      
        public SimpleHttpResponseMessage SendTestEmail(Stream postData)
        {
            NameValueCollection postKeyValuePairs;
            string recipientsCsv, username, password;
            SimpleHttpResponseMessage result;

            result = new SimpleHttpResponseMessage();
            result.Message = "success";

            // convert the postData into a collection
            postKeyValuePairs = ConvertPostStreamIntoNameValueCollection(postData);

            username = GetValueFromNameValueCollectionAndThenRemoveItToo(postKeyValuePairs, "username");
            password = GetValueFromNameValueCollectionAndThenRemoveItToo(postKeyValuePairs, "password");
            if (!UsernameAndPasswordAreCorrect(username, password))
            {
                result.Message = "unauthorized";
                return result;
            }

            // get recipient emails
            recipientsCsv = GetValueFromNameValueCollectionAndThenRemoveItToo(postKeyValuePairs, "warning-email-recipients-csv");

            Emailer emailer = new Emailer();
            emailer.SendTestEmail(recipientsCsv);

            return result;
        }
        #endregion
        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        private NameValueCollection ConvertPostStreamIntoNameValueCollection(Stream postData)
        {
            StreamReader reader;
            string postString;

            // read the POST data to the end
            reader = new StreamReader(postData);
            postString = reader.ReadToEnd();

            // parse the application/x-www-form-urlencoded POST data into a collection
            NameValueCollection collection = HttpUtility.ParseQueryString(postString);
            return collection;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetValueFromNameValueCollectionAndThenRemoveItToo(NameValueCollection collection, string key)
        {
            string value;            
            value = collection[key]; // value will be null if key does not exist
            collection.Remove(key); // nothing happens if the key does not exist
            return value;
        }
        /// <summary>
        /// Get just the XML content from the OWServer_v2-Enet POST.
        /// </summary>
        /// <param name="postData"></param>
        /// <returns>The XDocument that represents the XML from the post data.</returns>
        private XDocument ConvertXmlPostDataToXDocument(Stream postData)
        {
            int startIndex;
            int endIndex;
            int startCount;
            int endCount;
            string xmlContent;
            StreamReader reader;
            string postString;
            XDocument xdoc;

            reader = new StreamReader(postData);
            postString = reader.ReadToEnd();

            // strings are zero indexed, 
            // whereas counts are one indexed;
            // so we increment counts by one.
            startIndex = postString.IndexOf("<");
            endIndex = postString.LastIndexOf(">");
            startCount = startIndex + 1;
            endCount = endIndex + 1;

            // get the content that is between a < and a > character
            // we are assuming that this defines the XML content for now.
            xmlContent = postString.Substring(startIndex, endCount - startIndex);

            // convert strong to xdoc
            xdoc = XDocument.Parse(xmlContent);

            return xdoc;
        }
        /// <summary>
        /// Check the device temperatures against the threshold. 
        /// Send a warning email if temperature is too high.
        /// </summary>
        /// <param name="romID"></param>
        /// <param name="temperatureCelsius"></param>
        private bool TemperatureHasExceededThreshold(decimal temperatureCelsius, string romID)
        {
            decimal temperatureThreshold;
            bool result;

            // get the temperature threshold
            SystemSettings systemSettings = new SystemSettings(true);

            //Dictionary<string, DeviceSettings> deviceSettingsDictionary = systemSettings.DeviceSettingsDictionary;
            temperatureThreshold = systemSettings.GetDeviceSettingsByRomID(romID).TemperatureThreshold;            

            result = temperatureCelsius > temperatureThreshold;

            return result;
        }
        private bool UsernameAndPasswordAreCorrect(string username, string password)
        {
            string usernameKey, passwordKey;
            bool result;
            usernameKey = WebConfigurationManager.AppSettings["username"];
            passwordKey = WebConfigurationManager.AppSettings["password"];
            // assume incorrect password
            result = false;
            if (usernameKey.Equals(username) && passwordKey.Equals(password))
            {
                result = true;
            }
            return result;
        }
        #endregion
    }
}
