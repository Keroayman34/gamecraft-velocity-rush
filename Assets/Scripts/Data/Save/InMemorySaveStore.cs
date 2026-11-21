using System;
using VelocityRush.Core;

namespace VelocityRush.Data
{
    /// <summary>
    /// Hermetic, in-memory implementation of <see cref="ISaveStore"/> for testing with fault-injection hooks.
    /// </summary>
    public sealed class InMemorySaveStore : ISaveStore
    {
        private string _storedJson;
        private bool _hasData;

        public bool FailOnLoad { get; set; }
        public bool FailOnSave { get; set; }
        public bool FailOnDelete { get; set; }

        public InMemorySaveStore(string initialJson = null)
        {
            if (initialJson != null)
            {
                _storedJson = initialJson;
                _hasData = true;
            }
        }

        public bool Exists()
        {
            return _hasData;
        }

        public bool TryLoad(out string json, out string error)
        {
            if (FailOnLoad)
            {
                json = null;
                error = "Simulated load failure.";
                return false;
            }

            if (!_hasData)
            {
                json = null;
                error = null;
                return false;
            }

            json = _storedJson;
            error = null;
            return true;
        }

        public bool TrySave(string json, out string error)
        {
            if (FailOnSave)
            {
                error = "Simulated save failure.";
                return false;
            }

            if (json == null)
            {
                error = "Cannot save null payload.";
                return false;
            }

            _storedJson = json;
            _hasData = true;
            error = null;
            return true;
        }

        public bool TryDelete(out string error)
        {
            if (FailOnDelete)
            {
                error = "Simulated delete failure.";
                return false;
            }

            _storedJson = null;
            _hasData = false;
            error = null;
            return true;
        }

        /// <summary>Directly sets a raw payload for testing (e.g., malformed JSON).</summary>
        public void SetRawPayload(string raw)
        {
            _storedJson = raw;
            _hasData = raw != null;
        }
    }
}
