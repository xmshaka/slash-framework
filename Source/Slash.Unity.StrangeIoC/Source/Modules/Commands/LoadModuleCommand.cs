﻿namespace Slash.Unity.StrangeIoC.Modules.Commands
{
    using strange.extensions.command.impl;
    using strange.extensions.context.api;
    using Slash.Unity.StrangeIoC.Configs;

    public class LoadModuleCommand : Command
    {
        [Inject(ContextKeys.CONTEXT)]
        public ModuleContext Context { get; set; }

        [Inject]
        public StrangeModule Module { get; set; }

        /// <inheritdoc />
        public override void Execute()
        {
            this.Context.AddSubModule(this.Module);
        }
    }
}