using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Moodys3.WSPlatform.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Moodys3.WSPlatform.WebApi.Extension.Formatter
{
	public class ShouldSerializeContractResolver : DefaultContractResolver
	{
		private Dictionary<string, ICollection<string>> removedDataPoints;

		public ShouldSerializeContractResolver(Dictionary<string, ICollection<string>> theRemovedDataPoints)
		{
			removedDataPoints = theRemovedDataPoints;
		}

		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			JsonProperty property = base.CreateProperty(member, memberSerialization);

			if (removedDataPoints != null)
			{
				property.ShouldSerialize = aInstance =>
				{
					bool shouldSerialize = true;
					Type aInstanceType = aInstance.GetType();
					string aSpecifiedPropertyName = property.UnderlyingName + WsConstants.SPECIFIED;
					PropertyInfo aSpecifiedProperty = aInstanceType.GetProperty(aSpecifiedPropertyName);
					if (aSpecifiedProperty != null)
					{
						object aSpecifiedPropertyValue = aSpecifiedProperty.GetValue(aInstance, null);
						bool? aIsSpecified = aSpecifiedPropertyValue as bool?;
						if (aIsSpecified != null)
						{
							shouldSerialize = aIsSpecified.Value;
						}
					}

					string aCurrentTypeName = aInstanceType.Name;
					if (removedDataPoints.ContainsKey(aCurrentTypeName)
					&& removedDataPoints[aCurrentTypeName].Contains(property.UnderlyingName))
					{
						shouldSerialize = false;
					}

					return shouldSerialize;
				};
			}

			return property;
		}
	}
}
