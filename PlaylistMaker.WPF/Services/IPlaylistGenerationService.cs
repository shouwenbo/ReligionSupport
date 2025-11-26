using System.Threading;
using System.Threading.Tasks;
using PlaylistMaker.WPF.Models;

namespace PlaylistMaker.WPF.Services;

public interface IPlaylistGenerationService
{
    Task<PlaylistGenerationResult> GenerateAsync(PlaylistGenerationRequest request, CancellationToken cancellationToken);
}
