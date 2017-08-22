using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Moodys3.WSPlatform.WebService.Contracts.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Moodys3.WSPlatform.WebApi.Extension.Formatter
{
	public class JsonMediaTypeFormatter : MediaTypeFormatterBase
	{
		public JsonMediaTypeFormatter()
		{
			SupportedMediaTypes.Clear();
			MediaTypeMappings.Clear();
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));
            MediaTypeMappings.Add(new QueryStringMapping("alt", "json", "application/json"));
		}

		public override bool CanReadType(Type theType)
		{
			return true;
		}

        public override bool CanWriteType(Type theType)
		{
            return true;
		}

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
		{
			return Task.FromResult(ReadFromStream(type, readStream, content, formatterLogger));
		}

		private object ReadFromStream(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
		{
			HttpContentHeaders contentHeaders = content == null ? null : content.Headers;

			// If content length is 0 then return default value for this type
			if (contentHeaders != null && contentHeaders.ContentLength == 0)
			{
				return GetDefaultValueForType(type);
			}

			// Get the character encoding for the content
			// Never non-null since SelectCharacterEncoding() throws in error / not found scenarios
			Encoding effectiveEncoding = SelectCharacterEncoding(contentHeaders);

			try
			{
				return ReadFromStream(type, readStream, effectiveEncoding, formatterLogger);
			}
			catch (Exception e)
			{
				if (formatterLogger == null)
				{
					throw;
				}

				formatterLogger.LogError(string.Empty, e);
				return GetDefaultValueForType(type);
			}
		}

		private object ReadFromStream(Type type, Stream readStream, Encoding effectiveEncoding, IFormatterLogger formatterLogger)
		{
			using (JsonReader jsonReader = new JsonTextReader(new StreamReader(readStream, effectiveEncoding)))
			{
				jsonReader.CloseInput = false;
				jsonReader.MaxDepth = 256;

				JsonSerializer jsonSerializer = CreateJsonSerializer();

				EventHandler<Newtonsoft.Json.Serialization.ErrorEventArgs> errorHandler = null;
				if (formatterLogger != null)
				{
					errorHandler = (sender, e) =>
						{
							Exception exception = e.ErrorContext.Error;
							formatterLogger.LogError(e.ErrorContext.Path, exception);
							e.ErrorContext.Handled = true;
						};
					jsonSerializer.Error += errorHandler;
				}

				try
				{
					return jsonSerializer.Deserialize(jsonReader, type);
				}
				finally
				{
					if (errorHandler != null)
					{
						// Clean up the error handler in case CreateJsonSerializer() reuses a serializer
						jsonSerializer.Error -= errorHandler;
					}
				}
			}
		}

		private JsonSerializer CreateJsonSerializer()
		{
			JsonSerializer jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				MissingMemberHandling = MissingMemberHandling.Ignore,
				TypeNameHandling = TypeNameHandling.None
			});

			return jsonSerializer;
		}

		public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
		{
			WriteToStream(value, writeStream, content);
			return Task.FromResult<object>(null);
		}

		protected void WriteToStream(object theValue, Stream theStream, HttpContent theContent)
		{
			var aRemovedDataPoints = SetRemovedDataPoint(theValue);
			SetTrafficResponse(theValue, theContent);
			var aPlainTextDto = theValue as DtoPlainText;
			if (aPlainTextDto != null)
			{
				WritePlainText(aPlainTextDto, theContent, theStream, "json");
				return;
			}

			theValue = FormatResponseResult(theValue);

            var aRemovedDataPointDictionary = GetRemovedDatapointsDictionary(aRemovedDataPoints);
			var aSettings = new JsonSerializerSettings();

			if (aRemovedDataPointDictionary.Keys.Any())
			{
				ShouldSerializeContractResolver aResolver = new ShouldSerializeContractResolver(aRemovedDataPointDictionary);
				aSettings.ContractResolver = aResolver;
				aSettings.Context = new StreamingContext(new StreamingContextStates(), aResolver);
			}

			aSettings.Converters.Add(new StringEnumConverter());
			var aEncoding = SelectCharacterEncoding(theContent == null ? null : theContent.Headers);
			using (JsonWriter jsonWriter = new JsonTextWriter(new StreamWriter(theStream, aEncoding)))
			{
				jsonWriter.CloseOutput = false;
				JsonSerializer serializer = JsonSerializer.Create(aSettings);
				serializer.Serialize(jsonWriter, theValue);
				jsonWriter.Flush();
			}
		}

        private Dictionary<string, ICollection<string>> GetRemovedDatapointsDictionary(ICollection<string> theRemovedDatapoints)
		{
			var aRemovedDataPointDictionary = new Dictionary<string, ICollection<string>>();

            if (theRemovedDatapoints == null || !theRemovedDatapoints.Any())
			{
				return aRemovedDataPointDictionary;
			}

			foreach (string aRemovedDataPoint in theRemovedDatapoints)
			{
				string[] aRemovedDataPointPair = aRemovedDataPoint.Split('.');
				if (aRemovedDataPointPair.Length == 2)
				{
					if (aRemovedDataPointDictionary.ContainsKey(aRemovedDataPointPair[0]))
					{
						aRemovedDataPointDictionary[aRemovedDataPointPair[0]].Add(aRemovedDataPointPair[1]);
					}
					else
					{
						aRemovedDataPointDictionary.Add(
							aRemovedDataPointPair[0],
							new List<string> { aRemovedDataPointPair[1] });
					}
				}
			}

			return aRemovedDataPointDictionary;
		}
	}
}
