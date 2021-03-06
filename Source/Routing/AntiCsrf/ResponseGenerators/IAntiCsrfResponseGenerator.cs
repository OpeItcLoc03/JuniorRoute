﻿using System.Threading.Tasks;

using Junior.Route.Routing.AntiCsrf.NonceValidators;

namespace Junior.Route.Routing.AntiCsrf.ResponseGenerators
{
	public interface IAntiCsrfResponseGenerator
	{
		Task<ResponseResult> GetResponseAsync(ValidationResult result);
	}
}