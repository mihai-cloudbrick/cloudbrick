using Orleans.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Grains
{
    internal static class GrainStateExtensions
    {

        public static async Task WriteWithRetry<T>(this IPersistentState<T> state, T mutate, int maxAttempts = 20)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    state.State = mutate;
                    await state.WriteStateAsync();
                    return;
                }
                catch (InconsistentStateException) when (attempt < maxAttempts)
                {
                    // Refresh and reapply the mutation
                    await state.ReadStateAsync();
                    await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt)); // small backoff
                }
            }
            // Let the last exception bubble if we exhausted retries
            state.State = mutate;
            await state.WriteStateAsync();
        }
        /// <summary>
        /// Reads the latest state/Etag, applies a mutation, and writes it, with small retries if ETag races occur.
        /// </summary>
        public static async Task MutateAndSaveAsync<T>(
            this IPersistentState<T> ps,
            Action<T> mutate,
            int maxAttempts = 3)
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await ps.ReadStateAsync();      // refresh to latest ETag
                    mutate(ps.State);               // apply caller's changes
                    await ps.WriteStateAsync();     // persist
                    return;
                }
                catch (InconsistentStateException) when (attempt < maxAttempts)
                {
                    // brief backoff then retry on fresh read
                    await Task.Delay(25 * attempt);
                }
            }

            // last attempt, let it throw if it still fails
            await ps.ReadStateAsync();
            mutate(ps.State);
            await ps.WriteStateAsync();
        }
    }
}
