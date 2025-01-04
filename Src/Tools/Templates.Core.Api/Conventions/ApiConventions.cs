using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Templates.Core.Domain.Shared;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Templates.Core.Tools.API.Conventions;

public static class ApiConventions
{
	/// <summary>
	/// Convention for actions returning Task&lt;ActionResult&lt;Result&lt;T&gt;&gt;&gt;.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<object>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
	public static void Retrive(
		[ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)] params object[] parameters
	)
	{ }

	/// <summary>
	/// Convention for actions returning Task&lt;ActionResult&lt;Result&lt;T&gt;&gt;&gt;.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<object>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
	public static void Get(
		[ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)] params object[] parameters
	)
	{ }

	/// <summary>
	/// Convention for actions returning Task&lt;ActionResult&lt;Result&lt;T&gt;&gt;&gt; for PUT requests.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<object>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
	public static void Update(
		[ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)] params object[] parameters
	)
	{ }

	/// <summary>
	/// Convention for actions returning no content (204).
	/// </summary>
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
	public static void Delete(
		[ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)] params object[] parameters
	)
	{ }

	/// <summary>
	/// Convention for POST actions returning Task&lt;ActionResult&lt;Result&lt;T&gt;&gt;&gt;.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Result<object>))] // Use object as a generic placeholder
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)] // Matches method names starting with "Add" or "Post"
	public static void Add(
		[ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)] params object[] parameters
	)
	{
	}
}
