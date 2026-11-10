using Template.Core.Settings;

namespace Template.Core.Events
{
    /// <summary>
    /// One settings value changed. Subscribers apply what they own and ignore the rest (audio
    /// listens for the volume keys, gameplay for vibration, M5 localization for the language).
    /// </summary>
    public readonly struct SettingsChanged : IEvent
    {
        public SettingsChanged(SettingsKey key)
        {
            Key = key;
        }

        public SettingsKey Key { get; }
    }

    /// <summary>A save document was written and is durable as of this event.</summary>
    public readonly struct SaveWritten : IEvent
    {
        public SaveWritten(string key, int version)
        {
            Key = key;
            Version = version;
        }

        public string Key { get; }

        public int Version { get; }
    }

    /// <summary>Published once, before the first service initializes.</summary>
    public readonly struct AppBootStarted : IEvent
    {
        public AppBootStarted(int serviceCount)
        {
            ServiceCount = serviceCount;
        }

        public int ServiceCount { get; }
    }

    /// <summary>One per service, in boot order, after that service has finished initializing.</summary>
    public readonly struct ServiceInitialized : IEvent
    {
        public ServiceInitialized(string serviceName, int order)
        {
            ServiceName = serviceName;
            Order = order;
        }

        public string ServiceName { get; }

        public int Order { get; }
    }

    /// <summary>Every service initialized. The first real scene is loaded after this.</summary>
    public readonly struct AppBootCompleted : IEvent
    {
        public AppBootCompleted(int serviceCount)
        {
            ServiceCount = serviceCount;
        }

        public int ServiceCount { get; }
    }

    /// <summary>
    /// A covered scene transition began (fade out started). Screens and gameplay can mute input
    /// and audio cues on this and listen for <see cref="SceneTransitionCompleted"/> to resume.
    /// </summary>
    public readonly struct SceneTransitionStarted : IEvent
    {
        public SceneTransitionStarted(string sceneName)
        {
            SceneName = sceneName;
        }

        public string SceneName { get; }
    }

    /// <summary>
    /// A covered transition finished: the scene is active and the fade back has completed.
    /// ElapsedSeconds measures fade-out through fade-in, transition to transition.
    /// </summary>
    public readonly struct SceneTransitionCompleted : IEvent
    {
        public SceneTransitionCompleted(string sceneName, float elapsedSeconds)
        {
            SceneName = sceneName;
            ElapsedSeconds = elapsedSeconds;
        }

        public string SceneName { get; }

        public float ElapsedSeconds { get; }
    }

    /// <summary>
    /// A service threw while initializing. The chain stops here: a later service that depends on
    /// the broken one must not be allowed to start on half-built state.
    /// </summary>
    public readonly struct AppBootFailed : IEvent
    {
        public AppBootFailed(string serviceName, string reason)
        {
            ServiceName = serviceName;
            Reason = reason;
        }

        public string ServiceName { get; }

        public string Reason { get; }
    }
}
