using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Platform.iOS;

namespace Microsoft.Maui
{
	public class MauiContext : IMauiContext
	{
		readonly IServiceProvider? _services;
		readonly IMauiHandlersServiceProvider? _mauiHandlersServiceProvider;
		readonly IAnimationManager? _animationManager;

		public MauiContext()
		{
		}

		public MauiContext(IServiceProvider services)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_mauiHandlersServiceProvider = Services.GetRequiredService<IMauiHandlersServiceProvider>();
			
			_animationManager = Services.GetService<IAnimationManager>() ?? new AnimationManger();
			_animationManager.Ticker = Services.GetService<ITicker>() ?? new MaciOSTicker();
			
		}

		public IServiceProvider Services =>
			_services ?? throw new InvalidOperationException($"No service provider was specified during construction.");

		public IMauiHandlersServiceProvider Handlers =>
			_mauiHandlersServiceProvider ?? throw new InvalidOperationException($"No service provider was specified during construction.");

		public IAnimationManager AnimationManager =>
			_animationManager ?? throw new InvalidOperationException($"No service provider was specified during construction.");
	}
}