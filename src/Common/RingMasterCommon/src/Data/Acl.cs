// <copyright file="Acl.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;

    /// <summary>
    /// Access control entry that specifies the permissions that apply to an actor with a particular <see cref="Id"/>.
    /// </summary>
    public class Acl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Acl"/> class.
        /// </summary>
        public Acl()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Acl"/> class.
        /// </summary>
        /// <param name="perms">Permissions that apply to the actor</param>
        /// <param name="id"><see cref="Id"/> of the actor</param>
        public Acl(int perms, Id id)
        {
            this.Perms = perms;
            this.Id = id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Acl"/> class.
        /// </summary>
        /// <param name="other">The <see cref="Acl"/> instance to copy</param>
        public Acl(Acl other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            this.Perms = other.Perms;
            if (other.Id != null)
            {
                this.Id = new Id(other.Id);
            }
        }

        /// <summary>
        /// Permissions that specify the type of operations allowed on a node.
        /// </summary>
        [Flags]
        public enum Perm : int
        {
            /// <summary>
            /// No Permission to do any operation.
            /// </summary>
            NONE = 0,

            /// <summary>
            /// Allow creation of child nodes.
            /// </summary>
            CREATE = 1,

            /// <summary>
            /// Allow read of node data.
            /// </summary>
            READ = 2,

            /// <summary>
            /// Allow modification of node data.
            /// </summary>
            WRITE = 4,

            /// <summary>
            /// Allow deletion of child nodes.
            /// </summary>
            DELETE = 8,

            /// <summary>
            /// Allow Administrator level permissions.
            /// </summary>
            ADMIN = 16,

            /// <summary>
            /// Full permission to do any operation.
            /// </summary>
            ALL = 31
        }

        /// <summary>
        /// Gets the <see cref="Id"/> of the actor whose permissions are specified by this <see cref="Acl"/>.
        /// </summary>
        public Id Id { get; private set; }

        /// <summary>
        /// Gets the permissions that apply to the actor.
        /// </summary>
        public int Perms { get; private set; }

        /// <summary>
        /// Checks if the given <see cref="Acl"/>s are equal.
        /// </summary>
        /// <param name="lhs">One <see cref="Acl"/></param>
        /// <param name="rhs">Another <see cref="Acl"/></param>
        /// <returns><c>true</c>if <paramref name="lhs"/> is equal to <paramref name="rhs"/></returns>
        public static bool AreEqual(Acl lhs, Acl rhs)
        {
            if ((lhs == null) || (rhs == null))
            {
                return false;
            }

            if (lhs.Id.Scheme != rhs.Id.Scheme)
            {
                return false;
            }

            if (lhs.Id.Identifier != rhs.Id.Identifier)
            {
                return false;
            }

            if (lhs.Perms != rhs.Perms)
            {
                return false;
            }

            return true;
        }
    }
}
