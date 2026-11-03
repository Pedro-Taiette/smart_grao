using SmartGrao.Application.Abstractions;

namespace SmartGrao.Infrastructure.Storage;

internal sealed class FileSystemPhotoStorage(string root) : IPhotoStorage
{
    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    private string PathFor(string key)
    {
        var full = Path.GetFullPath(Path.Combine(root, key));
        if (!full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage key escapes the photo root.");
        return full;
    }
}
