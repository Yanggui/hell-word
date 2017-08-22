using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Moodys3.WSPlatform.WebService.Contracts.Dtos;
using ProtoBuf.Meta;

namespace Moodys3.WSPlatform.WebApi.Extension.Formatter
{
	public class ProtobufMediaTypeFormatter : MediaTypeFormatterBase
	{
		private static readonly Lazy<RuntimeTypeModel> ProtoBufModel = new Lazy<RuntimeTypeModel>(CreateProtoBufTypeModel);

		private static RuntimeTypeModel CreateProtoBufTypeModel()
		{
			var typeModel = TypeModel.Create();
			typeModel.UseImplicitZeroDefaults = false;

			return typeModel;
		}

		public ProtobufMediaTypeFormatter()
		{
			SupportedMediaTypes.Clear();
			MediaTypeMappings.Clear();
			MediaTypeMappings.Add(new QueryStringMapping("alt", "protobuf", "application/x-protobuf"));
		}

		public override bool CanReadType(Type theType)
		{
			return false;
		}

        public override bool CanWriteType(Type theType)
		{
            return theType != typeof(DtoFile) && theType != typeof(DtoPlainText);
		}

		protected void WriteToStream(object theValue, HttpContent theContent, Stream theStream)
		{
			SetRemovedDataPoint(theValue);
			SetTrafficResponse(theValue, theContent);

			ProtoBufModel.Value.Serialize(theStream, theValue);
		}

        public override Task WriteToStreamAsync(Type theType, object theValue, Stream theWriteStream, HttpContent theContent, TransportContext theTransportContext)
		{
            WriteToStream(theValue, theContent, theWriteStream);
			return Task.FromResult<object>(null);
		}
	}
}