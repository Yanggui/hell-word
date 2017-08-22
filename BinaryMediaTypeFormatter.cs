using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using Moodys3.WSPlatform.WebService.Contracts.Dtos;

namespace Moodys3.WSPlatform.WebApi.Extension.Formatter
{
    public class BinaryMediaTypeFormatter : MediaTypeFormatterBase
    {
        public BinaryMediaTypeFormatter()
        {
            SupportedMediaTypes.Clear();
            MediaTypeMappings.Clear();
            MediaTypeMappings.Add(new QueryStringMapping("alt", "pdf", "application/pdf"));
            MediaTypeMappings.Add(new QueryStringMapping("alt", "raw", "application/octet-stream"));
            MediaTypeMappings.Add(new QueryStringMapping("alt", "html", "text/html"));
            MediaTypeMappings.Add(new QueryStringMapping("alt", "mobi", "text/html"));
            MediaTypeMappings.Add(new QueryStringMapping("alt", "csv", "text/csv"));
            MediaTypeMappings.Add(new QueryStringMapping("alt", "jpg", "image/jpg"));
            MediaTypeMappings.Add(new QueryStringMapping("alt", "png", "image/png"));
            MediaTypeMappings.Add(new QueryStringMapping("alt", "excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        }

        public override bool CanReadType(Type theType)
        {
            return false;
        }

        public override bool CanWriteType(Type theType)
        {
            return theType == typeof(DtoFile);
        }

        public override Task WriteToStreamAsync(Type theType, object theValue, Stream theWriteStream, HttpContent theContent, TransportContext theTransportContext)
        {
            WriteToStream(theValue, theWriteStream, theContent);
            return Task.FromResult<object>(null);
        }

        protected void WriteToStream(object theValue, Stream theStream, HttpContent theContent)
        {
            SetRemovedDataPoint(theValue);
            SetTrafficResponse(theValue, theContent);

            var aFileResult = theValue as DtoFile;
            if (aFileResult == null || string.IsNullOrEmpty(aFileResult.Base64Content))
            {
                return;
            }

            var aBytesContent = Convert.FromBase64String(aFileResult.Base64Content);
            using (var aWriter = new BinaryWriter(theStream, Encoding.UTF8, true))
            {
                aWriter.Write(aBytesContent);
            }
        }
    }
}