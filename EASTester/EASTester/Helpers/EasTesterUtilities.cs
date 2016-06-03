﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Xsl;
using System.Xml;
using System.IO;

namespace EASTester.Helpers
{
    class EasTesterUtilities
    {
        public static string FriendlyException(string description, string exception)
        {
            return string.Format(@"{0}<br/><br/><div class=""level1""><code>{1}</code></div>", description, EasTesterUtilities.ExceptionCleanup(exception));
        }

        public static string ExceptionCleanup(string exceptionIn)
        {
            return exceptionIn.Replace(@"   at ", @"<br/>&nbsp;&nbsp;&nbsp;at ");
        }

        public static string TransformXml(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriter xml = XmlWriter.Create(sb);

            XslCompiledTransform xsl = new XslCompiledTransform();
    
            // Load the XSL from the embedded resources
             xsl.Load(XmlReader.Create(EasTesterUtilities.GetEmbeddedResourceAsStream("EASTester", "Embedded.XmlTransform.xslt")));

            // Write the XSLT transformed document
            xsl.Transform(doc, null, xml);

            return sb.ToString();
        }

        /// <summary>
        /// Quoted-Printable decoding
        /// RFC2045 - 6.6 Canonical Encoding Model
        /// http://tools.ietf.org/html/rfc2045
        ///
        /// This is often how iOS sends ICS attachments
        /// </summary>
        /// <param name="qpString">Input quoted-printable string</param>
        /// <returns>Decoded string</returns>
        public static string DecodeQP(string qpString)
        {
            // Soft line break / 78-character line-wrap
            qpString = qpString.Replace("=<br/>", "");
            qpString = qpString.Replace("=0D=0A=20", "");

            // Newline to <br/>
            qpString = qpString.Replace("=0D=0A", "<br/>");

            // Tab to spaces
            qpString = qpString.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");

            // Space to space
            qpString = qpString.Replace("=20", " ");

            // = to =
            qpString = qpString.Replace("=3D", "=");

            return qpString;
        }

        public static string DecodeEmailData(string dataString)
        {
            dataString = System.Web.HttpUtility.HtmlEncode(dataString);
            dataString = dataString.Replace("\r\n ", "<br/>&nbsp;");
            dataString = dataString.Replace("\r\n", "<br/>");
            dataString = dataString.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
            dataString = dataString.Replace("    ", "&nbsp;&nbsp;&nbsp;&nbsp;");

            // For now, only decode ICS items, since I have no example data for other QP data
            if (dataString.ToLower().Contains("content-transfer-encoding: quoted-printable") && dataString.ToLower().Contains(@"text/calendar"))
            {
                dataString = DecodeQP(dataString);
            }

            return dataString;
        }

        /// <summary>
        /// Returns the printable bytes as a string
        /// </summary>
        /// <param name="bytesFromFiddler">byte[] to convert</param>
        /// <returns>string representation of the passed bytes</returns>
        public static string GetByteString(byte[] bytesFromFiddler)
        {
            StringBuilder sb = new StringBuilder();

            // Output the byte pairs
            foreach (byte singleByte in bytesFromFiddler)
            {
                sb.Append(singleByte.ToString("x2").ToUpper());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a stream object from an embedded resource
        /// </summary>
        /// <param name="ns">The namespace of the resource</param>
        /// <param name="res">The resource name including path (e.g. Embedded.ResourceName.res)</param>
        /// <returns></returns>
        public static Stream GetEmbeddedResourceAsStream(string ns, string res)
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("{0}.{1}", ns, res));
        }

