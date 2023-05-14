using DNToolKit.Extensions;
using Serilog;

namespace DNToolKit.AnimeGame.Crypto
{
    static class KeyBruteForcer
    {
        private static readonly List<long> PrevSeeds = new();

        /// <summary>
        /// Brute force the seed by guessing over the request data.
        /// </summary>
        /// <param name="requestData">The data to guess the seed from.</param>
        /// <param name="serverKey">The key received from the server.</param>
        /// <param name="sendTimes">The timestamps of requests to use.</param>
        /// <returns>A tuple of the brute forced key to decrypt requests with and the guessed seed.
        /// Otherwise, a tuple of <see langword="null"/>.</returns>
        public static (MtKey?, long?) BruteForce(byte[] requestData, ulong serverKey, params long?[] sendTimes)
        {
            // Check against already guessed seeds
            foreach (var oldSeed in PrevSeeds)
            {
                var key = Guess(requestData, oldSeed, serverKey, 1000);
                if (key != null)
                    return (key, oldSeed);
            }

            foreach (var sendTime in sendTimes)
            {
                if (sendTime is null) continue;
                // Check against our arguments with timeStamp offset
                // Effective range of the loop is -1499..1499
                for (var count = 0; count < 3000; count++)
                {
                    // Special case: 1 would result in 0 again, which we already checked for count = 0
                    if (count == 1)
                        continue;

                    // Alternate between negative and positive offset
                    var offset = count >> 1;
                    if ((count & 1) > 0)
                        offset = -offset;

                    var seed = sendTime.Value + offset;
                    var key = Guess(requestData, seed, serverKey, 1000);
                    if (key == null)
                        continue;

                    // Save found seed
                    PrevSeeds.Add(seed);
                    return (key, seed);
                }
            }

            // If we didn't find the correct key
            return (null, null);
        }

        /// <summary>
        /// Guess the correct key for a given <paramref name="timeStamp"/>.
        /// </summary>
        /// <param name="testBuffer">The buffer to guess on.</param>
        /// <param name="timeStamp">The timestamp to initialize <see cref="Random"/> with.</param>
        /// <param name="serverKey">The key sent from the server.</param>
        /// <param name="depth">The number of iterations to guess for.</param>
        /// <returns>The created <see cref="MtKey"/>, if a correct seed was found. Otherwise <see langword="null"/>.</returns>
        private static MtKey? Guess(byte[] testBuffer, long timeStamp, ulong serverKey, int depth)
        {
            var keyPrefix = new[] { (byte)(testBuffer[0] ^ 0x45), (byte)(testBuffer[1] ^ 0x67) };
            var keySuffix = new[] { (byte)(testBuffer[^2] ^ 0x89), (byte)(testBuffer[^1] ^ 0xAB) };

            // Check up to depth since static random in client is reused on exiting coop and re-login to server
            var rand = new Random((int)timeStamp);
            for (var i = 0; i < depth; i++)
            {
                var clientSeed = rand.NextUInt64();
                var seed = serverKey ^ clientSeed;

                // Check data prefix
                var key = MtKey.Generate(seed, 8);
                if (key[0] != keyPrefix[0] || key[1] != keyPrefix[1])
                    continue;

                // Check data suffix
                var full = MtKey.Generate(seed);
                if (full[(testBuffer.Length - 2) % MtKey.BufferLength] != keySuffix[0] ||
                    full[(testBuffer.Length - 1) % MtKey.BufferLength] != keySuffix[1])
                    continue;

                // Return key for found seed
                Log.Debug("Seed found! {seed}", seed);
                Log.Verbose("Params: @{requestData} : {timeStamp} : {serverKey} : x{i}", testBuffer, timeStamp, serverKey, i);

                return MtKey.FromBuffer(full);
            }

            return null;
        }
    }
}

