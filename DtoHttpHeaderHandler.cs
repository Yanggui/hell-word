using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Moodys3.WSPlatform.WebService.Contracts.Dtos;

namespace Moodys3.WSPlatform.WebApi.Extension.Formatter
{
    public class DtoHttpHeaderHandler : DelegatingHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage theRequest, CancellationToken theCancellationToken)
        {
            var aHttpResponseMessage = await base.SendAsync(theRequest, theCancellationToken);
            if (aHttpResponseMessage.IsSuccessStatusCode && aHttpResponseMessage.Content != null)
            {
                var aObjectContent = aHttpResponseMessage.Content as ObjectContent;
                if (aObjectContent == null)
                {
                    return aHttpResponseMessage;
                }

                if (aObjectContent.ObjectType == typeof(DtoFile))
                {
                    var aDtoFile = (DtoFile)aObjectContent.Value;

                    if (!aDtoFile.NeedDisplayDirectly)
                    {
                        aHttpResponseMessage.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = string.Format("\"{0}.{1}\"", aDtoFile.FileName, aDtoFile.MimeType)
                        };
                    }

                    var aContentType = GetContentType(aDtoFile);
                    if (!string.IsNullOrEmpty(aContentType))
                    {
                        aHttpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(aContentType);
                    }
                }
                else if (aObjectContent.ObjectType == typeof(DtoPlainText))
                {
                    var aContentType = "text/xml";
                    if (aObjectContent.Formatter.GetType() == typeof(JsonMediaTypeFormatter))
                    {
                        aContentType = "application/json";
                    }

                    aHttpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(aContentType);
                }
            }

            return aHttpResponseMessage;
        }

        private string GetContentType(DtoFile theFile)
        {
            string contentType = string.Empty;
            if (theFile != null)
            {
                switch (theFile.MimeType)
                {
                    case "html":
                        {
                            contentType = "text/html";
                        }

                        break;

                    case "pdf":
                        {
                            contentType = "application/pdf";
                        }

                        break;

                    case "xls":
                        {
                            contentType = "application/vnd.ms-excel";
                        }

                        break;

                    case "xlsx":
                        {
                            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        }

                        break;

                    case "csv":
                        {
                            contentType = "text/csv";
                        }

                        break;

                    case "gif":
                        {
                            contentType = "image/gif";
                        }

                        break;

                    case "jpg":
                    case "jpeg":
                        {
                            contentType = "image/jpeg";
                        }

                        break;

                    case "png":
                        {
                            contentType = "image/png";
                        }

                        break;

                    case "svg":
                        {
                            contentType = "image/svg+xml";
                        }

                        break;

                    default:
                        contentType = "application/octet-stream";

                        break;
                }
            }

            return contentType;
        }
    }
}