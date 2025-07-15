//using Castle.Core.Internal;
//using PQS.PQZBinary;
//using System.Xml;

//namespace PQBI.Extensions
//{
//    internal class PQSServiceParser
//    {
//        internal static bool TryGetSessionID(string xml, out string sessionID)
//        {
//            sessionID = string.Empty;

//            if (xml.IsNullOrEmpty())
//            {
//                return false;
//            }

//            var xmlDoc = new XmlDocument();
//            xmlDoc.LoadXml(xml);


//            var items = xmlDoc.GetElementsByTagName("Status");
//            if (items.Count > 0)
//            {
//                if (items[0].InnerText.Equals("OK", StringComparison.OrdinalIgnoreCase))
//                {
//                    items = xmlDoc.GetElementsByTagName("Values");
//                    if (items.Count > 0)
//                    {
//                        sessionID = items[0].InnerText;
//                        return true;
//                    }
//                }
//            }

//            sessionID = null;
//            return false;
//        }


//        internal static bool IsSessionClosed(string xml)
//        {
//            var isErrorRecord = false;

//            var xmlDoc = new XmlDocument();
//            xmlDoc.LoadXml(xml);

//            var items = xmlDoc.GetElementsByTagName("Status");
//            if (items.Count > 0)
//            {
//                isErrorRecord = items[0].InnerText.Equals("OK", StringComparison.OrdinalIgnoreCase);
//            }

//            return isErrorRecord;
//        }


//        internal static bool IsNopSucceeded(string xml)
//        {

//            //var  container = new  PQZBinaryReader(null,null,true,true);
//            var isErrorRecord = false;

//            var xmlDoc = new XmlDocument();
//            xmlDoc.LoadXml(xml);

//            var items = xmlDoc.GetElementsByTagName("Status");
//            if (items.Count > 0)
//            {
//                isErrorRecord = items[0].InnerText.Equals("OK", StringComparison.OrdinalIgnoreCase);
//            }

//            return isErrorRecord;
//        }
//    }
//}