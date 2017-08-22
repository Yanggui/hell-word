using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Moodys3.WSPlatform.Data.Interfaces.Services;

namespace Moodys3.WSPlatform.WebApi.Extension.Formatter
{
	public class DataPointBatchFilter
	{
		private IExcelAddinConfigService excelAddInConfigService;

		private readonly XmlDocument xmlDocument;
		private XmlDocument resultDocument;
		private XmlNode noMatchedNode;
		private IDictionary<string, string> dataPointXPathDic = new Dictionary<string, string>();

		private const string RequesIndentifierElementName = "request_identifier";
		private const string NoMathedElementName = "no-match-items";
		private const string RDS_XML_RequesIndentifier_TEXT = "<request_identifier>{0}</request_identifier>";
		private const string RDS_XML_FOBIDDEN_IDENTIFIER_TEXT = "<no-match-item><identifier>{0}</identifier><status_code>Forbidden</status_code></no-match-item>";
		private const string RDS_XML_NOFOUND_IDENTIFIER_TEXT = "<no-match-item><identifier>{0}</identifier><status_code>NoFound</status_code></no-match-item>";

		private XmlNamespaceManager namespaceManager;

		private XmlNamespaceManager NamespaceManager
		{
			get
			{
				if (namespaceManager == null)
				{
					namespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
					namespaceManager.AddNamespace("ns", xmlDocument.DocumentElement.NamespaceURI);
				}

				return namespaceManager;
			}
		}

		public DataPointBatchFilter(XmlDocument theXmlDocument, IExcelAddinConfigService theExcelAddInConfigService)
		{
			xmlDocument = theXmlDocument;
			this.excelAddInConfigService = theExcelAddInConfigService;
		}

		public XmlDocument BatchFilter()
		{
			if (xmlDocument == null || xmlDocument.DocumentElement == null || !xmlDocument.DocumentElement.HasChildNodes)
			{
				return null;
			}

			resultDocument = new XmlDocument();
			resultDocument.LoadXml(xmlDocument.DocumentElement.CloneNode(false).OuterXml);
			this.noMatchedNode = resultDocument.CreateElement(NoMathedElementName);

			foreach (XmlNode orgmarketdata in xmlDocument.DocumentElement.ChildNodes)
			{
				if (orgmarketdata.Name.ToLower() == NoMathedElementName)
				{
					continue;
				}

				this.ProcessElementData(orgmarketdata);
			}

			resultDocument.DocumentElement.AppendChild(noMatchedNode);
			return resultDocument;
		}

		private void ProcessElementData(XmlNode orgmarketdata)
		{
			XmlNode requestIdentifierNode = null;
			foreach (XmlNode childNode in orgmarketdata.ChildNodes)
			{
				if (childNode.LocalName == RequesIndentifierElementName)
				{
					requestIdentifierNode = childNode;
					break;
				}
			}

			if (requestIdentifierNode == null)
			{
				return;
			}

			var requestIdentifier = new RequestIdentifier(requestIdentifierNode.InnerText);
			requestIdentifier.ProcessRequestIdentifier();

			foreach (string dpId in requestIdentifier.DataPointIds)
			{
				string dpIndentifier = string.Format(requestIdentifier.ReportInfo, dpId);

				string xPath = this.GetXPathByDataPoints(dpId);
				if (string.IsNullOrEmpty(xPath))
				{
					string xmlText = string.Format(RDS_XML_NOFOUND_IDENTIFIER_TEXT, dpIndentifier);
					this.noMatchedNode.InnerXml += xmlText;
					continue;
				}

				XmlNode datapoint = null;
				XmlNodeFilter xmlNodeFilter = new XmlNodeFilter(orgmarketdata);
				datapoint = xmlNodeFilter.FilterByxPath("./" + xPath, NamespaceManager);

				if (datapoint != null)
				{
					if (string.IsNullOrEmpty(datapoint.InnerXml))
					{
						string xmlText = string.Format(RDS_XML_NOFOUND_IDENTIFIER_TEXT, dpIndentifier);
						this.noMatchedNode.InnerXml += xmlText;
					}
					else
					{
						datapoint.InnerXml = string.Format(RDS_XML_RequesIndentifier_TEXT, dpIndentifier) + datapoint.InnerXml;
						resultDocument.DocumentElement.InnerXml += datapoint.OuterXml;
					}
				}
				else
				{
					string xmlText = string.Format(RDS_XML_FOBIDDEN_IDENTIFIER_TEXT, dpIndentifier);
					this.noMatchedNode.InnerXml += xmlText;
				}
			}
		}

		private string GetXPathByDataPoints(string theDataPointId)
		{
			if (dataPointXPathDic.ContainsKey(theDataPointId))
			{
				return dataPointXPathDic[theDataPointId];
			}

			int dpId;
			if (!int.TryParse(theDataPointId, out dpId))
			{
				return string.Empty;
			}

			string xPath = this.excelAddInConfigService.QueryFormulaMapping(dpId);
			if (!string.IsNullOrEmpty(xPath))
			{
				dataPointXPathDic.Add(theDataPointId, xPath);
			}

			return xPath;
		}
	}

