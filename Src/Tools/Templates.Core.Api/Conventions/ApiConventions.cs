using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Templates.Core.Tools.API.Conventions;
public static class ApiConventions
{
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public static void Default<T>() { }

	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public static void DefaultNoNotFound<T>() { }
}