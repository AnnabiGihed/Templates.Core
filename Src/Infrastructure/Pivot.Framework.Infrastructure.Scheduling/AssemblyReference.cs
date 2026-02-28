using System.Reflection;

namespace Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
