﻿using System;
using System.Reflection;

using Junior.Common;

namespace Junior.Route.AutoRouting.IdMappers
{
	public class RandomIdMapper : IIdMapper
	{
		private readonly IGuidFactory _guidFactory;

		public RandomIdMapper(IGuidFactory guidFactory)
		{
			guidFactory.ThrowIfNull("guidFactory");

			_guidFactory = guidFactory;
		}

		public IdResult Map(Type type, MethodInfo method)
		{
			type.ThrowIfNull("type");
			method.ThrowIfNull("method");

			return IdResult.IdMapped(_guidFactory.Random());
		}
	}
}