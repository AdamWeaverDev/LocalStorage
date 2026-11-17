#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage.Serialization;
using Blazored.LocalStorage.StorageOptions;
using Blazored.LocalStorage.TestExtensions;
using Xunit;

namespace Blazored.LocalStorage.Tests.LocalStorageServiceTests
{
    public class Regressions
    {
        private static LocalStorageService Create(IStorageProvider provider)
            => new(provider, new SystemTextJsonSerializer(new LocalStorageOptions()));

        [Fact]
        public async Task SetItemAsync_DoesNotThrow_When_ExistingValueIsPlainStringAndTypeIsNotString()
        {
            var provider = new InMemoryStorageProvider();
            provider.SetItem("key", "not json");
            var sut = Create(provider);

            await sut.SetItemAsync("key", 42);

            Assert.Equal("42", provider.GetItem("key"));
        }

        [Fact]
        public async Task SetItemAsync_OldValueIsNull_When_KeyMissing_MatchingSyncApi()
        {
            object? asyncOld = "unset", syncOld = "unset";
            var sut = Create(new InMemoryStorageProvider());
            sut.Changing += (_, e) => asyncOld = e.OldValue;
            await sut.SetItemAsync("a", 5);

            var sut2 = Create(new InMemoryStorageProvider());
            sut2.Changing += (_, e) => syncOld = e.OldValue;
            sut2.SetItem("a", 5);

            Assert.Null(syncOld);
            Assert.Equal(syncOld, asyncOld);
        }

        [Fact]
        public async Task SetItemAsync_PassesCancellationTokenToStorageProvider_When_ReadingOldValue()
        {
            using var cts = new CancellationTokenSource();
            var provider = new RecordingProvider();
            var sut = Create(provider);

            await sut.SetItemAsync("key", 1, cts.Token);
            await sut.SetItemAsStringAsync("key", "x", cts.Token);

            Assert.Equal(2, provider.GetTokens.Count);
            Assert.All(provider.GetTokens, t => Assert.Equal(cts.Token, t));
        }

        private sealed class RecordingProvider : IStorageProvider
        {
            private readonly InMemoryStorageProvider _inner = new();
            public List<CancellationToken> GetTokens { get; } = new();

            public ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default)
            {
                GetTokens.Add(cancellationToken);
                return _inner.GetItemAsync(key, cancellationToken);
            }

            public void Clear() => _inner.Clear();
            public ValueTask ClearAsync(CancellationToken cancellationToken = default) => _inner.ClearAsync(cancellationToken);
            public bool ContainKey(string key) => _inner.ContainKey(key);
            public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default) => _inner.ContainKeyAsync(key, cancellationToken);
            public string? GetItem(string key) => _inner.GetItem(key);
            public string? Key(int index) => _inner.Key(index);
            public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default) => _inner.KeyAsync(index, cancellationToken);
            public IEnumerable<string> Keys() => _inner.Keys();
            public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default) => _inner.KeysAsync(cancellationToken);
            public int Length() => _inner.Length();
            public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default) => _inner.LengthAsync(cancellationToken);
            public void RemoveItem(string key) => _inner.RemoveItem(key);
            public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default) => _inner.RemoveItemAsync(key, cancellationToken);
            public void RemoveItems(IEnumerable<string> keys) => _inner.RemoveItems(keys);
            public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default) => _inner.RemoveItemsAsync(keys, cancellationToken);
            public void SetItem(string key, string data) => _inner.SetItem(key, data);
            public ValueTask SetItemAsync(string key, string data, CancellationToken cancellationToken = default) => _inner.SetItemAsync(key, data, cancellationToken);
        }

        [Fact]
        public async Task RemoveItemsAsync_ThrowsArgumentNullException_When_KeysIsNull()
        {
            var sut = Create(new InMemoryStorageProvider());

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.RemoveItemsAsync(null!));
            Assert.Equal("keys", ex.ParamName);
        }
    }
}
