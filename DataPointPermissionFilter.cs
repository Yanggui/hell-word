using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Moodys3.WSPlatform.BizCore.Authorization.Interfaces.Domains;
using DataPoint = Moodys3.WSPlatform.BizCore.ExcelAddin.Interfaces.Models.DataPoint;

namespace Moodys3.WSPlatform.WebApi.Extension.Formatter
{
	public class DataPointPermissionFilter
	{
		public const string DataPointNamespace = "http://api.moodys.com/REST";

        public string FilterDataPointPermission(ICollection<DataPoint> allDataPoints, ICollection<ProductRight> theProductRight, IDictionary<int, ICollection<string>> theDataPointXpathMapping, string theStream)
		{
			var root = XDocument.Parse(theStream);
			var aDataPointIdList = allDataPoints.Select(theDp => theDp.Id);
            var subScribedXpathList = new List<string>();
            foreach (var productRight in theProductRight)
            {
                subScribedXpathList.AddRange(productRight.DataPointSubscription.Where(dp => aDataPointIdList.Contains(dp.Id) && theDataPointXpathMapping.ContainsKey(dp.Id)).SelectMany(dp => theDataPointXpathMapping[dp.Id]).ToList());
            }

			var allXpathList =
				allDataPoints.Where(dp => theDataPointXpathMapping.ContainsKey(dp.Id)).SelectMany(dp => theDataPointXpathMapping[dp.Id]);
			var subScribedXpathHash = new HashSet<string>();
			foreach (var xpath in subScribedXpathList)
			{
				subScribedXpathHash.Add(xpath);
			}

			var noPermissionXpathList =
				allXpathList.Where(xpath => !string.IsNullOrEmpty(xpath) && !subScribedXpathHash.Contains(xpath)).ToList();

			var aNameTable = new NameTable();
			var namespaceManager = new XmlNamespaceManager(aNameTable);
			namespaceManager.AddNamespace("ns", DataPointNamespace);
			foreach (var xPath in noPermissionXpathList)
			{
				foreach (var element in root.XPathSelectElements(xPath, namespaceManager))
				{
					element.Remove();
				}

				foreach (var element in root.XPathSelectElements("/" + xPath, namespaceManager))
				{
					element.Remove();
				}
			}

			return root.ToString();
		}
	}
}
