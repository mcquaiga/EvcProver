﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Entity.cs" company="">
//   
// </copyright>
// <summary>
//   Entity base class for domain objects
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Reflection;

namespace Prover.Shared.Domain
{
    /// <summary>
    ///     Entity base class for domain objects
    /// </summary>
    /// <typeparam name="TId">
    ///     Id type
    /// </typeparam>
    public abstract class Entity
    {
        protected Entity(Guid id)
        {
            Id = id;
        }

        /// <summary>
        ///     Gets or sets the id.
        /// </summary>
        public Guid Id { get; set; }

        //public override bool Equals(object obj)
        //{
        //    return Equals(obj as Entity<TId>);
        //}

        //private static bool IsTransient(Entity<TId> obj)
        //{
        //    return obj != null && Equals(obj.Id, default(int));
        //}

        //private Type GetUnproxiedType()
        //{
        //    return GetType();
        //}

        //public virtual bool Equals(Entity<TId> other)
        //{
        //    if (other == null)
        //        return false;

        //    if (ReferenceEquals(this, other))
        //        return true;

        //    if (!IsTransient(this) &&
        //        !IsTransient(other) &&
        //        Equals(Id, other.Id))
        //    {
        //        var otherType = other.GetUnproxiedType();
        //        var thisType = GetUnproxiedType();
        //        return thisType.IsAssignableFrom(otherType) ||
        //                otherType.IsAssignableFrom(thisType);
        //    }

        //    return false;
        //}

        //public override int GetHashCode()
        //{
        //    if (Equals(Id, default(int)))
        //        return base.GetHashCode();
        //    return Id.GetHashCode();
        //}

        //public static bool operator ==(Entity<TId> x, Entity<TId> y)
        //{
        //    return Equals(x, y);
        //}

        //public static bool operator !=(Entity<TId> x, Entity<TId> y)
        //{
        //    return !(x == y);
        //}
    }
}