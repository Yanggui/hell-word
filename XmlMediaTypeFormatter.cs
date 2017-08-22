using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;
using Moodys3.Common.ObjectBuilder;
using Moodys3.WSPlatform.BizCore.Authorization.Caching;
using Moodys3.WSPlatform.BizCore.Authorization.Interfaces.Domains;
using Moodys3.WSPlatform.BizCore.Authorization.Interfaces.Services;
using Moodys3.WSPlatform.BizCore.ExcelAddin.Interfaces.Models;
using Moodys3.WSPlatform.BizCore.Interfaces.Models.Custom;
using Moodys3.WSPlatform.Common;
using Moodys3.WSPlatform.Common.Utils;
using Moodys3.WSPlatform.Common.WebContext;
using Moodys3.WSPlatform.Data.ExcelAddin.Interfaces.Services;
using Moodys3.WSPlatform.Data.Interfaces.Services;
using Moodys3.WSPlatform.WebApi.Extension.Utils;
using Moodys3.WSPlatform.WebService.Common;
using Moodys3.WSPlatform.WebService.Contracts.Dtos;

namespace Moodys3.WSPlatform.WebApi.Extension.Formatter
{
	public class XmlMediaTypeFormatter : MediaTypeFormatterBase
	{
		private readonly IUserAuthorizationService userAuthorizationService;
		private readonly IExcelAddinConfigService excelAddinConfigCacheService;
		private readonly IProductUniverseDataService productUniverseDataService;
		private readonly IExcelAddInConfigDataService excelAddInConfigDataService;

		public XmlMediaTypeFormatter()
		{
			ObjectFactory.TryGetInstance(out userAuthorizationService);
			ObjectFactory.TryGetInstance(out excelAddinConfigCacheService);
			ObjectFactory.TryGetInstance(out productUniverseDataService);
			ObjectFactory.TryGetInstance(out excelAddInConfigDataService);

			SupportedMediaTypes.Clear();
			MediaTypeMappings.Clear();
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml"));
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml"));
			MediaTypeMappings.Add(new QueryStringMapping("alt", "xml", "application/xml"));
		}

		public override bool CanReadType(Type theType)
		{
			return true;
		}

		public override bool CanWriteType(Type theType)
		{
			return true;
		}

		public override Task<object> ReadFromStreamAsync(Type theType, Stream theReadStream, HttpContent theContent, IFormatterLogger theFormatterLogger)
		{
			return Task.FromResult(ReadFromStream(theType, theReadStream, theContent, theFormatterLogger));
		}

		protected object ReadFromStream(Type theType, Stream theReadStream, HttpContent theContent, IFormatterLogger theFormatterLogger)
		{
			HttpContentHeaders aContentHeaders = theContent == null ? null : theContent.Headers;

			if (aContentHeaders != null && aContentHeaders.ContentLength == 0)
			{
				return GetDefaultValueForType(theType);
			}

			try
			{
				using (var aReader = CreateXmlReader(theReadStream, theContent))
				{
					var aSerializer = XmlSerializerUtil.BuildXmlSerializer(theType);
					return aSerializer.Deserialize(aReader);
				}
			}
			catch (Exception aException)
			{
				if (theFormatterLogger != null)
				{
					theFormatterLogger.LogError("XmlMediaTypeFormatter.ReadFromStream", aException.Message);
				}

				return GetDefaultValueForType(theType);
			}
		}

		private XmlReader CreateXmlReader(Stream theReadStream, HttpContent theContent)
		{
			var aEffectiveEncoding = SelectCharacterEncoding(theContent == null ? null : theContent.Headers);
			return XmlDictionaryReader.CreateTextReader(theReadStream, aEffectiveEncoding, XmlDictionaryReaderQuotas.Max, null);
		}

		public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
		{
			WriteToStream(value, writeStream, content);
			return Task.FromResult<object>(null);
		}

