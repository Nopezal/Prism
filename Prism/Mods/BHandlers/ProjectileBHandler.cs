﻿using System;
using System.Collections.Generic;
using System.Linq;
using Prism.API.Behaviours;
using Prism.Mods.Hooks;
using Terraria;

namespace Prism.Mods.BHandlers
{
    public sealed class ProjectileBHandler : EntityBHandler<ProjectileBehaviour, Projectile>
    {
        IEnumerable<Action> onUpdate;

        public override void Create()
        {
            base.Create();

            onUpdate = HookManager.CreateHooks<ProjectileBehaviour, Action>(Behaviours, "OnUpdate");
        }
        public override void Clear ()
        {
            base.Clear ();

            onUpdate = null;
        }

        public void OnUpdate()
        {
            HookManager.Call(onUpdate);
        }
    }
}