// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="MultiLevelLockPool.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;

    public class MultiLevelLockPool : MultiLevelPool<Node, ILockObject>, IDisposable
    {
        public MultiLevelLockPool(int[] sizesPerLevel, bool lastLevelIsOverflow)
            : base(NewLock, sizesPerLevel, lastLevelIsOverflow)
        {
        }

        private static ILockObject NewLock()
        {
            return new LockObject();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (base.Levels != null)
                    {
                        // dispose managed state (managed objects) and null large fields
                        for (int i = 0; i < base.Levels.Length; i++)
                        {
                            for (int j = 0; j < base.Levels[i].Pool.Length; j++)
                            {
                                base.Levels[i].Pool[j].Dispose();
                            }
                            base.Levels[i].Pool = null;
                        }
                        base.Levels = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}