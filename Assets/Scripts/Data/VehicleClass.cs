namespace VelocityRush.Data
{
    /// <summary>
    /// Commercial roster class taxonomy (D-014 planning distribution: 2 Starter / 3 Sport /
    /// 2 Drift / 3 Supercar = 10 vehicles).
    /// </summary>
    /// <remarks>
    /// The distribution is <b>provisional planning data</b>. The enum is stable for asset
    /// authoring; enum values are serialized by index, so append new classes rather than
    /// reordering existing members.
    /// </remarks>
    public enum VehicleClass
    {
        /// <summary>Entry class focused on learning and forgiveness.</summary>
        Starter = 0,

        /// <summary>Balanced general-use class.</summary>
        Sport = 1,

        /// <summary>Drift-biased handling class.</summary>
        Drift = 2,

        /// <summary>High-capability, hard-to-master class.</summary>
        Supercar = 3
    }
}
