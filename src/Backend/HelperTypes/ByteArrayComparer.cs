// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="ByteArrayComparer.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System.Collections.Generic;

    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Singleton pattern.")]
        public readonly static ByteArrayComparer Instance = new ByteArrayComparer();

        private ByteArrayComparer()
        {
            
        }

        public static int Compare(byte[] a1, byte[] a2)
        {
            if (a1 == a2)
            {
                return 0;
            }
            if (a1 == null)
            {
                return -1;
            }
            if (a2 == null)
            {
                return 1;
            }

            int min = a1.Length;
            if (a2.Length < a1.Length)
            {
                min = a2.Length;
            }

            for (int i = 0; i < min; i++)
            {
                int comp = a1[i] - a2[i];
                if (comp != 0)
                {
                    return comp;
                }
            }

            return a1.Length - a2.Length;
        }

        public bool Equals(byte[] x, byte[] y)
        {
            return (Compare(x, y) == 0);
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null)
            {
                return 0;
            }

            int res = obj.Length.GetHashCode();

            if (obj.Length >= 3)
            {
                res ^= obj[0].GetHashCode();
                res ^= obj[obj.Length - 1].GetHashCode();
                res ^= obj[obj.Length / 2].GetHashCode();
            }

            return res;
        }
    }
}