﻿using Game.Universe;
using IRSE.Managers.Handlers;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Logger = NLog.Logger;

namespace IRSE.Managers
{
    public class HandlerManager
    {
        #region Fields

        private static Logger mainLog; //mainLog.Error
        private Game.Server.ControllerManager m_controllerManager;
        private UniverseHandler m_universeHandler;
        private readonly Assembly m_serverAssembly;
        private Assembly m_frameworkAssembly;
        private Type m_gameStateType;
        private Object server;

        #endregion Fields

        #region Properties

        public ChatHandler ChatHandler { get; private set; }
        public NetworkHandler NetworkHandler { get; private set; }
        public PlayerHandler PlayerHandler { get; private set; }
        public UniverseHandler UniverseHandler { get; private set; }

        public Game.Server.ControllerManager ControllerManager => m_controllerManager;

        #endregion Properties

        public HandlerManager(Assembly assembly, Assembly frameworkAssembly)
        {
            mainLog = NLog.LogManager.GetCurrentClassLogger();

            m_serverAssembly = assembly;
            m_frameworkAssembly = frameworkAssembly;
        }

        public Object GetHandlers()
        {
            try
            {
                m_gameStateType = m_frameworkAssembly.GetType("Game.GameStates.GameState");
                PropertyInfo m_activeState = m_gameStateType.GetProperty("ActiveState");
                object server = m_activeState.GetValue(null);

                if (server == null)
                    return null;

                mainLog.Info("IRSE: Loaded GameServer Instance!");

                FieldInfo m_controllerManagerField = server.GetType().GetField("m_controllers", BindingFlags.NonPublic | BindingFlags.Instance);

                m_controllerManager = m_controllerManagerField.GetValue(server) as Game.Server.ControllerManager;

                var universe = m_controllerManager.Universe as Game.Server.UniverseController;

                mainLog.Info("IRSE: Waiting for Universe To Populate..");

                // will be removed when they fix the ghost client spawner
                ServerInstance.Instance.SpawnGhostClients(m_controllerManager);

                while (m_controllerManager.Players.AllPlayers().Count() < 1)
                {
                    Thread.Sleep(1000);
                    if (m_controllerManager.Players.AllPlayers().Count() <= 1)
                    {
                        break;
                    }
                }

                mainLog.Info("IRSE: Loading Handlers..");
                NetworkHandler = new NetworkHandler(m_controllerManager);
                NetworkHandler.SetupNetworkHandler(server);

                PlayerHandler = new PlayerHandler(m_controllerManager);
                PlayerHandler.SetupPlayerHandler(server);

                ChatHandler = new ChatHandler(m_controllerManager);
                ChatHandler.SetupChatMessageHandler(NetworkHandler);

                UniverseHandler = new UniverseHandler(m_controllerManager);
                UniverseHandler.SetupHandler(server);

                mainLog.Info("IRSE: Loaded Universe!");

                return true;
            }
            catch (ArgumentException ex)
            {
                mainLog.Error("Failed to get handlers [Argument Exception] \r\n Exception:" + ex.ToString());
                return null;
            }
            catch (Exception ex)
            {
                mainLog.Error("Failed to get handlers \r\n Exception: " + ex.ToString());
                return null;
            }
        }

        // todo move to class

        public void ForceGalaxySave()
        {
            if (!ServerInstance.Instance.IsRunning)
                return;

            try
            {
                mainLog.Info("Attempting to Force a save!!!!");

                (m_controllerManager.Universe.Galaxy as ServerGalaxy).SaveGalaxy(m_controllerManager, "user", m_controllerManager.Universe.Galaxy.Name.ToLower(), true);

                mainLog.Info("Saved Galaxy!");
            }
            catch (Exception ex)
            {
                mainLog.Error("Save Failed! Exception Info: " + ex.ToString());
            }
        }
    }
}