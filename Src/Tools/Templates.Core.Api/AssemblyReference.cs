using System.Reflection;

namespace Templates.Core.Containers.API;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
