﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prism.Mods;
using Prism.Util;

namespace Prism.API.Defs
{
    public abstract class EntityRef<TDef> : IEquatable<EntityRef<TDef>>
    {
        Lazy<string> resName;

        public string ResourceName
        {
            get
            {
                return resName.Value;
            }
        }
        public string ModName
        {
            get;
            private set;
        }

        protected ModDef Requesting
        {
            get;
            private set;
        }

        public bool IsVanillaRef
        {
            get
            {
                return String.IsNullOrEmpty(ModName) || ModName == PrismApi.VanillaString || ModName == PrismApi.TerrariaString;
            }
        }

        public ModInfo Mod
        {
            get
            {
                return String.IsNullOrEmpty(ModName) ? ModInfo.Empty : (IsVanillaRef ? PrismApi.VanillaInfo : ModData.mods.Keys.FirstOrDefault(mi => mi.InternalName == ModName));
            }
        }

        protected internal EntityRef(Func<string> toResName)
        {
            resName = new Lazy<string>(toResName ?? Empty<string>.Func);
        }
        protected EntityRef(ObjectRef objRef, Assembly calling)
            : this(() => objRef.Name)
        {
            resName = new Lazy<string>(() => objRef.Name);

            ModName = objRef.ModName;

            Requesting = ModData.ModFromAssembly(calling);
        }

        public abstract TDef Resolve();

        public virtual bool Equals(EntityRef<TDef> other)
        {
            return ResourceName == other.ResourceName && Mod == other.Mod;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            if (obj is EntityRef<TDef>)
                return Equals((EntityRef<TDef>)obj);

            return false;
        }
        public override int GetHashCode()
        {
            return ResourceName.GetHashCode() + Mod.GetHashCode();
        }
        public override string ToString()
        {
            return String.IsNullOrEmpty(ResourceName) ? "<empty>" : ("{" + Mod.InternalName + "." + ResourceName + "}");
        }

        public static implicit operator ObjectRef(EntityRef<TDef> e)
        {
            return new ObjectRef(e.ResourceName, e.ModName)
            {
                requesting = e.Requesting
            };
        }
    }
    public abstract class EntityRefWithId<TDef> : EntityRef<TDef>
    {
        public int? ResourceID
        {
            get;
            private set;
        }

        protected EntityRefWithId(int resourceId, Func<int, string> toResName)
            : base(() => resourceId == 0 ? String.Empty : toResName(resourceId))
        {
            ResourceID = resourceId;
        }
        protected EntityRefWithId(ObjectRef objRef, Assembly calling)
            : base(objRef, calling)
        {

        }

        public override bool Equals(EntityRef<TDef> other)
        {
            if (ResourceID.HasValue && other is EntityRefWithId<TDef>)
            {
                var erid = (EntityRefWithId<TDef>)other;

                if (erid.ResourceID.HasValue)
                    return ResourceID.Value == erid.ResourceID.Value;
            }

            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            return ResourceID.HasValue ? ResourceID.GetHashCode() : base.GetHashCode();
        }
        public override string ToString()
        {
            return (ResourceID.HasValue ? ("#" + ResourceID.Value + " ") : String.Empty) + base.ToString();
        }
    }
}