		protected void WriteToStream(object theValue, Stream theStream, HttpContent theContent)
		{
			SetRemovedDataPoint(theValue);
			DataPointFilterMode dataPointFilterMode;
			string aDataPointFilterModeStr =
				WsRequestQueryParametersHelper.Instance.GetValue(WsConstants.QUERYPARAMETER_DATAPOINTFILTERMODE);
			if (!Enum.TryParse(aDataPointFilterModeStr, out dataPointFilterMode))
			{
				dataPointFilterMode = DataPointFilterMode.Null;
			}

			SetTrafficResponse(theValue, theContent);
			var aPlainTextDto = theValue as DtoPlainText;
			if (aPlainTextDto != null)
			{
				WritePlainText(aPlainTextDto, theContent, theStream, "xml");
				return;
			}

			UserPrincipalWrapper aUserPrincipalWrapper = AuthorizationUtil.GetUserPrincipalWrapper();
			bool aIsAdmin = aUserPrincipalWrapper != null && aUserPrincipalWrapper.IsAdmin;
		    ProductUniverse aProductUniverse = null;
            if (!aIsAdmin && aUserPrincipalWrapper != null && aUserPrincipalWrapper.ProductRights != null)
		    {
                var aOperationName = WspContext.Current.GetValue(WsConstants.OPERATION_NAME) as string;
                aProductUniverse = productUniverseDataService.GetOperationProductUniverse(aOperationName);
		    }

            theValue = FormatResponseResult(theValue);

            // no need to perform xml datapoint filter, perform serialization directly
		    if (dataPointFilterMode == DataPointFilterMode.Null && aProductUniverse == null)
		    {
                var aSerializedXmlStream = GetSerializedStream(theValue);
                WriteXmlData(theStream, aSerializedXmlStream, theContent);
		        return;
            }

            #region Datapoint Filter

            var aResultContent = GetSerializedString(theValue);
            if (aProductUniverse != null)
			{
				var aDataPointResultMapping = excelAddInConfigDataService.GetDataPointXpathMappings(true);
				var aAllDataPoints = excelAddInConfigDataService.QueryAllDataPointsWithoutItem(true);
                var aApiProductUniverseType = (EProductUniverseTypes)aProductUniverse.ProductUniverse1;
				if (aApiProductUniverseType == EProductUniverseTypes.RATINGS)
				{
					aAllDataPoints =
						aAllDataPoints.Where(theDp => theDp.DataSource == EProductUniverseTypes.EDF.ToString())
							.ToList();
				}
				else if (aApiProductUniverseType == EProductUniverseTypes.EDF || aApiProductUniverseType == EProductUniverseTypes.MIR)
				{
					aAllDataPoints =
						aAllDataPoints.Where(theDp => theDp.DataSource == aApiProductUniverseType.ToString())
							.ToList();
				}

				var aDataPointPermissionFilter = new DataPointPermissionFilter();
				var aFilterResult = aDataPointPermissionFilter.FilterDataPointPermission(
					aAllDataPoints,
                    aUserPrincipalWrapper.ProductRights,
					aDataPointResultMapping,
					aResultContent);
				aResultContent = aFilterResult;
			}

			switch (dataPointFilterMode)
			{
				case DataPointFilterMode.Single:
					{
						var xmlDoc = new XmlDocument();
						xmlDoc.LoadXml(aResultContent);
						string axPath = string.Empty;
                        var uriPattern = WspContext.Current.GetValue(WsConstants.OPERATION_TEMPLATE) as string;

						var aDpMappingformulas = this.excelAddInConfigDataService.QueryFormulaMapping(uriPattern);
						var aDpMappingformula = aDpMappingformulas.FirstOrDefault();
						if (aDpMappingformula != null)
						{
							axPath = aDpMappingformula.ResultPath;
						}

						if (string.IsNullOrEmpty(axPath))
						{
							WriteXmlData(theStream, string.Empty, theContent);
						}
						else
						{
							var xmlNodeFilter = new XmlNodeFilter(xmlDoc.DocumentElement);
							var dataPointFilteredXmldoc = xmlNodeFilter.FilterByxPath(axPath);
							WriteXmlData(theStream, dataPointFilteredXmldoc.OuterXml, theContent);
						}

						break;
					}

				case DataPointFilterMode.Batch:
					{
						var xmlDoc = new XmlDocument();
						xmlDoc.LoadXml(aResultContent);

						var filter = new DataPointBatchFilter(xmlDoc, this.excelAddinConfigCacheService);
						var resultDocument = filter.BatchFilter();

						WriteXmlData(theStream, resultDocument.OuterXml, theContent);
						break;
					}

				default:
					{
						WriteXmlData(theStream, aResultContent, theContent);
						break;
					}
            }

            #endregion
        }

		private void WriteXmlData(Stream theStream, string theXmlString, HttpContent theContent)
		{
		    var aXmlReaderSettings = new XmlReaderSettings
		    {
		        CheckCharacters = false
		    };

            using (var aXmlReader = XmlReader.Create(new StringReader(theXmlString), aXmlReaderSettings))
            {
                WriteXmlData(theStream, aXmlReader, theContent);
		    }
		}

        private void WriteXmlData(Stream theStream, Stream theXmlStream, HttpContent theContent)
        {
            var aXmlReaderSettings = new XmlReaderSettings
            {
                CheckCharacters = false
            };

            using (var aXmlReader = XmlReader.Create(theXmlStream, aXmlReaderSettings))
            {
                WriteXmlData(theStream, aXmlReader, theContent);
            }
        }

        private void WriteXmlData(Stream theStream, XmlReader theXmlReader, HttpContent theContent)
        {
            theXmlReader.MoveToContent();
            var aEncoding = SelectCharacterEncoding(theContent == null ? null : theContent.Headers);
            using (var aWriter = XmlDictionaryWriter.CreateTextWriter(theStream, aEncoding, false))
            {
                aWriter.WriteNode(theXmlReader, false);
            }
        }

		private string GetSerializedString(object theResult)
		{
            using (var aStreamReader = new StreamReader(GetSerializedStream(theResult)))
			{
				return aStreamReader.ReadToEnd();
			}
		}

        private Stream GetSerializedStream(object theResult)
        {
            var aStream = new MemoryStream();
            var aResultType = theResult.GetType();
            var aSerializer = XmlSerializerUtil.BuildXmlSerializer(aResultType);

            aSerializer.Serialize(aStream, theResult);
            aStream.Position = 0;
            return aStream;
        }
	}
}