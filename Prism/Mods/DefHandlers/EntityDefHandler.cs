﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prism.API.Behaviours;
using Prism.API.Defs;
using Terraria;

namespace Prism.Mods.DefHandlers
{
    /// <summary>
    /// Handles all the entity defs.
    /// </summary>
    abstract class EntityDefHandler<TEntityDef, TBehaviour, TEntity>
        where TEntity : class
        where TBehaviour : EntityBehaviour<TEntity>
        where TEntityDef : EntityDef<TBehaviour, TEntity>//, new()
    {
        /// <summary>
        /// The entity type index to which the next entity added will be assigned;
        /// </summary>
        internal int NextTypeIndex;

        protected static bool FillingVanilla = false;

        public Dictionary<int   , TEntityDef> DefsByType        = new Dictionary<int   , TEntityDef>();
        public Dictionary<string, TEntityDef> VanillaDefsByName = new Dictionary<string, TEntityDef>();

        int? minVanillaId = null, maxVanillaId = null;
        FieldInfo[] idFields = null;
        int[] idValues = null;
        string[] idNames = null;

        internal FieldInfo[] IDFields
        {
            get
            {
                if (idFields == null)
                    idFields = IDContainerType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType.IsPrimitive && f.FieldType != typeof(bool) && f.Name != "Count").ToArray();

                return idFields;
            }
        }
        internal int[] IDValues
        {
            get
            {
                if (idValues == null)
                    idValues = IDFields.Select(f => (int)Convert.ChangeType(f.GetValue(null), typeof(int))).ToArray();

                return idValues;
            }
        }
        internal string[] IDNames
        {
            get
            {
                if (idNames == null)
                    idNames = IDFields.Select(f => f.Name).ToArray();

                return idNames;
            }
        }

        protected int MinVanillaID
        {
            get
            {
                if (minVanillaId == null)
                    minVanillaId = IDFields.Select(f => (int)Convert.ChangeType(f.GetValue(null), typeof(int))).Min();

                return minVanillaId.Value;
            }
        }
        protected int MaxVanillaID
        {
            get
            {
                if (maxVanillaId == null)
                    maxVanillaId = (int)Convert.ChangeType(IDContainerType.GetField("Count", BindingFlags.Public | BindingFlags.Static).GetValue(null), typeof(int));

                return maxVanillaId.Value;
            }
        }

        protected abstract Type IDContainerType
        {
            get;
        }

        protected EntityDefHandler()
        {
            Reset();
        }

        protected abstract void ExtendVanillaArrays(int amt = 1);

        protected abstract TEntity GetVanillaEntityFromID(int id);
        protected abstract TEntityDef NewDefFromVanilla(TEntity entity);
        protected abstract string GetNameVanillaMethod(TEntity entity);

        protected abstract void CopyEntityToDef(TEntity entity, TEntityDef def);
        protected abstract void CopyDefToEntity(TEntityDef def, TEntity entity);

        protected abstract List<LoaderError> CheckTextures(TEntityDef def);
        protected abstract List<LoaderError> LoadTextures (TEntityDef def);
        protected abstract void CopySetProperties(TEntityDef def);

        protected abstract int GetRegularType(TEntity entity);

        protected virtual string InternalName(TEntity entity)
        {
            return null;
        }

        protected virtual void PostFillVanilla() { }

        internal void FillVanilla()
        {
            FillingVanilla = true;

            int id = 0;

            var def = NewDefFromVanilla(GetVanillaEntityFromID(id));
            def.InternalName = String.Empty;

            DefsByType.Add(id, def);
            VanillaDefsByName.Add(String.Empty, def);

            var byDisplayName = new Dictionary<string, TEntityDef>();

            for (id = MinVanillaID; id < MaxVanillaID; id++)
            {
                if (id == 0)
                    continue;

                var index = Array.IndexOf(IDValues, id);
                if (index == -1)
                    continue;

                var entity = GetVanillaEntityFromID(id);
                def = NewDefFromVanilla(entity);

                var iname_ = InternalName(entity);
                var iname = String.IsNullOrEmpty(iname_) ? IDNames[index] : iname_;

                DefsByType.Add(id, def);
                VanillaDefsByName.Add(iname, def);

                var n = GetNameVanillaMethod(entity);
                if (!byDisplayName.ContainsKey(n) && !VanillaDefsByName.ContainsKey(n))
                    byDisplayName.Add(n, def);

                CopyEntityToDef(entity, def); // TEntityDef is a class -> dictionary entries are updated, too

                def.InternalName = iname;
            }

            foreach (var kvp in byDisplayName)
                if (!VanillaDefsByName.ContainsKey(kvp.Key))
                    VanillaDefsByName.Add(kvp.Key, kvp.Value);

            PostFillVanilla();

            FillingVanilla = false;
        }

        internal void Reset()
        {
            ExtendVanillaArrays(-1);

            NextTypeIndex = MaxVanillaID;

            DefsByType.Clear();
        }

        internal IEnumerable<LoaderError> Load(Dictionary<string, TEntityDef> dict)
        {
            var err = new List<LoaderError>();

            ExtendVanillaArrays(dict.Count);

            foreach (var def in dict.Values)
            {
                if (!Main.dedServ)
                {
                    var cterrs = CheckTextures(def);

                    if (cterrs.Count > 0)
                    {
                        err.AddRange(cterrs);
                        continue;
                    }
                }

                def.Type = NextTypeIndex;

                if (!Main.dedServ)
                {
                    var lterrs = LoadTextures(def);

                    if (lterrs.Count > 0)
                    {
                        err.AddRange(lterrs);
                        continue;
                    }
                }

                CopySetProperties(def);
                DefsByType.Add(NextTypeIndex++, def);
            }

            return err;
        }
    }

    // because I can
    /// <summary>
    /// <see cref="EntityDefHandler{TEntityDef, TBehaviour, TEntity}" /> helper class for <see cref="Entity" />s.
    /// </summary>
    abstract class TEntityDefHandler<TEntityDef, TBehaviour, TEntity> : EntityDefHandler<TEntityDef, TBehaviour, TEntity>
        where TEntity : Entity
        where TBehaviour : EntityBehaviour<TEntity>
        where TEntityDef : EntityDef<TBehaviour, TEntity>//, new()
    {
        protected override string GetNameVanillaMethod(TEntity entity)
        {
            return entity.name;
        }
    }
}
