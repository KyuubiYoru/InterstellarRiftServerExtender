﻿using Game.Framework.Networking;
using Game.Server;
using NLog;
using System;

namespace IRSE.Managers.Plugins
{
    public abstract class PluginBase : IPlugin
    {
        #region Fields

        protected String m_directory;
        protected ControllerManager m_controllers;
        protected String m_version;
        protected String m_desc;
        protected String m_author;
        protected String m_name;
        protected String m_api;
        protected String[] m_aillias;
        protected Guid m_id;
        protected PluginHelper m_plugin_helper;
        protected Boolean isenabled = false;

        protected Logger m_log;
        protected PluginBaseConfig m_config;

        #endregion Fields
        

        #region Properties

        public virtual Boolean Enabled { get { return isenabled; } internal set { isenabled = value; } }
        public virtual Guid Id { get { return m_id; } internal set { m_id = value; } }
        public virtual String GetName { get { return m_name; } internal set { m_name = value; } }
        public virtual String Version { get { return m_version; } internal set { m_version = value; } }
        public virtual String Description { get { return m_desc; } internal set { m_desc = value; } }
        public virtual String Author { get { return m_author; } internal set { m_author = value; } }
        public virtual String Directory { get { return m_directory; } internal set { m_directory = value; } }
        public virtual String API { get { return m_api; } internal set { m_api = value; } }
        public virtual String[] Aillias { get { return m_aillias; } internal set { m_aillias = value; } }
        public virtual ControllerManager GetControllers { get { return m_controllers; } }

        public virtual PluginHelper GetPluginHelper { get { return m_plugin_helper; } }

        public virtual Logger PluginLog { get { return m_log; } }
        public virtual PluginBaseConfig Config { get { return m_config; } }

        public Logger GetLogger { get { return LogManager.GetCurrentClassLogger(); ; } }

        #endregion Properties

        #region Methods

        public PluginBase()
        {
            m_controllers = ServerInstance.Instance.Handlers.ControllerManager;

            m_plugin_helper = new PluginHelper(m_controllers);

            m_log = NLog.LogManager.GetCurrentClassLogger();

        }

        public virtual void Init(String modDirectory)
            => Init();

        public virtual void Init()
        {
            Enabled = true;
            if (!ServerInstance.Instance.IsRunning)
            {
                //ERROR! No Server Running!
                Console.WriteLine("No Running Server found!");
                OnDisable();
            }

            PluginAttribute pluginAttribute = Attribute.GetCustomAttribute(GetType(), typeof(PluginAttribute), true) as PluginAttribute;
            if (pluginAttribute != null)
            {
                GetName = pluginAttribute.Name;
                Version = pluginAttribute.Version;
                Description = pluginAttribute.Description;
                Author = pluginAttribute.Author;
                Enabled = true;
            }
            else
            {
                Enabled = false;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Plugin Invalid! No Plugin Attribute Found!");
                Console.ResetColor();
                return;
            }

            Console.WriteLine("Plugin Loaded!");
            //#TODO add Logger
        }

        public void Shutdown()
        {
            DisablePlugin();
        }

        public virtual void OnUpdate()
        {
            
        }

        public void OnLoad()
        {
        }

        public void DisablePlugin(bool remove = true)
        {
            Enabled = true;
            OnDisable();
            //if (remove) ServerInstance.Instance.PluginManager.ShutdownPlugin(Plugin);
        }

        public virtual void OnEnable()
        {
        }

        public virtual void OnDisable()
        {
        }

        public virtual void OnCommand(Player p, String command, String[] args)
        {
        }

        public virtual void OnConsoleCommand(String command, String[] args)
        {
        }

        #endregion Methods
    }
}