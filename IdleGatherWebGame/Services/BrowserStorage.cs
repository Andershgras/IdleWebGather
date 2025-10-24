// Services/BrowserStorage.cs
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace IdleGatherWebGame.Services
{
    public interface IBrowserStorage
    {
        Task<string?> GetAsync(string key);
        Task SetAsync(string key, string value);
        Task RemoveAsync(string key);
    }

    public sealed class BrowserStorage : IBrowserStorage
    {
        private readonly IJSRuntime _js;
        public BrowserStorage(IJSRuntime js) => _js = js;

        public Task<string?> GetAsync(string key)
            => _js.InvokeAsync<string?>("storage.get", key).AsTask();

        public Task SetAsync(string key, string value)
            => _js.InvokeVoidAsync("storage.set", key, value).AsTask();

        public Task RemoveAsync(string key)
            => _js.InvokeVoidAsync("storage.remove", key).AsTask();
    }
}