        /// <summary>
        /// Gets an embedded resource as a string
        /// </summary>
        /// <param name="ns">The namespace of the resource</param>
        /// <param name="res">The resource name including path (e.g. Embedded.ResourceName.res)</param>
        /// <returns>String value with the contents of the resource</returns>
        public static string GetEmbeddedResourceAsString(string ns, string res)
        {
            using (var reader = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("{0}.{1}", ns, res))))
            {
                return reader.ReadToEnd();
            }
        }


        public static string GetExtendedUserAgentInfo(string userAgent)
        {
            if (userAgent.IndexOf("Apple-") == 0)
            {
                return ParseAppleUserAgent(userAgent);
            }
            else if (userAgent.IndexOf("SAMSUNG-") == 0)
            {
                return ParseSamsungUserAgent(userAgent);
            }
            else if (userAgent.IndexOf("TouchDown") == 0)
            {
                //TouchDown(MSRPC)/6.1.0007
                string hw = userAgent.Substring(9, userAgent.IndexOf("/") - 9);
                string sw = userAgent.Substring(userAgent.IndexOf("/") + 1);

                return string.Format("{0} running {1}", hw, sw);
            }

            return userAgent;
        }

        public static Dictionary<string, string> SamsungHW;
        public static Dictionary<string, string> SamsungSW;
        public static string ParseSamsungUserAgent(string userAgent)
        {
            if (null == SamsungHW)
            {
                #region Samsung Hardware versions
                SamsungHW = new Dictionary<string, string>();

                SamsungHW.Add("SGH-I337M", "Galaxy S4");
                SamsungHW.Add("SGH-I317M", "Galaxy Note II");
                #endregion
            }

            if (null == SamsungSW)
            {
                #region Samsung Software versions - With more data maybe we can just parse this, looks like a pattern
                SamsungSW = new Dictionary<string, string>();

                SamsungSW.Add("101.403", "4.3");
                SamsungSW.Add("100.40102", "4.1.2");
                #endregion
            }

            if (userAgent.IndexOf("SAMSUNG-") == 0 && userAgent.IndexOf("/") > -1)
            {
                string hw = userAgent.Substring(8, userAgent.IndexOf("/") - 8);
                string sw = userAgent.Substring(userAgent.IndexOf("/") + 1);

                if (SamsungHW.ContainsKey(hw))
                {
                    hw = SamsungHW[hw];
                }

                if (SamsungSW.ContainsKey(sw))
                {
                    sw = "Android " + SamsungSW[sw];
                }

                return string.Format("Samsung {0} running {1}", hw, sw);
            }

            return userAgent;
        }

        public static Dictionary<string, string> AppleHW;
        public static Dictionary<string, string> AppleSW;
        public static string ParseAppleUserAgent(string userAgent)
        {
            if (null == AppleHW)
            {
                #region Apple Hardware versions
                AppleHW = new Dictionary<string, string>();

                AppleHW.Add("iPad1C1", "iPad");
                AppleHW.Add("iPad2C1", "iPad 2 WiFi");
                AppleHW.Add("iPad2C2", "iPad 2 WiFi + 3G");
                AppleHW.Add("iPad2C3", "iPad 2 WiFi + 3G CDMA");
                AppleHW.Add("iPad2C4", "iPad Mini - WiFi");
                AppleHW.Add("iPad2C5", "iPad Mini - WiFi + LTE");
                AppleHW.Add("iPad3C1", "The New iPad (iPad 3)- WiFi");
                AppleHW.Add("iPad3C2", "The New iPad (iPad 3) - WiFi + LTE");
                AppleHW.Add("iPad3C3", "iPad with Retina Display (iPad 4) - WiFi");
                AppleHW.Add("iPad3C4", "iPad with Retina Display (iPad 4) - WiFi + LTE");
                AppleHW.Add("iPad4C1", "iPad Air - WiFi");
                AppleHW.Add("iPad4C2", "iPad Air - WiFi + LTE");
                AppleHW.Add("iPad4C4", "iPad Mini with Retina Display - WiFi");
                AppleHW.Add("iPad4C5", "iPad Mini with Retina Display - WiFi + LTE");
                AppleHW.Add("iPad5C4", "iPad Air 2");
                AppleHW.Add("iPhone1C2", "iPhone 3G");
                AppleHW.Add("iPhone2C1", "iPhone 3GS");
                AppleHW.Add("iPhone3C1", "iPhone 4 GSM");
                AppleHW.Add("iPhone3C2", "iPhone4 GSM");
                AppleHW.Add("iPhone3C3", "iPhone 4 CDMA");
                AppleHW.Add("iPhone4C1", "iPhone 4S");
                AppleHW.Add("iPhone5C1", "iPhone 5 GSM/LTE");
                AppleHW.Add("iPhone5C2", "iPhone 5 CDMA USA/China");
                AppleHW.Add("iPhone5C3", "iPhone 5C GSM/CDMA/Americas");
                AppleHW.Add("iPhone5C4", "iPhone 5C Europe/Asia");
                AppleHW.Add("iPhone6C1", "iPhone 5S GSM/CDMA/Americas");
                AppleHW.Add("iPhone6C2", "iPhone 5S Europe/Asia");
                AppleHW.Add("iPhone7C1", "iPhone 6+");
                AppleHW.Add("iPhone7C2", "iPhone 6");
                AppleHW.Add("iPod2C1", "iPod Touch 2");
                AppleHW.Add("iPod3C1", "iPod Touch 3");
                AppleHW.Add("iPod4C1", "iPod Touch 4");
                AppleHW.Add("iPod5C1", "iPod Touch 5");
                #endregion
            }

            if (null == AppleSW)
            {
                #region Apple Software versions
                AppleSW = new Dictionary<string, string>();

                AppleSW.Add("508.11", "2.2.1");
                AppleSW.Add("701.341", "3.0");
                AppleSW.Add("701.400", "3.0.1");
                AppleSW.Add("702.367", "3.2");
                AppleSW.Add("702.405", "3.2.1");
                AppleSW.Add("702.500", "3.2.2");
                AppleSW.Add("703.144", "3.1");
                AppleSW.Add("703.146", "3.1");
                AppleSW.Add("704.11", "3.1.2");
                AppleSW.Add("705.18", "3.1.3");
                AppleSW.Add("801.293", "4.0");
                AppleSW.Add("801.306", "4.0.1");
                AppleSW.Add("801.400", "4.0.2");
                AppleSW.Add("802.117", "4.1");
                AppleSW.Add("802.118", "4.1");
                AppleSW.Add("803.148", "4.2.1");
                AppleSW.Add("803.14800001", "4.2.1");
                AppleSW.Add("805.128", "4.2.5");
                AppleSW.Add("805.200", "4.2.6");
                AppleSW.Add("805.303", "4.2.7");
                AppleSW.Add("805.401", "4.2.8");
                AppleSW.Add("805.501", "4.2.9");
                AppleSW.Add("805.600", "4.2.10");
                AppleSW.Add("806.190", "4.3");
                AppleSW.Add("806.191", "4.3");
                AppleSW.Add("807.4", "4.3.1");
                AppleSW.Add("808.7", "4.3.2");
                AppleSW.Add("808.8", "4.3.2");
                AppleSW.Add("810.2", "4.3.3");
                AppleSW.Add("810.3", "4.3.3");
                AppleSW.Add("811.2", "4.3.4");
                AppleSW.Add("812.1", "4.3.5");
                AppleSW.Add("901.334", "5.0");
                AppleSW.Add("901.405", "5.0.1");
                AppleSW.Add("901.406", "5.0.1");
                AppleSW.Add("902.176", "5.1");
                AppleSW.Add("902.179", "5.1");
                AppleSW.Add("902.206", "5.1.1");
                AppleSW.Add("902.208", "5.1.1");
                AppleSW.Add("1001.403", "6.0");
                AppleSW.Add("1001.405", "6.0");
                AppleSW.Add("1001.406", "6.0");
                AppleSW.Add("1001.407", "6.0");
                AppleSW.Add("1001.523", "6.0");
                AppleSW.Add("1001.525", "6.0");
                AppleSW.Add("1001.550", "6.0");
                AppleSW.Add("1001.551", "6.0");
                AppleSW.Add("1001.8426", "6.0");
                AppleSW.Add("1001.8500", "6.0");
                AppleSW.Add("1002.141", "6.1");
                AppleSW.Add("1002.142", "6.1");
                AppleSW.Add("1002.143", "6.1");
                AppleSW.Add("1002.144", "6.1");
                AppleSW.Add("1002.145", "6.1.1");
                AppleSW.Add("1002.146", "6.1.2");
                AppleSW.Add("1002.329", "6.1.3");
                AppleSW.Add("1002.350", "6.1.4");
                AppleSW.Add("1101.465", "7.0");
                AppleSW.Add("1101.470", "7.0.1");
                AppleSW.Add("1101.47000001", "7.0.1");
                AppleSW.Add("1101.501", "7.0.2");
                AppleSW.Add("1102.511", "7.0.3");
                AppleSW.Add("1102.55400001", "7.0.4");
                AppleSW.Add("1102.601", "7.0.5");
                AppleSW.Add("1102.651", "7.0.6");
                AppleSW.Add("1104.167", "7.1");
                AppleSW.Add("1104.169", "7.1");
                AppleSW.Add("1104.201", "7.1.1");
                AppleSW.Add("1104.257", "7.1.2");
                AppleSW.Add("1201.365", "8.0");
                AppleSW.Add("1201.366", "8.0.1");
                AppleSW.Add("1201.405", "8.0.2");
                AppleSW.Add("1202.410", "8.1");
                AppleSW.Add("1202.411", "8.1");
                #endregion
            }

            // Apple iOS: Apple-iPad2C1/1202.410
            if (userAgent.IndexOf("Apple-") == 0 && userAgent.IndexOf("/") > -1)
            {
                string hw = userAgent.Substring(6, userAgent.IndexOf("/") - 6);
                string sw = userAgent.Substring(userAgent.IndexOf("/") + 1);

                if (AppleHW.ContainsKey(hw))
                {
                    hw = AppleHW[hw];
                }

                if (AppleSW.ContainsKey(sw))
                {
                    sw = "iOS " + AppleSW[sw];
                }

                return string.Format("Apple {0} running {1}", hw, sw);
            }

            return userAgent;
        }

    }
}
