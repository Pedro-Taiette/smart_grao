namespace SmartGrao.Application.Abstractions;

/// <summary>Onde o arquivo da foto mora. Disco no desenvolvimento; armazenamento de objetos no ambiente publicado.</summary>
public interface IPhotoStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken);

    Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken);
}
