using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDGlobalSettings : IVersionable<HDGlobalSettings.Version>
    {
        enum Version
        {
            First,
        }

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }
    }
}
