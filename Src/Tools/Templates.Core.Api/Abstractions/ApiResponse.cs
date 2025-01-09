namespace Templates.Core.Containers.API.Abstractions;

public class ApiResponse<T>
{
	public string Message { get; set; } = string.Empty;
	public string ValidationErrors { get; set; } = string.Empty;
	public bool Success { get; set; } = true;
	public T Data { get; set; } = default!;
}
