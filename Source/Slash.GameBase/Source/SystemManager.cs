﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SystemManager.cs" company="Slash Games">
//   Copyright (c) Slash Games. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Slash.GameBase
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   Manages the game systems to be updated in each tick.
    /// </summary>
    public class SystemManager
    {
        #region Fields

        /// <summary>
        ///   Game this manager controls the systems of.
        /// </summary>
        private readonly Game game;

        /// <summary>
        ///   Maps system types to actual game systems.
        /// </summary>
        private readonly Dictionary<Type, ISystem> systems;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Constructs a new system manager without any systems.
        /// </summary>
        /// <param name="game">
        ///   Game to manage the systems for.
        /// </param>
        public SystemManager(Game game)
        {
            this.game = game;
            this.systems = new Dictionary<Type, ISystem>();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///   Adds the passed system to this manager. The system will be updated
        ///   in each tick.
        /// </summary>
        /// <param name="system">
        ///   System to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   The passed system is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   A system of the same type has already been added.
        /// </exception>
        public void AddSystem(ISystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException("system");
            }

            Type systemType = system.GetType();

            if (this.systems.ContainsKey(systemType))
            {
                throw new ArgumentException("A system of type " + systemType + " has already been added.", "system");
            }

            this.systems.Add(system.GetType(), system);

            this.game.EventManager.QueueEvent(FrameworkEventType.SystemAdded, system);
        }

        /// <summary>
        ///   Gets the system of the specified type.
        /// </summary>
        /// <param name="systemType">
        ///   Type of the system to get.
        /// </param>
        /// <returns>
        ///   System of the specified type.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   The passed type is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   A system of the specified type has never been added.
        /// </exception>
        public ISystem GetSystem(Type systemType)
        {
            if (systemType == null)
            {
                throw new ArgumentNullException("systemType");
            }

            ISystem system;
            if (this.systems.TryGetValue(systemType, out system))
            {
                return system;
            }

            throw new ArgumentException("A system of type " + systemType + " has never been added.", "systemType");
        }

        /// <summary>
        ///   Gets the system of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of the system to get.</typeparam>
        /// <returns>System of the specified type.</returns>
        /// <exception cref="ArgumentNullException">
        ///   The passed type is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   A system of the specified type has never been added.
        /// </exception>
        public T GetSystem<T>() where T : class, ISystem
        {
            return this.GetSystem(typeof(T)) as T;
        }

        /// <summary>
        ///   Ticks all systems.
        /// </summary>
        /// <param name="dt">
        ///   Time passed since the last tick, in seconds.
        /// </param>
        public void Update(float dt)
        {
            foreach (ISystem system in this.systems.Values)
            {
                system.UpdateSystem(dt);
            }
        }

        #endregion
    }
}