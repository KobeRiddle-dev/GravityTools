using System;
using FlaxEngine;

namespace GravityTools
{
    /// <summary>
    /// The sample game plugin.
    /// </summary>
    /// <seealso cref="FlaxEngine.GamePlugin" />
    public class GravityTools : GamePlugin
    {
        /// <inheritdoc />
        public GravityTools()
        {
            _description = new PluginDescription
            {
                Name = "GravityTools",
                Category = "Other",
                Author = "KobeRiddle-dev",
                AuthorUrl = null,
                HomepageUrl = null,
                RepositoryUrl = "https://github.com/FlaxEngine/GravityTools",
                Description = "This is an example plugin project.",
                Version = new Version(0, 1),
                IsAlpha = false,
                IsBeta = false,
            };
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            Debug.Log("Hello from plugin code!");
        }

        /// <inheritdoc />
        public override void Deinitialize()
        {
            // Use it to cleanup data

            base.Deinitialize();
        }
    }
}
