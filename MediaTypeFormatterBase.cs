using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Text;
using Moodys3.WSPlatform.Common;
using Moodys3.WSPlatform.Common.Attributes;
using Moodys3.WSPlatform.Common.WebContext;
using Moodys3.WSPlatform.WebApi.Extension.Utils;
using Moodys3.WSPlatform.WebService.Common;
using Moodys3.WSPlatform.WebService.Contracts.Dtos;
using Moodys3.WSPlatform.WebService.Contracts.Dtos.WrappedResponse;
using Moodys3.WSPlatform.WebService.Contracts.Utils;

namespace Moodys3.WSPlatform.WebApi.Extension.Formatter
{
	public abstract class MediaTypeFormatterBase : MediaTypeFormatter
	{
		protected MediaTypeFormatterBase()
        {
            SupportedEncodings.Clear();
			SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
			SupportedEncodings.Add(new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true));
		}

		protected ICollection<string> SetRemovedDataPoint(object theResult)
		{
			ICollection<string> result = new List<string>();
			string theRequestDataPoint = WsRequestQueryParametersHelper.Instance.GetValue(WsConstants.QUERYPARAMETER_DATA_POINT_FILTER);
			if (string.IsNullOrEmpty(theRequestDataPoint))
			{
				return result;
			}

			string[] aDataPointArray = theRequestDataPoint.Split('-');
			List<Type> aResultDataType = new List<Type>();

			Type aReultType = theResult.GetType();
			if (aReultType.IsGenericType)
			{
				Type[] aGenericItemTypeArray = aReultType.GetGenericArguments();
				aResultDataType.AddRange(aGenericItemTypeArray);
			}
			else if (aReultType.IsArray)
			{
				Type aArrayItemType = aReultType.GetElementType();
				aResultDataType.Add(aArrayItemType);
			}
			else
			{
				aResultDataType.Add(aReultType);
			}

			foreach (var aTypeItem in aResultDataType)
			{
				if (!(aTypeItem.IsPrimitive || aTypeItem == typeof(string)))
				{
					string aClassTypeName = aTypeItem.Name;
					PropertyInfo[] aProperties = aTypeItem.GetProperties();
					foreach (PropertyInfo aProperty in aProperties)
					{
						DataPointFilterAttribute aDataPointFilterAttribute = aProperty.GetCustomAttribute<DataPointFilterAttribute>(true);
						if (aDataPointFilterAttribute != null)
						{
							string aDataPointName = aDataPointFilterAttribute.DataPointName;
							if (!aDataPointArray.Contains(aDataPointName, StringComparer.InvariantCultureIgnoreCase))
							{
								string aRemovedDataPointName = string.Format("{0}.{1}", aClassTypeName, aProperty.Name);
								ICollection<string> removed;
                                if (WspContext.Current == null)
								{
									return result;
								}

                                if (WspContext.Current.GetValue(WsConstants.REMOVED_DATA_POINTS) != null)
								{
                                    removed = WspContext.Current.GetValue(WsConstants.REMOVED_DATA_POINTS) as ICollection<string>;
								}
								else
								{
									removed = new List<string>();
								}

								if (!removed.Contains(aRemovedDataPointName))
								{
									removed.Add(aRemovedDataPointName);
								}

								result = removed;
                                WspContext.Current.SetValue(WsConstants.REMOVED_DATA_POINTS, result);
							}
						}
					}
				}
			}

			return result;
		}

		protected void SetTrafficResponse(object theValue, HttpContent theContent)
		{
			var aDtoBaseObjectResult = theValue as DtoBaseObject;
			if (aDtoBaseObjectResult != null)
			{
                WspContext.Current.SetValue(UriTemplates.TRAFFIC_RESPONSE, aDtoBaseObjectResult.TrafficResponse);
			}
		}

		protected void WritePlainText(DtoPlainText theValue, HttpContent theContent, Stream theStream, string theFormatType)
		{
			if (theValue == null)
			{
				return;
			}

			string aTextValue;
			switch (theFormatType)
			{
				case "xml":
					aTextValue = theValue.XmlText;
					break;
				case "json":
					aTextValue = theValue.JsonText;
					break;
				default:
					aTextValue = theValue.XmlText;
					break;
			}

			if (!string.IsNullOrEmpty(aTextValue))
			{
				using (var aWriter = new StreamWriter(theStream, Encoding.UTF8, Encoding.UTF8.GetByteCount(aTextValue), true))
				{
					aWriter.Write(aTextValue);
				}
			}
		}

		protected object FormatResponseResult(object theResult)
		{
			if (!IsWrappedResponse())
			{
				return theResult;
			}

			var aResponse = new DtoResponse();
			var aError = theResult as DtoWspError;
			if (aError != null)
			{
				aResponse.Code = (int)aError.StatusCode;
				aResponse.Status = "fail";
				aResponse.Message = aError.ErrorDescription;
				aResponse.Data = aError.StatusCode == HttpStatusCode.InternalServerError ? aError.Id.ToString() : null;
			}
			else
			{
				aResponse.Code = 200;
				aResponse.Status = "success";
				aResponse.Data = theResult;
			}

			return aResponse;
		}

		private bool IsWrappedResponse()
		{
			bool isWrappedResponse;
			string aIsWrappedResponseStr = WsRequestQueryParametersHelper.Instance.GetValue(WsConstants.QUERYPARAMETER_IsWrappedResponse);
			if (!bool.TryParse(aIsWrappedResponseStr, out isWrappedResponse))
			{
				isWrappedResponse = false;
			}

			return isWrappedResponse;
		}
	}
}
