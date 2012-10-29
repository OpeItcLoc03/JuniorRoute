﻿using System;
using System.Text;

namespace Junior.Route.Routing.Responses.Application
{
	public class OggResponse : ImmutableResponse
	{
		public OggResponse(Func<byte[]> content, Action<Response> configurationDelegate = null)
			: base(Response.OK().ApplicationOgg().Content(content), configurationDelegate)
		{
		}

		public OggResponse(Func<byte[]> content, Encoding encoding, Action<Response> configurationDelegate = null)
			: base(Response.OK().ApplicationOgg().ContentEncoding(encoding).Content(content), configurationDelegate)
		{
		}

		public OggResponse(byte[] content, Action<Response> configurationDelegate = null)
			: base(Response.OK().ApplicationOgg().Content(content), configurationDelegate)
		{
		}

		public OggResponse(byte[] content, Encoding encoding, Action<Response> configurationDelegate = null)
			: base(Response.OK().ApplicationOgg().ContentEncoding(encoding).Content(content), configurationDelegate)
		{
		}
	}
}