	public class XmlNodeFilter
	{
		private XmlNode rootNode;
		private Dictionary<XmlNode, XmlNode> map = new Dictionary<XmlNode, XmlNode>();

		private XmlDocument xmlDoc;

		private XmlNamespaceManager namespaceManager;

		private XmlNamespaceManager NamespaceManager
		{
			get
			{
				if (namespaceManager == null)
				{
					namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
					namespaceManager.AddNamespace("ns", xmlDoc.DocumentElement.NamespaceURI);
				}

				return namespaceManager;
			}
		}

		public XmlNodeFilter(XmlNode theXmlNode)
		{
			xmlDoc = new XmlDocument();

			xmlDoc.LoadXml(theXmlNode.OuterXml);
		}

		public XmlNode FilterByxPath(string xpath)
		{
			if (string.IsNullOrEmpty(xpath))
			{
				return null;
			}

			XmlNodeList dataPoints = GetNodeListByXpath(xpath);

			if (dataPoints.Count == 0)
			{
				return rootNode;
			}

			foreach (XmlNode dataPoint in dataPoints)
			{
				XmlNode cloneDataPoint = dataPoint.CloneNode(true);

				CreateDataPointTree(dataPoint, cloneDataPoint, dataPoints);
			}

			return rootNode;
		}

		public XmlNode FilterByxPath(string xpath, XmlNamespaceManager theNsManager)
		{
			if (string.IsNullOrEmpty(xpath))
			{
				return null;
			}

			XmlNodeList dataPoints = xmlDoc.SelectNodes(xpath, theNsManager);

			if (dataPoints.Count == 0)
			{
				return rootNode;
			}

			foreach (XmlNode dataPoint in dataPoints)
			{
				XmlNode cloneDataPoint = dataPoint.CloneNode(true);

				CreateDataPointTree(dataPoint, cloneDataPoint, dataPoints);
			}

			return rootNode;
		}

		private void CreateDataPointTree(XmlNode dataPoint, XmlNode cloneDataPoint, XmlNodeList nodeList)
		{
			if (map.Keys.Contains(dataPoint))
			{
				var isExist = IsOneOfNodesByXpath(dataPoint, nodeList);

				if (!isExist)
				{
					map[dataPoint].AppendChild(cloneDataPoint.FirstChild);
				}

				return;
			}

			if (dataPoint.ParentNode.NodeType == XmlNodeType.Document)
			{
				if (rootNode != null)
				{
					rootNode.AppendChild(cloneDataPoint.FirstChild);
				}
				else
				{
					rootNode = cloneDataPoint;
				}

				return;
			}

			var parentClone = dataPoint.ParentNode.CloneNode(false);
			parentClone.AppendChild(cloneDataPoint);
			map.Add(dataPoint, cloneDataPoint);

			dataPoint = dataPoint.ParentNode;
			cloneDataPoint = cloneDataPoint.ParentNode;
			CreateDataPointTree(dataPoint, cloneDataPoint, nodeList);
		}

		private bool IsOneOfNodesByXpath(XmlNode dataPoint, XmlNodeList nodeList)
		{
			bool isExist = false;
			foreach (XmlNode node in nodeList)
			{
				if (dataPoint.Equals(node))
				{
					isExist = true;
					break;
				}
			}

			return isExist;
		}

		private XmlNodeList GetNodeListByXpath(string nodeXPath)
		{
			if (xmlDoc.DocumentElement != null && (string.IsNullOrEmpty(xmlDoc.DocumentElement.NamespaceURI)))
			{
				return xmlDoc.SelectNodes(nodeXPath);
			}

			return xmlDoc.SelectNodes(nodeXPath, NamespaceManager);
		}
	}

	public class RequestIdentifier
	{
		private const char DataPointSplitTag = '-';

		private string reportInfo = string.Empty;

		private string dataPointreg = @"\[DP(?<dataPointId>[^\]]+)\]";

		private IList<string> dataPointIds;

		public string ReportInfo
		{
			get
			{
				return this.reportInfo;
			}
		}

		public IList<string> DataPointIds
		{
			get
			{
				if (dataPointIds == null)
				{
					dataPointIds = new List<string>();
				}

				return dataPointIds;
			}
		}

		private readonly string requestIndentifyText;

		public RequestIdentifier(string theRequestIndentify)
		{
			requestIndentifyText = theRequestIndentify;
		}

		public void ProcessRequestIdentifier()
		{
			string elementValue = requestIndentifyText;
			if (string.IsNullOrEmpty(elementValue))
			{
				return;
			}

			Match mc = Regex.Match(elementValue, dataPointreg, RegexOptions.IgnoreCase);
			if (mc.Groups["dataPointId"].Success)
			{
				string dpstr = mc.Groups["dataPointId"].Value;

				reportInfo = this.requestIndentifyText.Replace(dpstr, "{0}");

				string[] dps = dpstr.Split(DataPointSplitTag);

				dataPointIds = dps.ToList();
			}
		}
	}
}
